using NetTally.Models.Posts;

namespace NetTally.Models.Defaults;

public static class AuthorDefaults
{
    extension(Author)
    {
        public static Author None => _none;
        public static Author Unknown => _unknown;
    }

    private static readonly Author _none = new NoAuthor();
    private static readonly Author _unknown = new UnknownAuthor();
}
