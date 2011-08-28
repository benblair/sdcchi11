using System;
using System.Collections.Generic;
using System.Linq;

namespace Cerrio.Samples.SDC
{
    public class UserTweetData : IPosititonable
    {
        private static Random s_random = new Random();
        private List<UserTweetData> m_dependency = new List<UserTweetData>();
        public UserTweetData()
        {
            WordProbibility = new Dictionary<string, double>();
            WordCount = new Dictionary<string, int>();
            X = s_random.NextDouble();
            Y = s_random.NextDouble();
        }

        public string UserName { get; set; }

        private string m_corpus;
        public string Corpus
        {
            get { return m_corpus; }
            set
            {
                m_corpus = value;
                WordCount.Clear();
                WordProbibility.Clear();

                string[] words = m_corpus.ToLowerInvariant().Split(
                    new[] { ' ', '.', ',', '!', '?', ':', ';', '@', '/', '#', '\'' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(word => word.Length > 3)
                    .ToArray();

                foreach (string word in words)
                {
                    if (!WordCount.ContainsKey(word))
                    {
                        WordCount.Add(word, 1);
                    }
                    else
                    {
                        WordCount[word]++;
                    }
                }

                foreach (string word in WordCount.Keys)
                {
                    WordProbibility[word] = ((double)WordCount[word]) / words.Length * 100.0;
                }
            }
        }

        public Dictionary<string, int> WordCount { get; set; }

        public Dictionary<string, double> WordProbibility { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double Probability(string word)
        {
            if (!WordProbibility.ContainsKey(word))
            {
                return 0;
            }

            return WordProbibility[word];
        }

        public void AddDependency(UserTweetData item)
        {
            m_dependency.Add(item);
        }

        public double Distance(UserTweetData item)
        {
            return Math.Sqrt((X - item.X) * (X - item.X)
                + (Y - item.Y) * (Y - item.Y));
        }

        public IEnumerable<UserTweetData> Dependencies
        {
            get
            {
                return m_dependency;
            }
        }
    }
}
