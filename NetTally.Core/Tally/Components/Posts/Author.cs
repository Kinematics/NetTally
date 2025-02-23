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
    /// <summary>
    /// An empty <see cref="Author"/> object.
    /// </summary>
    public static Author None { get; } = new(string.Empty);

    /// <summary>
    /// An unknown <see cref="Author"/>.
    /// </summary>
    public static Author Unknown { get; } = new(Strings.UnknownAuthor);

    /// <summary>
    /// Create a new <see cref="Author"/> with the given name.
    /// </summary>
    /// <param name="name">The name of the author.</param>
    /// <returns>An <see cref="Author"/>. If no name is provided, returns <see cref="None"/></returns>
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
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name) == 0;
    }

    public int GetHashCode([DisallowNull] Author obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Name);
    }
}

