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
        private readonly int m_maxUsers;
        public string RequestingUser { get; private set; }

        private static string s_catchAll = "Other";
        private object m_lockObject = new object();

        private Dictionary<string, OutputData> m_lastResults = new Dictionary<string, OutputData>();

        public PerUserGrouper(Pig<OutputData, string> outputPig, string requestingUser,int maxUsers)
        {
            m_ouptutPig = outputPig;
            m_maxUsers = maxUsers;
            RequestingUser = requestingUser;


            try
            {
                m_ouptutPig.Tree.Root
                    .Where<OutputData>(i => i.OriginatingUser == RequestingUser)
                    .ForEach(i => m_lastResults.Add(i.TwitterHandle, i));
                bool.Parse("true");

            }
            catch (Exception)
            {
                //oh well it was worth a try
            }
        }

        public void DoAnalysis(IEnumerable<InputData> inputData)
        {
            lock (m_lockObject)
            {
                Dictionary<string, UserTweetData> data = new Dictionary<string, UserTweetData>();
                Dictionary<string, OutputData> results = new Dictionary<string, OutputData>();

                foreach (InputData inputLine in inputData
                    .OrderByDescending(d=>d.Text.Length)
                    .Take(m_maxUsers))
                {
                    data[inputLine.User] = new UserTweetData
                                               {
                                                   UserName = inputLine.User,
                                                   Corpus = inputLine.Text.ToLowerInvariant()
                                               };
                }

                if(data.Any())
                {
                    if (null != m_lastResults)
                    {
                        m_lastResults.ForEach(i =>
                        {
                            if (data.ContainsKey(i.Key))
                            {
                                data[i.Key].X = i.Value.X;
                                data[i.Key].Y = i.Value.Y;
                            }
                        });
                    }

                    Dictionary<string, WordResults> wordCounts = GetGroupingWords(data.Values);

                    int connections = AddConnections(data, wordCounts);


                    EventLog.Log(Severity.Medium, "added {0} connections for {1} users", connections, data.Count);
                    List<UserTweetData> items = data.Values.ToList();
                    //items = ThirdParty(items, "LinLog").ToList(); //shows very little layout persistance

                    ForceDirectedLayout layout = new ForceDirectedLayout(1, 1);
                    layout.Layout(items.As<UserTweetData, IPosititonable>());

                    Dictionary<KMeans<UserTweetData>.Cluster, string> clusters = TestClusters(items, wordCounts);


                    foreach (KMeans<UserTweetData>.Cluster cluster in clusters.Keys)
                    {
                        string label = clusters[cluster];
                        EventLog.Log(Severity.Medium, "Got label: " + label + " for user: " + RequestingUser);

                        foreach (UserTweetData item in cluster.Items)
                        {
                            if (!string.IsNullOrEmpty(item.UserName))
                            {
                                OutputData output = ConverterToOutPut(item, label, cluster.Mean);
                                results.Add(output.Key, output);
                            }
                        }
                    }
                }



                EventLog.Log("Done with user: {0}. Has {1} items", RequestingUser,results.Count);

                if (null != m_ouptutPig)
                {
                    if (0 == results.Count && null!=m_lastResults && m_lastResults.Count>0)
                    {
                        m_lastResults.Values.ForEach(d => m_ouptutPig.SendDelete(d));
                    }
                    m_lastResults = results;



                    m_ouptutPig.Republish(results, item => item.OriginatingUser == RequestingUser,
                    m_ouptutPig.GetUpdateToken("X", "Y", "GroupName", "GroupCenterX",
                                             "GroupCenterY"));

                }
            }
        }

        private Dictionary<KMeans<UserTweetData>.Cluster, string> TestClusters(IList<UserTweetData> items, Dictionary<string, WordResults> wordCounts)
        {
            //from http://en.wikipedia.org/wiki/Determining_the_number_of_clusters_in_a_data_set
            //this is a rule of thumb for =icking K
            int i = (int)Math.Round(Math.Sqrt(items.Count / 2.0));

            Dictionary<KMeans<UserTweetData>.Cluster, string> possibleResult = new Dictionary<KMeans<UserTweetData>.Cluster, string>();
            HashSet<string> labels = new HashSet<string>();
            List<KMeans<UserTweetData>.Cluster> clusters = Cluster(items, Math.Min(i, items.Count));
            foreach (KMeans<UserTweetData>.Cluster cluster in clusters)
            {
                string label = GetLabel(cluster.Items, labels, wordCounts);
                labels.Add(label);
                possibleResult.Add(cluster, label);
            }

            return possibleResult;

        }

        private string GetLabel(IEnumerable<UserTweetData> data, ICollection<string> usedLabel, Dictionary<string, WordResults> wordCounts)
        {
            Dictionary<string, double> counts = new Dictionary<string, double>();
            Dictionary<string, int> uniqueUsers = new Dictionary<string, int>();

            foreach (UserTweetData d in data)
            {
                foreach (string word in d.WordCount.Keys)
                {
                    if (!usedLabel.Contains(word))
                    {
                        if(!uniqueUsers.ContainsKey(word))
                        {
                            uniqueUsers[word] = 1;
                        }
                        else
                        {
                            uniqueUsers[word]++;
                        }

                        double probiblityInMainCorpus;

                        if(wordCounts.ContainsKey(word))
                        {
                            probiblityInMainCorpus = wordCounts[word].Probability;
                        }
                        else
                        {
                            probiblityInMainCorpus = 1;
                        }

                        double score = d.Probability(word) / probiblityInMainCorpus;
                        if (!counts.ContainsKey(word))
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

            string result = counts.Keys.MaxElement(k => counts[k] * uniqueUsers[k]);
            return result;
        }

        private int AddConnections(Dictionary<string, UserTweetData> data, Dictionary<string, WordResults> wordCounts)
        {
            List<UserTweetData> items = data.Values.ToList();
            double[][] values = new double[items.Count][];
            List<Pair<double, Pair<int, int>>> counts = new List<Pair<double, Pair<int, int>>>();
            int links = 0;
            
            for (int i = 0; i < items.Count; i++)
            {
                values[i] = new double[items.Count];
                for (int j = i + 1; j < items.Count; j++)
                {
                    double count = 0;
                    foreach (string word in items[i].WordProbibility.Keys)
                    {
                        if (word.Length > 3)
                        {
                            WordResults wordresult = wordCounts.GetValueOrDefault(word);

                            /// the more words in common 2 users have the higher thier similarity
                            /// get extra points for length of word
                            /// get extra points for rarity of word in overall corpus

                            double bonus= word.Length / 10.0 / (null == wordresult ? 1 : wordresult.Probability);


                            count += bonus*Math.Min(items[i].Probability(word), items[j].Probability(word));
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


            double avg;
            if(counts.Any())
            {
                avg = counts.Average(p => p.First);
            }
            else
            {
                avg = 0;
            }

            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    if (values[i][j] > avg)
                    {
                        items[i].AddDependency(items[j]);
                        links++;
                    }
                }
            }

            if (!data.ContainsKey(""))
            {
                UserTweetData center = new UserTweetData
                {
                    UserName = "",
                };

                foreach(UserTweetData user in data.Values)
                {
                    user.AddDependency(center);
                }

                data.Add("",center);

            }

            return links;
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
                UserTweetData user = (UserTweetData) d;
                if (items.Contains(user))
                {
                    edges.Add(new Pair<UserTweetData, UserTweetData>(i, user));
                }

            }));
            GraphSharp<UserTweetData> graph = new GraphSharp<UserTweetData>(items, edges);
            return graph.Layout(type);
        }

    }
}
