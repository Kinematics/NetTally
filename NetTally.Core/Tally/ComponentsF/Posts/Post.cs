using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.ComponentsF.Posts;
public record PostType(OriginType Origin, string Text, ImmutableArray<VoteLineType> VoteLines)
{
    public bool HasVote => VoteLines.Length > 0;

    public int VoteLineCount => VoteLines.Length;
}

public static class Post
{
    public static PostType? Create(OriginType? origin, string text)
    {
        if (origin == null) return null;
        if (string.IsNullOrEmpty(text)) return null;

        var voteLines = VoteParser.ExtractVoteLines(text);

        return new PostType(origin, text, [.. voteLines]);
    }

    public static PostToProcess? CreateToProcess(OriginType origin, string text)
    {
        var post = Create(origin, text);
        if (post == null) return null;

        return new PostToProcess(post);
    }
}

public class PostComparer : IEqualityComparer<PostType>, IEqualityComparer<PostToProcess>
{
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
