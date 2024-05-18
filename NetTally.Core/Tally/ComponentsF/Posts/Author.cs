using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NetTally.Utility;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.ComponentsF.Posts;
/// <summary>
/// Data type to store Author information.
/// </summary>
/// <param name="Name">The name of the author.</param>
public record AuthorType(string Name);

/// <summary>
/// Static class to handle creation methods for <see cref="AuthorType"/> objects.
/// </summary>
public static class Author
{
    public static AuthorType None { get; } = new(string.Empty);
    public static AuthorType Unknown { get; } = new(Strings.UnknownAuthor);

    public static AuthorType Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return None;

        name = name.RemoveUnsafeCharacters().Trim();

        return new AuthorType(name);
    }
}

/// <summary>
/// Comparer class for <see cref="AuthorType"/> objects.
/// </summary>
public class AuthorComparer : IEqualityComparer<AuthorType>, IComparer<AuthorType>
{
    public static AuthorComparer Instance { get; } = new();

    public int Compare(AuthorType? x, AuthorType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name);
    }

    public bool Equals(AuthorType? x, AuthorType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] AuthorType obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Name);
    }
}

