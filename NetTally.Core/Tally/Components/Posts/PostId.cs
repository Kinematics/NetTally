using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Components.Posts;
public record PostId(long Id);

public static class PostIds
{
    public static PostId Zero { get; } = new PostId(0);

    public static PostId Create(long id)
    {
        if (id < 1)
            return Zero;

        return new PostId(id);
    }

    public static PostId? Create(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (long.TryParse(id, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long idValue))
        {
            return idValue switch
            {
                > 0 => new PostId(idValue),
                _ => Zero
            };
        }

        return null;
    }
}

public class PostIdComparer : IEqualityComparer<PostId>, IComparer<PostId>
{
    public static PostIdComparer Instance { get; } = new();

    public int Compare(PostId? x, PostId? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return x.Id.CompareTo(y.Id);
    }

    public bool Equals(PostId? x, PostId? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] PostId obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Id);
    }
}

