using System.Collections.Generic;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Base vote counter interface, that all other vote counter interfaces derive from.
    /// </summary>
    interface IBaseVoteCounter
    {

    }

    /// <summary>
    /// Vote counter interface for ranked votes.
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.IBaseVoteCounter" />
    interface IRankVoteCounter : IBaseVoteCounter
    {
        RankResultsByTask CountVotes(Dictionary<string, HashSet<string>> votes);
    }
}
