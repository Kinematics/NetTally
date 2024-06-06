using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Components.Posts;
public record PostIdType(long Id);

public static class PostId
{
    public static PostIdType Zero { get; } = new PostIdType(0);

    public static PostIdType Create(long id)
    {
        if (id < 1)
            return Zero;

        return new PostIdType(id);
    }

    public static PostIdType? Create(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (long.TryParse(id, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long idValue))
        {
            return idValue switch
            {
                > 0 => new PostIdType(idValue),
                _ => Zero
            };
        }

        return null;
    }
}

public class PostIdComparer : IEqualityComparer<PostIdType>, IComparer<PostIdType>
{
    public static PostIdComparer Instance { get; } = new();

    public int Compare(PostIdType? x, PostIdType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return x.Id.CompareTo(y.Id);
    }

    public bool Equals(PostIdType? x, PostIdType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] PostIdType obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Id);
    }
}

