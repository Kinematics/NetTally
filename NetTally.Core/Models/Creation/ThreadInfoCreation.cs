using NetTally.Configure;

namespace NetTally.Models;

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

