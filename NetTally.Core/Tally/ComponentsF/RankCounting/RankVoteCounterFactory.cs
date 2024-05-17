using System;
using NetTally.Enums;

namespace NetTally.Tally.ComponentsF.RankCounting;

public static class RankVoteCounterFactory
{
    public static IRankVoteCounter CreateRankVoteCounter(RankVoteCounterMethod rankVoteCounterMethod)
    {
        IRankVoteCounter rankVoteCounter = rankVoteCounterMethod switch
        {
            RankVoteCounterMethod.RIRV => new RatedInstantRunoff(),
            RankVoteCounterMethod.Baldwin => new Baldwin(),
            RankVoteCounterMethod.Schulze => new Schulze(),
            RankVoteCounterMethod.Wilson => new Wilson(),
            RankVoteCounterMethod.Default => new RatedInstantRunoff(),
            _ => throw new ArgumentOutOfRangeException(nameof(rankVoteCounterMethod),
            $"Unknown rank vote counter type: {rankVoteCounterMethod}")
        };

        return rankVoteCounter;
    }
}
