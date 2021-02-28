using System;
using NetTally.Types.Enums;

namespace NetTally.VoteCounting.RankVotes
{
    public class RankVoteCounterFactory
    {
        public RankVoteCounterFactory() { }

        public IRankVoteCounter2 CreateRankVoteCounter(RankVoteCounterMethod rankVoteCounterMethod)
        {
            IRankVoteCounter2 rankVoteCounter = rankVoteCounterMethod switch
            {
                RankVoteCounterMethod.RIRV => (IRankVoteCounter2)new RatedInstantRunoff(),
                RankVoteCounterMethod.Baldwin => new Baldwin(),
                RankVoteCounterMethod.Schulze => new Schulze(),
                RankVoteCounterMethod.Wilson => new Wilson(),
                RankVoteCounterMethod.Default => new RatedInstantRunoff(),
                _ => throw new ArgumentOutOfRangeException($"Unknown rank vote counter type: {rankVoteCounterMethod}", nameof(rankVoteCounterMethod))
            };

            return rankVoteCounter;
        }
    }
}
