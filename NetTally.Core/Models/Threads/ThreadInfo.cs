using NetTally.Models.Posts;
using NetTally.Models.Threads;

namespace NetTally.Models.Threads;

public sealed record ThreadInfo(
    string Title,
    Author Author,
    ThreadRange ThreadRange);

public static class PredefinedThreadInfo
{
    extension(ThreadInfo)
    {
        public static ThreadInfo None => _none;
    }

    private static readonly ThreadInfo _none =
        new(string.Empty, Author.None, ThreadRange.None);
}
