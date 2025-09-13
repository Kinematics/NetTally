using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.Posts.Component;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Posts.Comparer;

/// <summary>
/// Comparer class for <see cref="Author"/> objects.
/// </summary>
public class AuthorComparer : IEqualityComparer<Author>, IComparer<Author>
{
    public static AuthorComparer Instance { get; } = new();

    public int Compare(Author? x, Author? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name);
    }

    public bool Equals(Author? x, Author? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name) == 0;
    }

    public int GetHashCode([DisallowNull] Author obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj);
    }
}

