using System.Collections.Generic;
using NetTally.VoteCounting.RankVoteCounting.Utility;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    /// <summary>
    /// Vote counter interface for ranked votes.
    /// </summary>
    interface IRankVoteCounter
    {
        RankResultsByTask CountVotes(Dictionary<string, HashSet<string>> votes);
    }

    public interface IRankVoteCounter2
    {
        // TODO: is rank needed in the tuple, considering a List<> is explicitly ordered?
        List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>
            CountVotesForTask(VoteStorage taskVotes);
    }
}
