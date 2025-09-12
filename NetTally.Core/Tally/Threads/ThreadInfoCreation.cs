using NetTally.Tally.Posts.Component;
using NetTally.Utility;

namespace NetTally.Tally.Threads;

public static class ThreadInfoCreation
{
    extension(ThreadInfo)
    {
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
}

