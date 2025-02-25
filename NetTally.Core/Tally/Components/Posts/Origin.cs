using System.Diagnostics.CodeAnalysis;
using NetTally.Enums;
using NetTally.Utility;

namespace NetTally.Tally.Components.Posts;
public sealed record Origin1(
    IdentityType Category,
    Author Author,
    Uri Thread,
    Uri Permalink,
    PostId PostId,
    int ThreadPostNumber,
    DateTimeOffset Timestamp,
    Origin1? Source)
{
    public bool IsUser => Category == IdentityType.User;
    public bool IsPlan => Category == IdentityType.Plan;

    public override string ToString()
    {
        return $"{{Name: {Author.Name} ({Category}), {ThreadPostNumber} @ {Thread}";
    }
}

public static class Origins1
{
    static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);

    /// <summary>
    /// An empty Origin1.
    /// </summary>
    public static Origin1 None { get; } = new Origin1(
        IdentityType.User,
        Authors.None,
        ExampleUri,
        ExampleUri,
        PostIds.Zero,
        0,
        DateTimeOffset.MinValue,
        null);

    /// <summary>
    /// Create an <see cref="Origin1"/> object, fully defined.
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
    public static Origin1? Create(
        IdentityType category,
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        int postNumber,
        DateTimeOffset timestamp,
        Origin1 source)
    {
        if (author == Authors.None)
            return null;

        if (postNumber < 1)
            postNumber = 0;

        return new Origin1(category, author,
            thread, permalink, postId, postNumber, timestamp, source);
    }

    /// <summary>
    /// Create a simple <see cref="Origin1"/> with only name and category values.
    /// </summary>
    /// <param name="category">The type of author.</param>
    /// <param name="author">The author for the Origin1.</param>
    /// <returns></returns>
    public static Origin1? CreateOriginForName(
        IdentityType category,
        Author author)
    {
        return Create(category, author, ExampleUri, ExampleUri, PostIds.Zero,
            0, DateTimeOffset.MinValue, None);
    }

    /// <summary>
    /// Shortcut to create an <see cref="Origin1"/> for a user.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="thread"></param>
    /// <param name="permalink"></param>
    /// <param name="postId"></param>
    /// <param name="postNumber"></param>
    /// <returns></returns>
    public static Origin1? CreateUser(
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        int postNumber)
    {
        return Create(IdentityType.User, author, thread, permalink, postId, postNumber, DateTimeOffset.MinValue, None);
    }

    /// <summary>
    /// Shortcut to create an <see cref="Origin1"/> for a user.
    /// Include timestamp.
    /// </summary>
    /// <param name="author"></param>
    /// <param name="thread"></param>
    /// <param name="permalink"></param>
    /// <param name="postId"></param>
    /// <param name="postNumber"></param>
    /// <param name="timestamp"></param>
    /// <returns></returns>
    public static Origin1? CreateUser(
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
    /// Create an <see cref="Origin1"/> for a plan, using a user as a base.
    /// </summary>
    /// <param name="Origin1"></param>
    /// <param name="planName">The name of the plan to use.</param>
    /// <returns></returns>
    public static Origin1? CreatePlanOrigin1(Origin1 Origin1, string? planName)
    {
        if (planName == null)
            return null;

        Author author = Authors.Create(planName);

        return CreatePlanOrigin1(Origin1, author);
    }

    /// <summary>
    /// Create an <see cref="Origin1"/> for a plan, using a user as a base.
    /// </summary>
    /// <param name="Origin1"></param>
    /// <param name="plan">The <see cref="Author"/> for the plan.</param>
    /// <returns></returns>
    public static Origin1? CreatePlanOrigin1(Origin1 Origin1, Author plan)
    {
        if (Origin1.Category != IdentityType.User)
        {
            return null;
        }

        if (plan == Authors.None)
        {
            return null;
        }

        return Create(IdentityType.Plan, plan,
            Origin1.Thread, Origin1.Permalink, Origin1.PostId, Origin1.ThreadPostNumber,
            Origin1.Timestamp, Origin1);
    }
}

public class OriginComparer1 : IEqualityComparer<Origin1>, IComparer<Origin1>
{
    public static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);
    public static OriginComparer1 Instance { get; } = new OriginComparer1();

    public int Compare(Origin1? x, Origin1? y)
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

    public bool Equals(Origin1? x, Origin1? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin1 obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}

public class OriginNameComparer1 : IEqualityComparer<Origin1>, IComparer<Origin1>
{
    public static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);
    public static OriginNameComparer1 Instance { get; } = new();

    public int Compare(Origin1? x, Origin1? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int result = x.Category.CompareTo(y.Category);

        if (result != 0) return result;

        return AuthorComparer.Instance.Compare(x.Author, y.Author);
    }

    public bool Equals(Origin1? x, Origin1? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin1 obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}
