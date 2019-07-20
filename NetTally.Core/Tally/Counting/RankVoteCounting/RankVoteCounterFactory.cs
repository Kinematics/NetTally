using System;

namespace NetTally.VoteCounting.RankVotes
{
    public class RankVoteCounterFactory
    {
        public RankVoteCounterFactory() { }

        public IRankVoteCounter2 CreateRankVoteCounter(RankVoteCounterMethod rankVoteCounterMethod)
        {
            switch (rankVoteCounterMethod)
            {
                case RankVoteCounterMethod.RIRV:
                    return new RatedInstantRunoff();
                case RankVoteCounterMethod.Baldwin:
                    return new Baldwin();
                case RankVoteCounterMethod.Schulze:
                    return new Schulze();
                case RankVoteCounterMethod.Wilson:
                    return new Wilson();
                case RankVoteCounterMethod.Default:
                    return new RatedInstantRunoff();
                default:
                    throw new ArgumentOutOfRangeException($"Unknown rank vote counter type: {rankVoteCounterMethod}", nameof(rankVoteCounterMethod));
            }
        }
    }
}
