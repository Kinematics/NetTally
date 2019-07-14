using System;
using System.Collections.Generic;
using System.Text;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVoteCounting;

namespace NetTally.Experiment3
{
    public class RankVoteCounterFactory
    {
        public RankVoteCounterFactory() { }

        public IRankVoteCounter2 CreateRankVoteCounter(RankVoteCounterMethod rankVoteCounterMethod)
        {
            IRankVoteCounter2 rankVoteCounter = rankVoteCounterMethod switch
            {
                RankVoteCounterMethod.RIRV => (IRankVoteCounter2)new RIRVRankVoteCounter(),
                RankVoteCounterMethod.Baldwin => new BaldwinRankVoteCounter(),
                RankVoteCounterMethod.Schulze => new SchulzeRankVoteCounter(),
                RankVoteCounterMethod.Wilson => new WilsonRankVoteCounter(),
                RankVoteCounterMethod.Default => new RIRVRankVoteCounter(),
                _ => throw new ArgumentOutOfRangeException($"Unknown rank vote counter type: {rankVoteCounterMethod}", nameof(rankVoteCounterMethod))
            };

            return rankVoteCounter;
        }
    }
}
