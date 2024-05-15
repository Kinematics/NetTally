using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.ComponentsF.Posts;
public record PostType(OriginType Origin, string Text, List<VoteLineType> VoteLines)
{
    public bool HasVote => VoteLines.Count > 0;
}

public static class Post
{
    public static PostType? Create(OriginType? origin, string text)
    {
        if (origin == null) return null;
        if (string.IsNullOrEmpty(text)) return null;

        var voteLines = VoteParser.ExtractVoteLines(text);

        return new PostType(origin, text, voteLines);
    }
}

public class PostComparer : IEqualityComparer<PostType>
{
    public static PostComparer Instance { get; } = new();

    public bool Equals(PostType? x, PostType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return OriginComparer.Instance.Equals(x.Origin, y.Origin) &&
            Agnostic.InsensitiveComparer.Equals(x.Text, y.Text);
    }

    public int GetHashCode([DisallowNull] PostType obj)
    {
        return OriginComparer.Instance.GetHashCode(obj.Origin);
    }
}
