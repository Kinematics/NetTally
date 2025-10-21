using System.Collections.Immutable;
using NetTally.Models.Posts;
using NetTally.Models.Behavior;
using NetTally.Models.Votes;

namespace NetTally.Tally.Processing;

/// <summary>
/// Class that encapculates a <see cref="Post.Component.Post"/>, and allows
/// setting mutable state describing it.
/// </summary>
/// <param name="post">The <see cref="Post.Component.Post"/> to encapsulate.</param>
public class PostToProcess(Post post)
{
    /// <summary>
    /// The encapsulated post.
    /// </summary>
    public Post Post => post;

    /// <summary>
    /// The post's origin.
    /// </summary>
    public Origin Origin => post.Origin;

    /// <summary>
    /// The post's vote lines.
    /// </summary>
    public ImmutableArray<VoteLine> VoteLines => post.VoteLines;

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
    /// </summary>
    public List<VoteBlockRef> WorkingVote { get; } = [];

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
