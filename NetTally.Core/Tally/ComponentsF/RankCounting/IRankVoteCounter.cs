using System.Collections.Generic;
using NetTally.Tally.ComponentsF.Storage;

namespace NetTally.Tally.ComponentsF.RankCounting;

/// <summary>
/// Vote counter interface for ranked votes.
/// </summary>
public interface IRankVoteCounter
{
    // TODO: is rank needed in the tuple, considering a List<> is explicitly ordered?
    List<((int rank, double rankScore) ranking, VoteStorageEntryF vote)>
        CountVotesForTask(VoteStorage taskVotes);
}
