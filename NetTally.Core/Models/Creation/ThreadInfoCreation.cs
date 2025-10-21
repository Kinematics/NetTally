using NetTally.Configure;
using NetTally.Models.Creation;
using NetTally.Models.Defaults;
using NetTally.Models.Posts;
using NetTally.Models.Threads;

namespace NetTally.Models.Creation;

public static class ThreadInfoCreation
{
    extension(ThreadInfo)
    {
        public static ThreadInfo? Create(
            string? title,
            Author? author,
            ThreadRange? threadRange)
        {
            if (string.IsNullOrEmpty(title))
                title = Strings.UntitledThread;

            author ??= Author.Unknown;

            if (threadRange is null)
                return null;

            return new ThreadInfo(title, author, threadRange);
        }
    }
}

