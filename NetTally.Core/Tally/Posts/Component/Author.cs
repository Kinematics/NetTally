using NetTally.Utility;

namespace NetTally.Tally.Posts.Component;

/// <summary>
/// Data type to store Author information.
/// </summary>
/// <param name="Name">The name of the author.</param>
public record Author(string Name)
{
    public static implicit operator string(Author author) => author.Name;
}

public static class PredefinedAuthors
{
    extension(Author)
    {
        public static Author None => _empty;
        public static Author Unknown => _unknown;
    }

    private static readonly Author _empty = new("");
    private static readonly Author _unknown = new(Strings.UnknownAuthor);
}
