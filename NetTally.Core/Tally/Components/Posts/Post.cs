using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.Components.Threads;
using NetTally.Tally.Components.Votes;
using NetTally.Tally.Vote.Components;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Components.Posts;

/// <summary>
/// Record for a user post.
/// </summary>
/// <param name="Origin">Origin information on the post.</param>
/// <param name="Text">Text contents of the post.</param>
/// <param name="VoteLines">Any extracted vote lines from the post.</param>
public record Post(Origin Origin, string Text, ImmutableArray<VoteLineType> VoteLines)
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
/// Class for creating <see cref="Post"/> objects.
/// </summary>
public static class Posting
{
    /// <summary>
    /// Create a <see cref="Post"/> object for a provided origin
    /// and text content.
    /// </summary>
    /// <param name="origin">The origination of the post. Result is null if this is null.</param>
    /// <param name="text">The contents of the post. Result is null if this is null or empty.</param>
    /// <returns>A <see cref="Post"/> containing the post information.</returns>
    public static Post? Create(Origin? origin, string text)
    {
        if (origin == null) return null;
        if (string.IsNullOrEmpty(text)) return null;

        var voteLines = VoteParser.ExtractVoteLines(text);

        return new Post(origin, text, [.. voteLines]);
    }

    /// <summary>
    /// Create a <see cref="PostToProcess"/> object which encapsulates
    /// a <see cref="Post"/>.
    /// </summary>
    /// <param name="origin">The post's origin.</param>
    /// <param name="text">The text contents of the post.</param>
    /// <returns>A <see cref="PostToProcess"/>. Returns <c>null</c> if no post could be created.</returns>
    public static PostToProcess? CreateToProcess(Origin? origin, string text)
    {
        var post = Create(origin, text);
        if (post == null) return null;

        return new PostToProcess(post);
    }
}

/// <summary>
/// Extension methods for <see cref="Post"/> objects.
/// </summary>
public static class PostExtensions
{
    /// <summary>
    /// Determine if a post falls before the starting point of the tallied range.
    /// </summary>
    /// <param name="post">The post to check</param>
    /// <param name="threadRange">The range of posts examined in the thread.</param>
    /// <returns><c>True</c> if the post falls before the tally starting point.</returns>
    public static bool IsBeforeStart(this Post post, ThreadRange threadRange)
    {
        return threadRange switch
        {
            ThreadRangeById range => post.Origin.PostId.Value < range.PostId.Value,
            ThreadRangeByPosts range => post.Origin.PostNumber.Value < range.StartPostNumber,
            _ => throw new InvalidOperationException("Unknown ThreadRange type.")
        };
    }

    /// <summary>
    /// Determine if a post falls after the ending point of the tallied range.
    /// </summary>
    /// <param name="post">The post to check</param>
    /// <param name="quest">The quest being tallied</param>
    /// <param name="threadRange">The tally range</param>
    /// <returns><c>True</c> if the post falls after the tally ending point.</returns>
    public static bool IsAfterEnd(this Post post, ThreadRange threadRange)
    {
        return threadRange switch
        {
            ThreadRangeById => false,
            ThreadRangeByPosts range when range.EndPostNumber == 0 => false,
            ThreadRangeByPosts range => post.Origin.PostNumber.Value > range.EndPostNumber,
            _ => throw new InvalidOperationException("Unknown ThreadRange type.")
        };
    }

    /// <summary>
    /// Checks if a post matches a username filter in the given quest.
    /// </summary>
    /// <param name="post">The <see cref="Post"/> to examine.</param>
    /// <param name="quest">The <see cref="Quest"/> with the filter.</param>
    /// <returns><c>True</c> if the username filter matches. Otherwise <c>false</c>.</returns>
    public static bool MatchesUsernameFilter(this Post post, Quest quest)
    {
        return quest.UseCustomUsernameFilters && quest.UsernameFilter.Blocks(post.Origin.Author.Name);
    }

    /// <summary>
    /// Checks if a post matches a post number filter in the given quest.
    /// </summary>
    /// <param name="post">The <see cref="Post"/> to examine.</param>
    /// <param name="quest">The <see cref="Quest"/> with the filter.</param>
    /// <returns><c>True</c> if the post number filter matches. Otherwise <c>false</c>.</returns>
    public static bool MatchesPostNumberFilter(this Post post, Quest quest)
    {
        return quest.UseCustomPostFilters &&
            (quest.PostsFilter.Blocks(post.Origin.PostNumber.Value) ||
             quest.PostsFilter.Blocks(post.Origin.PostId.Value));
    }
}


/// <summary>
/// Comparer handler for <see cref="Post"/> and <see cref="PostToProcess"/>
/// </summary>
public class PostComparer : IEqualityComparer<Post>, IEqualityComparer<PostToProcess>
{
    /// <summary>
    /// Static instance of a <see cref="PostComparer"/>
    /// </summary>
    public static PostComparer Instance { get; } = new();

    public bool Equals(Post? x, Post? y)
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

    public int GetHashCode([DisallowNull] Post obj)
    {
        return OriginComparer.Instance.GetHashCode(obj.Origin);
    }

    public int GetHashCode([DisallowNull] PostToProcess obj)
    {
        return GetHashCode(obj.Post);
    }
}
