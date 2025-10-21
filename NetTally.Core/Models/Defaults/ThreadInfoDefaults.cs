using NetTally.Models.Defaults;
using NetTally.Models.Posts;
using NetTally.Models.Threads;

namespace NetTally.Models.Defaults;

public static class ThreadInfoDefaults
{
    extension(ThreadInfo)
    {
        public static ThreadInfo None => _none;
    }

    private static readonly ThreadInfo _none =
        new(string.Empty, Author.None, ThreadRange.None);
}
