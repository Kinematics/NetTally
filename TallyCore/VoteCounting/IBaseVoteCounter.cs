using System.Collections.Generic;

namespace NetTally.VoteCounting
{
    // Task (string), Ordered list of ranked votes
    using RankResultsByTask = Dictionary<string, List<string>>;
    // Vote (string), collection of voters
    using SupportedVotes = Dictionary<string, HashSet<string>>;

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
        RankResultsByTask CountVotes(SupportedVotes votes);
    }
}
