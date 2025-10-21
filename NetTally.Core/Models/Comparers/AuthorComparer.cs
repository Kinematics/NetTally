using System.Diagnostics.CodeAnalysis;
using NetTally.Models.Behavior;
using NetTally.Models.Posts;
using NetTally.Utility.Comparers;

namespace NetTally.Models.Comparers;

/// <summary>
/// Comparer class for <see cref="Author"/> objects.
/// </summary>
public class AuthorComparer : IEqualityComparer<Author>, IComparer<Author>
{
    public static AuthorComparer Instance { get; } = new();

    public int Compare(Author? x, Author? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (NoAuthor, NoAuthor) => 0,
            (UnknownAuthor, UnknownAuthor) => 0,
            (NoAuthor, _) => -1,
            (_, NoAuthor) => 1,
            (UnknownAuthor, _) => -1,
            (_, UnknownAuthor) => 1,
            (NamedAuthor xa, NamedAuthor ya) => Agnostic.CaseInsensitiveComparer.Compare(xa.Name, ya.Name),
            _ => throw new NotImplementedException($"Unknown Author types: {x.GetType()}, {y.GetType()}")
        };
    }

    public bool Equals(Author? x, Author? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Author obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.DisplayName);
    }
}

