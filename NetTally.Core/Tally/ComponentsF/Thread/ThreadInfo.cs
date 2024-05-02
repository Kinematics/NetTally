using NetTally.Tally.ComponentsF.Post;

namespace NetTally.Tally.ComponentsF.Thread;

public record ThreadInfoType(string Title, AuthorType Author, int Pages);

public static class ThreadInfo
{
    public static ThreadInfoType? Create(string title, AuthorType? author, int pages)
    {
        if (string.IsNullOrEmpty(title))
            return null;

        author ??= Author.None;

        return new ThreadInfoType(title, author, pages);
    }
}
