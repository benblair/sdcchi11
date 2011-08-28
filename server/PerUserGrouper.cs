using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bbr.Collections;
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

        public PerUserGrouper(Pig<OutputData, string> outputPig,string requestingUser)
        {
            m_ouptutPig = outputPig;
            RequestingUser = requestingUser;
        }

        public void DoAnalysis(IEnumerable<InputData> inputData)
        {
            Dictionary<string, UserTweetData> data = new Dictionary<string, UserTweetData>();
            foreach (InputData inputLine in inputData)
            {
                data.Add(inputLine.User, new UserTweetData
                {
                    UserName = inputLine.User,
                    Corpus = inputLine.Text.ToLowerInvariant()
                });
            }
            IEnumerable<WordResults> stds = GetGroupingWords(data.Values);
            stds = stds.OrderByDescending(r => r.StandardDeviation).ToList();
            stds.ForEach(r => Console.WriteLine("{0}: {1}: {2:0.00} {3:0.00}", r.Word, r.Occurrences, r.StandardDeviation, r.StandardDeviationScaled));

            IEnumerable<WordResults> tops = stds.Take(Math.Min(5, stds.Count()));

            List<UserTweetData> items = AddConnections(data, tops);
            items = ThirdParty(items, "KK").ToList();
            //items = ThirdParty(items, "ISOM").ToList();//ok looks liek bigger groups
            //items = ThirdParty(items, "EfficientSugiyama").ToList();//ok but slow as balls
            //items = ThirdParty(items, "CompoundFDP").ToList();

            Dictionary<KMeans<UserTweetData>.Cluster, string> clusters = TestClusters(items, tops);

            Dictionary<string, OutputData> results = new Dictionary<string, OutputData>();

            foreach (KMeans<UserTweetData>.Cluster cluster in clusters.Keys)
            {
                string label = clusters[cluster];
                Console.WriteLine("Got label: " + label);
                //double scale = Math.Sqrt(.5)*();
                double scale = 1.5 * cluster.Items.Count() / items.Count;
                foreach (UserTweetData item in ThirdParty(cluster.Items, "Circular"))
                {
                    item.X = ((item.X -.5) * scale) + cluster.Mean.X;
                    item.Y = ((item.Y -.5) * scale) + cluster.Mean.Y;
                    OutputData output = ConverterToOutPut(item, label, cluster.Mean);
                    results.Add(output.Key, output);
                }
            }

            Console.WriteLine("done");

            m_ouptutPig.Republish(results, item => item.OriginatingUser == RequestingUser, m_ouptutPig.GetUpdateToken("X", "Y", "GroupName","GroupCenterX","GroupCenterY"));
        }

        private Dictionary<KMeans<UserTweetData>.Cluster, string> TestClusters(List<UserTweetData> items, IEnumerable<WordResults> tops)
        {
            for (int i = 1; i < items.Count / 2+1; i++)
            {
                Dictionary<KMeans<UserTweetData>.Cluster, string> possibleResult = new Dictionary<KMeans<UserTweetData>.Cluster, string>();
                HashSet<string> labels = new HashSet<string>();
                List<KMeans<UserTweetData>.Cluster> clusters = Cluster(items, i);
                foreach (KMeans<UserTweetData>.Cluster cluster in clusters)
                {
                    string label = GetLabel(cluster.Items, tops, labels);
                    labels.Add(label);
                    possibleResult.Add(cluster, label);
                }

                if (clusters.Any(c => c.Items.Count() <= 1)
                    || labels.Contains(s_catchAll))
                {
                    return possibleResult;
                }
            }

            throw new Exception("What the hecko, this should never happen, no suitabl number of groups were found");
        }

        private string GetLabel(IEnumerable<UserTweetData> data, IEnumerable<WordResults> results, HashSet<string> usedLabel)
        {
            Dictionary<string, double> counts = new Dictionary<string, double>();

            foreach (WordResults result in results)
            {
                if (usedLabel.Contains(result.Word))
                {
                    continue;
                }
                double cutOff = result.Average;
                foreach (UserTweetData d in data)
                {
                    if (d.Probability(result.Word) > cutOff)
                    {
                        double ammount = d.Probability(result.Word) - cutOff;
                        if (!counts.ContainsKey(result.Word))
                        {
                            counts[result.Word] = ammount;
                        }
                        else
                        {
                            counts[result.Word] += ammount;
                        }
                    }
                }
            }

            if (counts.Count == 0)
            {
                return s_catchAll;
            }

            return counts.Keys.MaxElement(k => counts[k]);
        }

        private List<UserTweetData> AddConnections(Dictionary<string, UserTweetData> data, IEnumerable<WordResults> tops)
        {
            foreach (UserTweetData i1 in data.Values)
            {
                foreach (UserTweetData i2 in data.Values)
                {
                    if (i1 != i2)
                    {
                        foreach (WordResults wordResult in tops)
                        {

                            double cutHigh = wordResult.Average + wordResult.StandardDeviation * .5;
                            double p1 = i1.Probability(wordResult.Word);
                            double p2 = i2.Probability(wordResult.Word);

                            if (p1 > cutHigh && p2 > cutHigh)
                            {
                                Console.WriteLine("Adding connection from {0} to {1}", i1.UserName, i2.UserName);
                                i1.AddDependency(i2);
                            }
                        }
                    }
                }
            }

            return data.Values.ToList();
        }

        private List<KMeans<UserTweetData>.Cluster> Cluster(List<UserTweetData> data, int k)
        {
            KMeans<UserTweetData> kMeans = new KMeans<UserTweetData>(
                (u1, u2) => u1.Distance(u2),
                list =>
                {
                    double x;
                    double y;
                    if (list.Count() > 1)
                    {
                        x = list.Average(u => u.X);
                        y = list.Average(u => u.Y);
                    }
                    else
                    {
                        x = list.First().X;
                        y = list.First().Y;
                    }
                    return new UserTweetData { X = x, Y = y, };
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

        private IEnumerable<WordResults> GetGroupingWords(IEnumerable<UserTweetData> data)
        {
            WordGrouper grouper = new WordGrouper();
            IEnumerable<WordResults> resuls = grouper.GetWords(data);

            return resuls.Where(r => r.Occurrences > 3 && r.StandardDeviation > 0);
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
