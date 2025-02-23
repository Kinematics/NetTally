using NetTally.Tally.Components.Posts;
using NetTally.Utility;

namespace NetTally.Tally.Components.Threads;

public record ThreadInfo(
    string Title,
    AuthorType Author,
    ThreadRange ThreadRange);

public static class ThreadInfos
{
    public static ThreadInfo None { get; } =
        new ThreadInfo(string.Empty, Author.None, ThreadRanges.None);

    public static ThreadInfo Create(
        string? title,
        AuthorType? author,
        ThreadRange threadRange)
    {
        if (string.IsNullOrEmpty(title))
            title = Strings.UntitledThread;
        author ??= Author.Unknown;

        return new ThreadInfo(title, author, threadRange);
    }
}
