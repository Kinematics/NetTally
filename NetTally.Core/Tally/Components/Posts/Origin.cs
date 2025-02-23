using System.Diagnostics.CodeAnalysis;
using NetTally.Enums;
using NetTally.Utility;

namespace NetTally.Tally.Components.Posts;
public sealed record OriginType(
    IdentityType Category,
    Author Author,
    Uri Thread,
    Uri Permalink,
    PostId PostId,
    int ThreadPostNumber,
    DateTimeOffset Timestamp,
    OriginType? Source)
{
    public bool IsUser => Category == IdentityType.User;
    public bool IsPlan => Category == IdentityType.Plan;

    public override string ToString()
    {
        return $"{{Name: {Author.Name} ({Category}), {ThreadPostNumber} @ {Thread}";
    }
}

public static class Origin
{
    static readonly Uri ExampleUri = OriginComparer.ExampleUri;

    /// <summary>
    /// An empty origin.
    /// </summary>
    public static OriginType None { get; } = new OriginType(
        IdentityType.User,
        Authors.None,
        ExampleUri,
        ExampleUri,
        PostIds.Zero,
        0,
        DateTimeOffset.MinValue,
        null);

    /// <summary>
    /// Create an <see cref="OriginType"/> object, fully defined.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="author"></param>
    /// <param name="thread"></param>
    /// <param name="permalink"></param>
    /// <param name="postId"></param>
    /// <param name="postNumber"></param>
    /// <param name="timestamp"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static OriginType? Create(
        IdentityType category,
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        int postNumber,
        DateTimeOffset timestamp,
        OriginType source)
    {
        if (author == Authors.None)
            return null;

        if (postNumber < 1)
            postNumber = 0;

        return new OriginType(category, author,
            thread, permalink, postId, postNumber, timestamp, source);
    }

    /// <summary>
    /// Create a simple <see cref="OriginType"/> with only name and category values.
    /// </summary>
    /// <param name="category">The type of author.</param>
    /// <param name="author">The author for the origin.</param>
    /// <returns></returns>
    public static OriginType? CreateOriginForName(
        IdentityType category,
        Author author)
    {
        return Create(category, author, ExampleUri, ExampleUri, PostIds.Zero,
            0, DateTimeOffset.MinValue, None);
    }

    /// <summary>
    /// Shortcut to create an <see cref="OriginType"/> for a user.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="thread"></param>
    /// <param name="permalink"></param>
    /// <param name="postId"></param>
    /// <param name="postNumber"></param>
    /// <returns></returns>
    public static OriginType? CreateUser(
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        int postNumber)
    {
        return Create(IdentityType.User, author, thread, permalink, postId, postNumber, DateTimeOffset.MinValue, None);
    }

    /// <summary>
    /// Shortcut to create an <see cref="OriginType"/> for a user.
    /// Include timestamp.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="thread"></param>
    /// <param name="permalink"></param>
    /// <param name="postId"></param>
    /// <param name="postNumber"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static OriginType? CreateUser(
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        int postNumber,
        DateTimeOffset timestamp)
    {
        return Create(IdentityType.User, author, thread, permalink, postId, postNumber, timestamp, None);
    }

    /// <summary>
    /// Create an <see cref="OriginType"/> for a plan, using a user as a base.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="planName">The name of the plan to use.</param>
    /// <returns></returns>
    public static OriginType? CreatePlanOrigin(OriginType origin, string? planName)
    {
        if (planName == null)
            return null;

        Author author = Authors.Create(planName);

        return CreatePlanOrigin(origin, author);
    }

    /// <summary>
    /// Create an <see cref="OriginType"/> for a plan, using a user as a base.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="plan">The <see cref="Author"/> for the plan.</param>
    /// <returns></returns>
    public static OriginType? CreatePlanOrigin(OriginType origin, Author plan)
    {
        if (origin.Category != IdentityType.User)
        {
            return null;
        }

        if (plan == Authors.None)
        {
            return null;
        }

        return Create(IdentityType.Plan, plan,
            origin.Thread, origin.Permalink, origin.PostId, origin.ThreadPostNumber,
            origin.Timestamp, origin);
    }
}

public class OriginComparer : IEqualityComparer<OriginType>, IComparer<OriginType>
{
    public static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);
    public static OriginComparer Instance { get; } = new();

    public int Compare(OriginType? x, OriginType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int result = x.Category.CompareTo(y.Category);

        if (result != 0) return result;

        result = AuthorComparer.Instance.Compare(x.Author, y.Author);

        if (result != 0) return result;

        if (x.Thread.AbsoluteUri != ExampleUri.AbsoluteUri &&
            y.Thread.AbsoluteUri != ExampleUri.AbsoluteUri)
        {
            result = x.Thread.AbsoluteUri.CompareTo(y.Thread.AbsoluteUri);

            if (result != 0) return result;

            result = PostIdComparer.Instance.Compare(x.PostId, y.PostId);

            if (result != 0) return result;
        }

        return 0;
    }

    public bool Equals(OriginType? x, OriginType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] OriginType obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}

public class OriginNameComparer : IEqualityComparer<OriginType>, IComparer<OriginType>
{
    public static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);
    public static OriginNameComparer Instance { get; } = new();

    public int Compare(OriginType? x, OriginType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int result = x.Category.CompareTo(y.Category);

        if (result != 0) return result;

        return AuthorComparer.Instance.Compare(x.Author, y.Author);
    }

    public bool Equals(OriginType? x, OriginType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] OriginType obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}
