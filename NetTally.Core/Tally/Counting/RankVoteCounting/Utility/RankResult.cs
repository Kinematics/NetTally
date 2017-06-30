using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTally.VoteCounting.RankVoteCounting.Utility
{
    /// <summary>
    /// Individual rank result, with associated debugging info for more detailed output.
    /// </summary>
    public class RankResult
    {
        public string Option { get; }
        public string Debug { get; }

        public RankResult(string option, string debug = null)
        {
            Option = option;
            Debug = debug ?? string.Empty;
        }
    }

    /// <summary>
    /// List of preference results ordered by winner
    /// </summary>
    public class RankResults : List<RankResult>
    {
        public RankResults() { }

        public RankResults(List<string> listOfOptions)
        {
            AddRange(listOfOptions.Select(o => new RankResult(o, null)));
        }
    }

    /// <summary>
    /// Task (string), Ordered list of ranked votes
    /// </summary>
    public class RankResultsByTask : Dictionary<string, RankResults> { }
}
