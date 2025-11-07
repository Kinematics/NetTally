using System.Collections.Immutable;
using NetTally.Models;

namespace NetTally.Tally.Processing;

public class VoteToProcess(Vote vote)
{
    /// <summary>
    /// The encapsulated vote.
    /// </summary>
    public Vote Vote => vote;

    /// <summary>
    /// The vote's origin.
    /// </summary>
    public Origin Origin => vote.Origin;

    /// <summary>
    /// The vote's vote lines.
    /// </summary>
    public ImmutableList<VoteLine> VoteLines => vote.VoteLines;

    /// <summary>
    /// Vote lines after processing to expand plans within the original vote, and remove proposed plans.
    /// </summary>
    public List<VoteBlockRef> WorkingVote { get; } = [];

    /// <summary>
    /// Flag whether the WorkingVote has been completely filled in.
    /// </summary>
    public bool WorkingVoteComplete { get; set; }

    /// <summary>
    /// Flag whether this vote has been processed.
    /// </summary>
    public bool Processed { get; set; }

    /// <summary>
    /// Flag to bypass process restrictions, if normal processing doesn't happen.
    /// </summary>
    public bool ForceProcess { get; set; }

    /// <summary>
    /// Reset processed state back to the default.
    /// </summary>
    public void Reset()
    {
        Processed = false;
        ForceProcess = false;
        WorkingVoteComplete = false;
        WorkingVote.Clear();
    }
}
