using System.Collections.Generic;
using System.Linq;
using Bbr.Extensions;

namespace Cerrio.Samples.SDC
{
    public class WordGrouper
    {
        public Dictionary<string, WordResults> GetWords(IEnumerable<UserTweetData> users)
        {
            Dictionary<string, int> allCount = new Dictionary<string, int>();
            //word,user:count
            long allWords=0;

            foreach (UserTweetData user in users)
            {
                foreach (string word in user.WordCount.Keys)
                {
                    
                    if(!allCount.ContainsKey(word))
                    {
                        allCount.Add(word, user.WordCount[word]);
                    }
                    else
                    {
                        allCount[word] += user.WordCount[word];
                    }
                    allWords++;
                }
            }


            Dictionary<string, WordResults> results = new Dictionary<string, WordResults>();

            foreach(string word in allCount.Keys)
            {
                if (allCount[word] > 1)
                {
                    WordResults result = new WordResults
                                             {
                                                 Word = word,
                                                 Occurrences = allCount[word],
                                                 Probability = allCount[word]/(double) allWords
                                             };
                    List<double> values =
                        users.Select(u => u.WordProbibility.ContainsKey(word) ? u.WordProbibility[word] : 0).ToList();
                    double std = values.StandardDeviation();
                    result.StandardDeviation = std;
                    result.Average = values.Average();
                    results.Add(word, result);
                }
            }

            return results;
        }
    }

    public class WordResults
    {
        public string Word { get; set; }
        public int Occurrences { get; set; }

        public double Probability { get; set;}

        public double Average { get; set; }

        public double StandardDeviation { get; set; }

    }
}
