using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Processing;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Posts.Comparer;

/// <summary>
/// Comparer handler for <see cref="Posts"/> and <see cref="PostToProcess"/>
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

        return object.Equals(x.Post, y.Post);
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
