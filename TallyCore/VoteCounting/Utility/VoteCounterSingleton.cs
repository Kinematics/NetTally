using System;

namespace NetTally.VoteCounting
{
    public static class VoteCounter
    {
        public static IVoteCounter Instance { get; } = VoteCounterImpl.Instance;
    }
}
