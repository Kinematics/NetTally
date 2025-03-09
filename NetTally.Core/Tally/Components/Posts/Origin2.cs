using System.Diagnostics.CodeAnalysis;
using NetTally.Utility;

namespace NetTally.Tally.Components.Posts;

public abstract record Origin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostId PostNumber, DateTimeOffset Timestamp)
{
    public bool IsUser => this is UserOrigin;
    public bool IsPlan => this is PlanOrigin;
}

public sealed record UserOrigin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostId PostNumber, DateTimeOffset Timestamp)
    : Origin(Author, Thread, Permalink, PostId, PostNumber, Timestamp);

public sealed record PlanOrigin(Origin Origin, Author PlanName) : Origin(Origin);

/// <summary>
/// Class for creating <see cref="Origin"/> objects.
/// </summary>
public static class Origins
{
    static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);

    public static Origin None { get; } = new UserOrigin(Authors.None,
        ExampleUri, ExampleUri, PostIds.Zero, PostIds.Zero, DateTimeOffset.MinValue);

    public static Origin? CreateUser(Author author, Uri thread, Uri permalink, PostId postId, PostId postNumber) =>
        CreateUser(author, thread, permalink, postId, postNumber, DateTimeOffset.MinValue);

    public static Origin? CreateUser(
        Author author,
        Uri thread,
        Uri permalink,
        PostId postId,
        PostId postNumber,
        DateTimeOffset timestamp)
    {
        if (author == Authors.None)
            return null;

        return new UserOrigin(author, thread, permalink, postId, postNumber, timestamp);
    }

    public static Origin CreatePlan(Origin origin, Author planName)
    {
        return new PlanOrigin(origin, planName);
    }

    public static Origin CreateUserNameOnly(Author author)
    {
        return None with { Author = author };
    }

    public static Origin CreatePlanNameOnly(Author planName)
    {
        return new PlanOrigin(None, planName);
    }
}

/// <summary>
/// Class containing mapping function for subclasses of <see cref="Origin"/> objects.
/// </summary>
public static class OriginMapping
{
    /// <summary>
    /// Map function that defines how to implement a function that can apply to different
    /// subclasses of <see cref="Origin"/>.
    /// </summary>
    /// <typeparam name="T">The function return type.</typeparam>
    /// <param name="origin">The <see cref="Origin"/> that this extension method applies to.</param>
    /// <param name="userMap">What to do if the <see cref="Origin"/> is a <see cref="UserOrigin"/></param>
    /// <param name="planMap">What to do if the <see cref="Origin"/> is a <see cref="PlanOrigin"/></param>
    /// <returns>The result of whichever function got applied.</returns>
    /// <exception cref="InvalidOperationException">Will trigger if another subclass is
    /// ever created, but this function hasn't been updated.</exception>
    public static T Map<T>(this Origin origin, Func<UserOrigin, T> userMap, Func<PlanOrigin, T> planMap) =>
        origin switch
        {
            UserOrigin userOrigin => userMap(userOrigin),
            PlanOrigin planOrigin => planMap(planOrigin),
            _ => throw new InvalidOperationException("Unknown Origin type.")
        };
}

/// <summary>
/// Extension methods for <see cref="Origin"/> objects.
/// </summary>
public static class OriginExtensions
{
    /// <summary>
    /// Get the appropriate <see cref="Author"/> based on the type of <see cref="Origin"/>.
    /// <see cref="UserOrigin"/> returns the Author. <see cref="PlanOrigin"/> returns the PlanName.
    /// </summary>
    /// <param name="origin"></param>
    /// <returns>The <see cref="Author"/> of the <see cref="Origin"/>.</returns>
    public static Author GetName(this Origin origin) => origin.Map(
        userOrigin => userOrigin.Author,
        planOrigin => planOrigin.PlanName);

    /// <summary>
    /// Gets the original <see cref="Origin"/> used as a basis for this one.
    /// Only applies to <see cref="PlanOrigin"/> objects. Otherwise returns <see cref="Origins.None"/>.
    /// </summary>
    /// <param name="origin">The Origin of the <see cref="Origin"/>, if any.</param>
    /// <returns></returns>
    public static Origin Source(this Origin origin) => origin.Map(
        userOrigin => Origins.None,
        planOrigin => planOrigin.Origin);

    /// <summary>
    /// Gets a formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.
    /// </summary>
    /// <param name="origin"></param>
    /// <returns>A formatted BBCode string containing the URL for the <see cref="Origin"/>'s author.</returns>
    public static string GetBBCodeLink(this Origin origin) => origin.Map(
        userOrigin => $"[url=\"{userOrigin.Permalink}\"]{userOrigin.Author.Name}[/url]",
        planOrigin => $"[url=\"{planOrigin.Permalink}\"]{Strings.PlanNameMarker}{planOrigin.PlanName.Name}[/url]");
}


public class OriginComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);
    public static OriginComparer Instance { get; } = new();

    public int Compare(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x is UserOrigin ^ y is UserOrigin)
        {
            return (x is PlanOrigin) ? -1 : 1;
        }

        int result = AuthorComparer.Instance.Compare(x.GetName(), y.GetName());

        if (result == 0)
        {
            if (x.Thread.AbsoluteUri != ExampleUri.AbsoluteUri &&
                y.Thread.AbsoluteUri != ExampleUri.AbsoluteUri)
            {
                result = x.Thread.AbsoluteUri.CompareTo(y.Thread.AbsoluteUri);

                if (result == 0)
                {
                    result = PostIdComparer.Instance.Compare(x.PostId, y.PostId);
                }
            }
        }

        return result;
    }

    public bool Equals(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}

public class OriginNameComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    public static OriginNameComparer Instance { get; } = new();

    public int Compare(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x is UserOrigin ^ y is UserOrigin)
        {
            return (x is PlanOrigin) ? -1 : 1;
        }

        return AuthorComparer.Instance.Compare(x.GetName(), y.GetName());
    }

    public bool Equals(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.GetName());
    }
}


