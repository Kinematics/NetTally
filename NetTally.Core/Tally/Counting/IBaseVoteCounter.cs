using System.Collections.Generic;
using NetTally.Experiment3;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting
{
    /// <summary>
    /// Vote counter interface for ranked votes.
    /// </summary>
    interface IRankVoteCounter
    {
        RankResultsByTask CountVotes(Dictionary<string, HashSet<string>> votes);
    }

    public interface IRankVoteCounter2
    {
        List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>
            CountVotesForTask(VoteStorage taskVotes);
    }
}
