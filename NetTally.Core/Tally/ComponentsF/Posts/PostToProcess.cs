using System.Collections.Generic;
using NetTally.Tally.ComponentsF.Votes;

namespace NetTally.Tally.ComponentsF.Posts;
public class PostToProcess(PostType post)
{
    public PostType Post => post;

    public OriginType Origin => post.Origin;

    public List<VoteLineType> VoteLines => post.VoteLines;

    public bool HasVote => post.HasVote;

    /// <summary>
    /// Vote lines after processing to expand plans within the original vote, and remove proposed plans.
    /// The <see cref="VoteLineType"/> is a normal line, while the <see cref="VoteBlockType"/> represents a complete plan.
    /// The WorkingVote is a sequence of one or the other.
    /// </summary>
    public List<VoteBlockType> WorkingVote { get; } = [];

    /// <summary>
    /// Flag whether the WorkingVote has been completely filled in.
    /// </summary>
    public bool WorkingVoteComplete { get; set; }
    /// <summary>
    /// Flag whether this post has been processed.
    /// </summary>
    public bool Processed { get; set; }
    /// <summary>
    /// Flag to bypass process restrictions, if normal processing doesn't happen.
    /// </summary>
    public bool ForceProcess { get; set; }

    public void Reset()
    {
        Processed = false;
        ForceProcess = false;
        WorkingVoteComplete = false;
        WorkingVote.Clear();
    }
}
