using NetTally.Tally.Components.Posts;
using NetTally.Utility;

namespace NetTally.Tally.Components.Threads;

public record ThreadInfo(
    string Title,
    Author Author,
    ThreadRange ThreadRange);

public static class ThreadInfos
{
    public static ThreadInfo None { get; } =
        new ThreadInfo(string.Empty, Authors.None, ThreadRanges.None);

    public static ThreadInfo Create(
        string title,
        Author author,
        ThreadRange threadRange)
    {
        if (string.IsNullOrEmpty(title))
            title = Strings.UntitledThread;
        author ??= Authors.Unknown;

        return new ThreadInfo(title, author, threadRange);
    }
}
