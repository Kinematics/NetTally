using NetTally.Utility;

namespace NetTally.Tally.Posts.Component;

/// <summary>
/// Data type for the author of a post.
/// </summary>
/// <param name="Name">The name of the author.</param>
public record Author(string Name);

public static class PredefinedAuthors
{
    extension(Author)
    {
        public static Author None => _none;
        public static Author Unknown => _unknown;
    }

    private static readonly Author _none = new("");
    private static readonly Author _unknown = new(Strings.UnknownAuthor);
}
