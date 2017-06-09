using System;
using NetTally.VoteCounting.RankVoteCounting;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Static class used to request the proper vote counter class to use for
    /// any given situation.
    /// </summary>
    public static class VoteCounterLocator
    {
        /// <summary>
        /// Generic function to get the default vote counter for a given vote type.
        /// </summary>
        /// <param name="voteType">Type of the vote.</param>
        /// <returns>Returns a base vote counter interface.</returns>
        public static IBaseVoteCounter GetVoteCounter(VoteType voteType)
        {
            switch (voteType)
            {
                case VoteType.Rank:
                    return GetRankVoteCounter();
                case VoteType.Vote:
                    return GetStandardVoteCounter();
                case VoteType.Approval:
                    return GetApprovalVoteCounter();
                default:
                    throw new ArgumentOutOfRangeException(nameof(voteType));
            }
        }

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
                case RankVoteCounterMethod.Democratix:
                    return new DemocratixFormat();
                case RankVoteCounterMethod.condorcetvote:
                    return new CondorcetVoteFormat();
                default:
                    return new RIRVRankVoteCounter();
            }
        }

        /// <summary>
        /// Gets a standard vote counter.
        /// </summary>
        /// <param name="method">The methodology that the requested vote counter should use.</param>
        /// <returns>Returns a class to handle counting standard votes using the requested methodology.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static IRankVoteCounter GetStandardVoteCounter(StandardVoteCounterMethod method = StandardVoteCounterMethod.Default)
        {
            switch (method)
            {
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets an approval vote counter.
        /// </summary>
        /// <param name="method">The methodology that the requested vote counter should use.</param>
        /// <returns>Returns a class to handle counting approval votes using the requested methodology.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static IRankVoteCounter GetApprovalVoteCounter(ApprovalVoteCounterMethod method = ApprovalVoteCounterMethod.Default)
        {
            switch (method)
            {
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
