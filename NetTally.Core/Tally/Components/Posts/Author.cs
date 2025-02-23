using System.Diagnostics.CodeAnalysis;
using NetTally.Utility;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.Components.Posts;
/// <summary>
/// Data type to store Author information.
/// </summary>
/// <param name="Name">The name of the author.</param>
public record Author(string Name);

/// <summary>
/// Static class to handle creation methods for <see cref="Author"/> objects.
/// </summary>
public static class Authors
{
    public static Author None { get; } = new(string.Empty);
    public static Author Unknown { get; } = new(Strings.UnknownAuthor);

    public static Author Create(string name)
    {
        name = name.RemoveUnsafeCharacters().Trim();

        if (string.IsNullOrEmpty(name))
            return None;

        return new Author(name);
    }
}

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
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Author obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Name);
    }
}

