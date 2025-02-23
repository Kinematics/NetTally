using NetTally.Tally.Components.Posts;
using NetTally.Utility;

namespace NetTally.Tally.Components.Threads;

public record ThreadInformationType(
    string Title,
    AuthorType Author,
    ThreadRange ThreadRange);

public static class ThreadInformation
{
    public static ThreadInformationType None { get; } =
        new ThreadInformationType(string.Empty, Author.None, ThreadRanges.None);

    public static ThreadInformationType Create(
        string? title,
        AuthorType? author,
        ThreadRange threadRange)
    {
        if (string.IsNullOrEmpty(title))
            title = Strings.UntitledThread;
        author ??= Author.Unknown;

        return new ThreadInformationType(title, author, threadRange);
    }
}
