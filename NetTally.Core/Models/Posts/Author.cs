namespace NetTally.Models.Posts;

public abstract record Author();
public sealed record NamedAuthor(string Name) : Author;
public sealed record UnknownAuthor() : Author;
public sealed record NoAuthor() : Author;

public static class PredefinedAuthors
{
    extension(Author)
    {
        public static Author None => _none;
        public static Author Unknown => _unknown;
    }

    private static readonly Author _none = new NoAuthor();
    private static readonly Author _unknown = new UnknownAuthor();
}
