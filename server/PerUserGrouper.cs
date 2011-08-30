using System;
using System.Collections.Generic;
using System.Linq;
using Bbr.Collections;
using Bbr.Diagnostics;
using Bbr.Extensions;
using Bbr.Zaphod;
using Cerrio.Samples.Helpers;

namespace Cerrio.Samples.SDC
{
    class PerUserGrouper
    {
        private Pig<OutputData, string> m_ouptutPig;
        public string RequestingUser { get; private set;}

        private static string s_catchAll = "Other";
        private object m_lockObject = new object();

        public PerUserGrouper(Pig<OutputData, string> outputPig,string requestingUser)
        {
            m_ouptutPig = outputPig;
            RequestingUser = requestingUser;
        }

        public void DoAnalysis(IEnumerable<InputData> inputData)
        {
            lock (m_lockObject)
            {
                Dictionary<string, UserTweetData> data = new Dictionary<string, UserTweetData>();
                foreach (InputData inputLine in inputData)
                {
                    data[inputLine.User] = new UserTweetData
                                               {
                                                   UserName = inputLine.User,
                                                   Corpus = inputLine.Text.ToLowerInvariant()
                                               };
                }
                Dictionary<string, WordResults> wordCounts = GetGroupingWords(data.Values);

                int connections = AddConnections(data, wordCounts);
  

                EventLog.Log("added {0} connections for {1} users",connections,data.Count);
                List<UserTweetData> items = data.Values.ToList();
                items = ThirdParty(items, "LinLog").ToList();
                //items = ThirdParty(items, "Circular").ToList();
                //items = ThirdParty(items, "BoundedFR").ToList();
                //items = ThirdParty(items, "KK").ToList();
                //items = ThirdParty(items, "ISOM").ToList();//ok looks liek bigger groups
                //items = ThirdParty(items, "EfficientSugiyama").ToList();//ok but slow as balls
                //items = ThirdParty(items, "CompoundFDP").ToList();

                Dictionary<KMeans<UserTweetData>.Cluster, string> clusters = TestClusters(items, wordCounts);

                Dictionary<string, OutputData> results = new Dictionary<string, OutputData>();

                foreach (KMeans<UserTweetData>.Cluster cluster in clusters.Keys)
                {
                    string label = clusters[cluster];
                    EventLog.Log(Severity.Medium,"Got label: " + label + " for user: " + RequestingUser);
                    //double scale = Math.Sqrt(.5)*();

                    foreach (UserTweetData item in cluster.Items)
                    {
                        OutputData output = ConverterToOutPut(item, label, cluster.Mean);
                        results.Add(output.Key, output);
                    }
                }

                EventLog.Log("done " + RequestingUser);

                m_ouptutPig.Republish(results, item => item.OriginatingUser == RequestingUser,
                                      m_ouptutPig.GetUpdateToken("X", "Y", "GroupName", "GroupCenterX", "GroupCenterY"));
            }
        }

        private Dictionary<KMeans<UserTweetData>.Cluster, string> TestClusters(IList<UserTweetData> items, Dictionary<string, WordResults> wordCounts)
        {
            int i = (int)Math.Round(Math.Sqrt(items.Count/2.0));
            //for (int i = 1; i < items.Count / 2+1; i++)
            {
                Dictionary<KMeans<UserTweetData>.Cluster, string> possibleResult = new Dictionary<KMeans<UserTweetData>.Cluster, string>();
                HashSet<string> labels = new HashSet<string>();
                List<KMeans<UserTweetData>.Cluster> clusters = Cluster(items, Math.Min(i,items.Count));
                foreach (KMeans<UserTweetData>.Cluster cluster in clusters)
                {
                    string label = GetLabel(cluster.Items, labels, wordCounts);
                    labels.Add(label);
                    possibleResult.Add(cluster, label);
                }

                return possibleResult;
            }

            //throw new Exception("What the hecko, this should never happen, no suitable number of groups were found for user "+RequestingUser+"");
        }

        private string GetLabel(IEnumerable<UserTweetData> data, ICollection<string> usedLabel,Dictionary<string, WordResults> wordCounts)
        {
            Dictionary<string, double> counts = new Dictionary<string, double>();

            foreach (UserTweetData d in data)
            {
                foreach (string word in d.WordCount.Keys)
                {
                    if (!usedLabel.Contains(word))
                    {
                        double score = ScoreWord(word, d, wordCounts.GetValueOrDefault(word));
                        if(!counts.ContainsKey(word))
                        {
                            counts[word] = score;
                        }
                        else
                        {
                            counts[word] += score;
                        }
                    }
                }
            }

            if (counts.Count == 0)
            {
                return s_catchAll;
            }

            string result= counts.Keys.MaxElement(k => counts[k]);
            return result;
        }


        //the more words in common 2 users have the higher thier similarity
        //  get extra points for legnth of word
        //  get extra points for raratiy of word in overall corpus

        private int AddConnections(Dictionary<string, UserTweetData> data, Dictionary<string, WordResults> wordCounts)
        {
            List<UserTweetData> items = data.Values.ToList();
            double[][] values = new double[items.Count][];
            List<Pair<double, Pair<int, int>>> counts = new List<Pair<double, Pair<int, int>>>();
            int links=0;

            for (int i = 0; i < items.Count; i++)
            {
                values[i] = new double[items.Count];
                for (int j = i+1; j < items.Count; j++)
                {
                    double count = 0;
                    foreach(string word in items[i].WordProbibility.Keys)
                    {
                        if (word.Length > 3)
                        {
                            WordResults wordresult = wordCounts.GetValueOrDefault(word);
                            count += Math.Min(
                                ScoreWord(word, items[i], wordresult),
                                ScoreWord(word, items[j], wordresult));
                        }
                    }
                    values[i][j] = count;
                    if (count != 0)
                    {
                        counts.Add(new Pair<double, Pair<int, int>>(count, new Pair<int, int>(i, j)));
                    }
                }
            }

            counts.Sort((p1, p2) => p1.First.CompareTo(p2.First));

            /*EventLog.Log("there are {0} possible counts", counts.Count);
            double desiredConnections = data.Count*Math.Max(2,data.Count/10.0);

            for(int i=0;i<desiredConnections&&i<counts.Count;i++)
            {
                Pair<int, int> point = counts[i].Second;
                items[point.First].AddDependency(items[point.Second]);
                items[point.Second].AddDependency(items[point.First]);
                links += 2;
            }*/

            double avg = counts.Average(p=>p.First);

            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i+1; j < items.Count; j++)
                {
                    if(values[i][j]>avg)
                    {
                        items[i].AddDependency(items[j]);
                        items[j].AddDependency(items[i]);
                        links += 2;
                    }
                }
            }

            return links;
        }

        private double ScoreWord(string word, UserTweetData user,WordResults result)
        {
            return word.Length/10.0
                *1/(null==result?1:result.Probability)
                *user.Probability(word);
        }

        private List<KMeans<UserTweetData>.Cluster> Cluster(IList<UserTweetData> data, int k)
        {
            KMeans<UserTweetData> kMeans = new KMeans<UserTweetData>(
                (u1, u2) => u1.Distance(u2),
                list => new UserTweetData
                            {
                                X = list.Average(u => u.X),
                                Y = list.Average(u => u.Y),
                            });

            return kMeans.CreateCluster(k, data);
        }

        private OutputData ConverterToOutPut(UserTweetData item, string groupName, IPosititonable groupCenter)
        {
            OutputData outputData = new OutputData
            {
                GroupName = groupName,
                OriginatingUser = RequestingUser,
                TwitterHandle = item.UserName,
                X = item.X,
                Y = item.Y,
                GroupCenterX = groupCenter.X,
                GroupCenterY = groupCenter.Y
            };
            outputData.UpdateKey();
            return outputData;
        }

        private Dictionary<string, WordResults> GetGroupingWords(IEnumerable<UserTweetData> data)
        {
            WordGrouper grouper = new WordGrouper();
            return grouper.GetWords(data);
        }

        private IEnumerable<UserTweetData> ThirdParty(IEnumerable<UserTweetData> items, string type)
        {
            List<Pair<UserTweetData, UserTweetData>> edges = new List<Pair<UserTweetData, UserTweetData>>();
            items.ForEach(i => i.Dependencies.ForEach(d =>
            {
                if (items.Contains(d))
                {
                    edges.Add(new Pair<UserTweetData, UserTweetData>(i, d));
                }

            }));
            GraphSharp<UserTweetData> graph = new GraphSharp<UserTweetData>(items, edges);
            return graph.Layout(type);
        }

    }
}
