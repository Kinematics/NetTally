using System.Collections.Generic;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Base vote counter interface, that all other vote counter interfaces derive from.
    /// </summary>
    public interface IBaseVoteCounter
    {

    }

    /// <summary>
    /// Vote counter interface for ranked votes.
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.IBaseVoteCounter" />
    public interface IRankVoteCounter : IBaseVoteCounter
    {
        RankResultsByTask CountVotes(Dictionary<string, HashSet<string>> votes);
    }
}
