using NetTally.Tally.Posts.Component;
using NetTally.Utility;

namespace NetTally.Tally.Threads;

public record ThreadInfo(
    string Title,
    Author Author,
    ThreadRange ThreadRange);

public static class ThreadInfos
{
    public static ThreadInfo None { get; } =
        new ThreadInfo(string.Empty, Author.None, ThreadRanges.None);

    public static ThreadInfo Create(
        string title,
        Author author,
        ThreadRange threadRange)
    {
        if (string.IsNullOrEmpty(title))
            title = Strings.UntitledThread;
        author ??= Author.Unknown;

        return new ThreadInfo(title, author, threadRange);
    }
}
