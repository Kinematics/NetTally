using NetTally.Configure;

namespace NetTally.Models;

/// <summary>
/// Provides factory methods for creating instances of the ThreadInfo class.
/// </summary>
/// <remarks>This static class contains extension methods that simplify the creation of ThreadInfo objects by
/// handling default values for missing or null parameters. Use these methods to ensure consistent initialization of
/// thread metadata throughout the application.</remarks>
public static class ThreadInfoCreation
{
    extension(ThreadInfo)
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ThreadInfo"/> class using the specified title, author, and thread
        /// range.
        /// </summary>
        /// <param name="title">The title of the thread. If <c>null</c> or empty, a default title is used.</param>
        /// <param name="author">The author of the thread. If <c>null</c>, an unknown author is assigned.</param>
        /// <param name="threadRange">The range of the thread. Must not be <c>null</c>; otherwise, the method returns <c>null</c>.</param>
        /// <returns>A <see cref="ThreadInfo"/> instance initialized with the provided values, or <c>null</c> if <paramref
        /// name="threadRange"/> is <c>null</c>.</returns>
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

