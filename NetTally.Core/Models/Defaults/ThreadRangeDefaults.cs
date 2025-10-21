using NetTally.Models.Threads;

namespace NetTally.Models.Defaults;

public static class ThreadRangeDefaults
{
    extension(ThreadRange)
    {
        public static ThreadRange None => _none;
    }

    private static readonly ThreadRange _none =
        new ThreadRangeByPosts(0, 0, 20, 1);
}

