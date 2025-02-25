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

public static class OriginMapping
{
    public static T Map<T>(this Origin origin, Func<UserOrigin, T> userMap, Func<PlanOrigin, T> planMap) =>
        origin switch
        {
            UserOrigin userOrigin => userMap(userOrigin),
            PlanOrigin planOrigin => planMap(planOrigin),
            _ => throw new InvalidOperationException("Unknown Origin type.")
        };
}

public static class OriginExtensions
{
    public static Author GetName(this Origin origin) => origin.Map(
        userOrigin => userOrigin.Author,
        planOrigin => planOrigin.PlanName
        );

    public static Origin Source(this Origin origin) => origin.Map(
        userOrigin => Origins.None,
        planOrigin => planOrigin.Origin
        );

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


