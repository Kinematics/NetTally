using System.Collections.Immutable;
using NetTally.Models.Votes;

namespace NetTally.Models.Posts;

/// <summary>
/// Record for a user post.
/// </summary>
/// <param name="Origin">Origin information on the post.</param>
/// <param name="Text">Text contents of the post.</param>
/// <param name="VoteLines">Any extracted vote lines from the post.</param>
public sealed record Post(Origin Origin, string Text, ImmutableArray<VoteLine> VoteLines);
