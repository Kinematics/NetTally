using System;
using NetTally.VoteCounting.RankVoteCounting;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Static class used to request the proper vote counter class to use for
    /// any given situation.
    /// </summary>
    static class VoteCounterLocator
    {
        /// <summary>
        /// Gets a rank vote counter.
        /// </summary>
        /// <param name="method">The methodology that the requested vote rank counter should use.</param>
        /// <returns>Returns a class to handle counting rank votes using the requested methodology.</returns>
        public static IRankVoteCounter GetRankVoteCounter(RankVoteCounterMethod method = RankVoteCounterMethod.Default)
        {
            switch (method)
            {
                //case RankVoteCounterMethod.Coombs:
                //    return new CoombsRankVoteCounter();
                //case RankVoteCounterMethod.LegacyCoombs:
                //    return new LegacyCoombsRankVoteCounter();
                //case RankVoteCounterMethod.InstantRunoff:
                //    return new InstantRunoffRankVoteCounter();
                //case RankVoteCounterMethod.Borda:
                //    return new BordaRankVoteCounter();
                //case RankVoteCounterMethod.BordaNormalized:
                //    return new BordaNormalizedRankVoteCounter();
                //case RankVoteCounterMethod.Pairwise:
                //    return new PairwiseRankVoteCounter();
                //case RankVoteCounterMethod.Distance:
                //    return new DistanceRankVoteCounter();
                //case RankVoteCounterMethod.DistanceU0:
                //    return new DistanceU0RankVoteCounter();
                case RankVoteCounterMethod.Baldwin:
                    return new BaldwinRankVoteCounter();
                case RankVoteCounterMethod.Wilson:
                    return new WilsonRankVoteCounter();
                case RankVoteCounterMethod.Schulze:
                    return new SchulzeRankVoteCounter();
                case RankVoteCounterMethod.RIRV:
                    return new RIRVRankVoteCounter();
                default:
                    return new RIRVRankVoteCounter();
            }
        }
    }
}
