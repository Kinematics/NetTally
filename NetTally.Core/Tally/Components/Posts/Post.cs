using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using NetTally.Enums;
using NetTally.Tally.Components.Threads;
using NetTally.Tally.Components.Votes;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Components.Posts;

/// <summary>
/// Record for a user post.
/// </summary>
/// <param name="Origin">Origin information on the post.</param>
/// <param name="Text">Text contents of the post.</param>
/// <param name="VoteLines">Any extracted vote lines from the post.</param>
public record PostType(OriginType Origin, string Text, ImmutableArray<VoteLineType> VoteLines)
{
    /// <summary>
    /// Whether the post has any vote lines.
    /// </summary>
    public bool HasVote => VoteLines.Length > 0;

    /// <summary>
    /// How many vote lines are contained in the post.
    /// </summary>
    public int VoteLineCount => VoteLines.Length;
}

/// <summary>
/// Class for creating <see cref="PostType"/> objects.
/// </summary>
public static class Post
{
    /// <summary>
    /// Create a <see cref="PostType"/> object for a provided origin
    /// and text content.
    /// </summary>
    /// <param name="origin">The origination of the post. Result is null if this is null.</param>
    /// <param name="text">The contents of the post. Result is null if this is null or empty.</param>
    /// <returns>A <see cref="PostType"/> containing the post information.</returns>
    public static PostType? Create(OriginType? origin, string text)
    {
        if (origin == null) return null;
        if (string.IsNullOrEmpty(text)) return null;

        var voteLines = VoteParser.ExtractVoteLines(text);

        return new PostType(origin, text, [.. voteLines]);
    }

    /// <summary>
    /// Create a <see cref="PostToProcess"/> object which encapsulates
    /// a <see cref="PostType"/>.
    /// </summary>
    /// <param name="origin">The post's origin.</param>
    /// <param name="text">The text contents of the post.</param>
    /// <returns></returns>
    public static PostToProcess? CreateToProcess(OriginType? origin, string text)
    {
        var post = Create(origin, text);
        if (post == null) return null;

        return new PostToProcess(post);
    }
}

/// <summary>
/// Extension methods for <see cref="PostType"/> objects.
/// </summary>
public static class PostExtensions
{
    /// <summary>
    /// Determine if a post falls before the starting point of the tallied range.
    /// </summary>
    /// <param name="post">The post to check</param>
    /// <param name="threadInfo">The tally range</param>
    /// <returns><c>True</c> if the post falls before the tally starting point.</returns>
    public static bool IsBeforeStart(this PostType post, ThreadInformationType threadInfo)
    {
        return threadInfo.PostRange switch
        {
            PostRangeById range => post.Origin.PostId.Id < range.PostId.Id,
            PostRangeByPosts range => post.Origin.ThreadPostNumber < range.StartPostNumber,
            _ => throw new InvalidOperationException("Unknown PostRange type.")
        };
    }

    /// <summary>
    /// Determine if a post falls after the ending point of the tallied range.
    /// </summary>
    /// <param name="post">The post to check</param>
    /// <param name="quest">The quest being tallied</param>
    /// <param name="threadInfo">The tally range</param>
    /// <returns><c>True</c> if the post falls after the tally ending point.</returns>
    public static bool IsAfterEnd(this PostType post, ThreadInformationType threadInfo)
    {
        return threadInfo.PostRange switch
        {
            PostRangeById => false,
            PostRangeByPosts range when range.EndPostNumber == 0 => false,
            PostRangeByPosts range => post.Origin.ThreadPostNumber > range.EndPostNumber,
            _ => throw new InvalidOperationException("Unknown PostRange type.")
        };
    }

    public static bool MatchesUsernameFilter(this PostType post, Quest quest)
    {
        return quest.UseCustomUsernameFilters && quest.UsernameFilter.Blocks(post.Origin.Author.Name);
    }

    public static bool MatchesPostNumberFilter(this PostType post, Quest quest)
    {
        return quest.UseCustomPostFilters &&
            (quest.PostsFilter.Blocks(post.Origin.ThreadPostNumber) ||
             quest.PostsFilter.Blocks(post.Origin.PostId.Id));
    }

}


/// <summary>
/// Comparer handler for <see cref="PostType"/> and <see cref="PostToProcess"/>
/// </summary>
public class PostComparer : IEqualityComparer<PostType>, IEqualityComparer<PostToProcess>
{
    /// <summary>
    /// Static instance of a <see cref="PostComparer"/>
    /// </summary>
    public static PostComparer Instance { get; } = new();

    public bool Equals(PostType? x, PostType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return OriginComparer.Instance.Equals(x.Origin, y.Origin) &&
            Agnostic.InsensitiveComparer.Equals(x.Text, y.Text);
    }

    public bool Equals(PostToProcess? x, PostToProcess? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Equals(x.Post, y.Post);
    }

    public int GetHashCode([DisallowNull] PostType obj)
    {
        return OriginComparer.Instance.GetHashCode(obj.Origin);
    }

    public int GetHashCode([DisallowNull] PostToProcess obj)
    {
        return GetHashCode(obj.Post);
    }
}
