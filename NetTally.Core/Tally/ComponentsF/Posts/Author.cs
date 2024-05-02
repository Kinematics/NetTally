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
    public static AuthorType None { get; } = new("");

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
    private static readonly AuthorComparer authorComparer = new();

    public static bool AreEqual(AuthorType? x, AuthorType? y) => authorComparer.Equals(x, y);
    public static int CompareWith(AuthorType? x, AuthorType? y) => authorComparer.Compare(x, y);

    public int Compare(AuthorType? x, AuthorType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name);
    }

    public bool Equals(AuthorType? x, AuthorType? y)
    {
        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] AuthorType obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Name);
    }
}

