using NetTally.Tally.ComponentsF.Posts;

namespace NetTally.Tally.ComponentsF.Threads;

public record ThreadInfoType(string Title, AuthorType Author, int Pages);

public static class ThreadInfo
{
    public static readonly ThreadInfoType None = new(string.Empty, Author.None, 0);

    public static ThreadInfoType? Create(string title, AuthorType? author, int pages)
    {
        if (string.IsNullOrEmpty(title))
            return null;

        author ??= Author.None;

        return new ThreadInfoType(title, author, pages);
    }
}
