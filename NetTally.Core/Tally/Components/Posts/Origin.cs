using System.Diagnostics.CodeAnalysis;
using NetTally.Enums;
using NetTally.Utility;

namespace NetTally.Tally.Components.Posts;
public sealed record Origin(
    IdentityType Category,
    Author Author,
    Uri Thread,
    Uri Permalink,
    PostId PostId,
    int ThreadPostNumber,
    DateTimeOffset Timestamp,
    Origin? Source)
{
    public bool IsUser => Category == IdentityType.User;
    public bool IsPlan => Category == IdentityType.Plan;

    public override string ToString()
    {
        return $"{{Name: {Author.Name} ({Category}), {ThreadPostNumber} @ {Thread}";
    }
}

public static class Origins
{
    static readonly Uri ExampleUri = OriginComparer.ExampleUri;

    /// <summary>
    /// An empty origin.
    /// </summary>
    public static Origin None { get; } = new Origin(
        IdentityType.User,
        Authors.None,
        ExampleUri,
        ExampleUri,
        PostIds.Zero,
        0,
        DateTimeOffset.MinValue,
        null);

    /// <summary>
    /// Create an <see cref="Origin"/> object, fully defined.
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
    public static Origin? Create(
        IdentityType category,
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        int postNumber,
        DateTimeOffset timestamp,
        Origin source)
    {
        if (author == Authors.None)
            return null;

        if (postNumber < 1)
            postNumber = 0;

        return new Origin(category, author,
            thread, permalink, postId, postNumber, timestamp, source);
    }

    /// <summary>
    /// Create a simple <see cref="Origin"/> with only name and category values.
    /// </summary>
    /// <param name="category">The type of author.</param>
    /// <param name="author">The author for the origin.</param>
    /// <returns></returns>
    public static Origin? CreateOriginForName(
        IdentityType category,
        Author author)
    {
        return Create(category, author, ExampleUri, ExampleUri, PostIds.Zero,
            0, DateTimeOffset.MinValue, None);
    }

    /// <summary>
    /// Shortcut to create an <see cref="Origin"/> for a user.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="thread"></param>
    /// <param name="permalink"></param>
    /// <param name="postId"></param>
    /// <param name="postNumber"></param>
    /// <returns></returns>
    public static Origin? CreateUser(
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        int postNumber)
    {
        return Create(IdentityType.User, author, thread, permalink, postId, postNumber, DateTimeOffset.MinValue, None);
    }

    /// <summary>
    /// Shortcut to create an <see cref="Origin"/> for a user.
    /// Include timestamp.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="thread"></param>
    /// <param name="permalink"></param>
    /// <param name="postId"></param>
    /// <param name="postNumber"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static Origin? CreateUser(
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
    /// Create an <see cref="Origin"/> for a plan, using a user as a base.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="planName">The name of the plan to use.</param>
    /// <returns></returns>
    public static Origin? CreatePlanOrigin(Origin origin, string? planName)
    {
        if (planName == null)
            return null;

        Author author = Authors.Create(planName);

        return CreatePlanOrigin(origin, author);
    }

    /// <summary>
    /// Create an <see cref="Origin"/> for a plan, using a user as a base.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="plan">The <see cref="Author"/> for the plan.</param>
    /// <returns></returns>
    public static Origin? CreatePlanOrigin(Origin origin, Author plan)
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

public class OriginComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    public static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);
    public static OriginComparer Instance { get; } = new();

    public int Compare(Origin? x, Origin? y)
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

    public bool Equals(Origin? x, Origin? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}

public class OriginNameComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    public static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);
    public static OriginNameComparer Instance { get; } = new();

    public int Compare(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int result = x.Category.CompareTo(y.Category);

        if (result != 0) return result;

        return AuthorComparer.Instance.Compare(x.Author, y.Author);
    }

    public bool Equals(Origin? x, Origin? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}
