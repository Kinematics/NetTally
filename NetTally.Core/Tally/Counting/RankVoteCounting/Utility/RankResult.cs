using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTally.VoteCounting.RankVoteCounting.Utility
{
    /// <summary>
    /// Individual rank result, with associated debugging info for more detailed output.
    /// </summary>
    class RankResult
    {
        public string Option { get; }
        public string Debug { get; }

        public RankResult(string option, string debug = "")
        {
            Option = option;
            Debug = debug;
        }
    }

    /// <summary>
    /// List of preference results ordered by winner
    /// </summary>
    class RankResults : List<RankResult>
    {
        public RankResults() { }

        public RankResults(List<string> listOfOptions)
        {
            AddRange(listOfOptions.Select(o => new RankResult(o)));
        }
    }

    /// <summary>
    /// Task (string), Ordered list of ranked votes
    /// </summary>
    class RankResultsByTask : Dictionary<string, RankResults> { }
}
