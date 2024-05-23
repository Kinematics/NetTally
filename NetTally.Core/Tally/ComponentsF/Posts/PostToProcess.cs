using System.Collections.Generic;
using System.Collections.Immutable;
using NetTally.Tally.ComponentsF.Votes;

namespace NetTally.Tally.ComponentsF.Posts;

/// <summary>
/// Class that encapculates a <see cref="PostType"/>, and allows
/// setting mutable state describing it.
/// </summary>
/// <param name="post">The <see cref="PostType"/> to encapsulate.</param>
public class PostToProcess(PostType post)
{
    /// <summary>
    /// The encapsulated post.
    /// </summary>
    public PostType Post => post;

    /// <summary>
    /// The post's origin.
    /// </summary>
    public OriginType Origin => post.Origin;

    /// <summary>
    /// The post's vote lines.
    /// </summary>
    public ImmutableArray<VoteLineType> VoteLines => post.VoteLines;

    /// <summary>
    /// Whether the post has any vote lines.
    /// </summary>
    public bool HasVote => post.HasVote;

    /// <summary>
    /// How many vote lines are contained in the post.
    /// </summary>
    public int VoteLineCount => post.VoteLineCount;

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
