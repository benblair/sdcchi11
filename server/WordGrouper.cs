using System.Collections.Generic;
using System.Linq;
using Bbr.Extensions;

namespace Cerrio.Samples.SDC
{
    public class WordGrouper
    {
        public IEnumerable<WordResults> GetWords(IEnumerable<UserTweetData> users)
        {
            Dictionary<string, int> allCount = new Dictionary<string, int>();
            //word,user:count

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

                }
            }


            List<WordResults> results = new List<WordResults>();

            foreach(string word in allCount.Keys)
            {
                WordResults result = new WordResults {Word = word, Occurrences = allCount[word]};
                List<double> values =
                    users.Select(u => u.WordProbibility.ContainsKey(word) ? u.WordProbibility[word] : 0).ToList();
                double std = values.StandardDeviation();
                result.StandardDeviation = std;
                result.Average = values.Average();
                result.StandardDeviationScaled = std / result.Average;
                results.Add(result);
            }

            return results;
        }
    }

    public class WordResults
    {
        public string Word { get; set; }
        public int Occurrences { get; set; }

        public double Average { get; set; }

        public double StandardDeviation { get; set; }

        public double StandardDeviationScaled { get; set; }
    }
}
