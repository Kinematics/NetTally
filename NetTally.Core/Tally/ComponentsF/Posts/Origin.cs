using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NetTally.Data;
using NetTally.Enums;

namespace NetTally.Tally.ComponentsF.Posts;
public sealed record OriginType(
    IdentityType Category,
    AuthorType Author,
    Uri Thread,
    Uri Permalink,
    PostIdType PostId,
    int ThreadPostNumber,
    DateTimeOffset Timestamp,
    OriginType? Source)
{
    public bool IsUser => Category == IdentityType.User;
    public bool IsPlan => Category == IdentityType.Plan;
}

public static class Origin
{
    static readonly Uri ExampleUri = OriginComparer.ExampleUri;

    public static OriginType None { get; } = new OriginType(
        IdentityType.User,
        Author.None,
        ExampleUri,
        ExampleUri,
        PostId.Zero,
        0,
        DateTimeOffset.MinValue,
        null);

    public static OriginType? Create(
        IdentityType category,
        AuthorType author,
        Uri thread,
        Uri permalink,
        PostIdType postId,
        int postNumber,
        DateTimeOffset timestamp,
        OriginType source)
    {
        if (author == Author.None)
            return null;

        if (postNumber < 1)
            postNumber = 0;

        return new OriginType(category, author,
            thread, permalink, postId, postNumber, timestamp, source);
    }

    public static OriginType? CreateOriginForName(
        IdentityType category,
        AuthorType author)
    {
        return Create(category, author, ExampleUri, ExampleUri, PostId.Zero,
            0, DateTimeOffset.MinValue, None);
    }

    public static OriginType? CreateUser(
        AuthorType author,
        Uri thread,
        Uri permalink,
        PostIdType postId,
        int postNumber)
    {
        return Create(IdentityType.User, author, thread, permalink, postId, postNumber, DateTimeOffset.MinValue, None);
    }

    public static OriginType? CreateUser(
        AuthorType author,
        Uri thread,
        Uri permalink,
        PostIdType postId,
        int postNumber,
        DateTimeOffset timestamp)
    {
        return Create(IdentityType.User, author, thread, permalink, postId, postNumber, timestamp, None);
    }

    public static OriginType? CreatePlanOrigin(OriginType origin, string? planName)
    {
        if (planName == null)
            return null;

        AuthorType author = Author.Create(planName);

        return CreatePlanOrigin(origin, author);
    }

    public static OriginType? CreatePlanOrigin(OriginType origin, AuthorType plan)
    {
        if (origin.Category != IdentityType.User)
        {
            return null;
        }

        if (plan == Author.None)
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
    public static readonly Uri ExampleUri = new(StringData.ExampleHostUrl);
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
