using NetTally.Tally.Components.Posts;
using NetTally.Utility;

namespace NetTally.Tally.Components.Threads;

public record ThreadInformationType(
    string Title,
    AuthorType Author,
    ThreadRange PostRange);

public static class ThreadInformation
{
    public static ThreadInformationType None { get; } =
        new ThreadInformationType(string.Empty, Author.None, PostRanges.None);

    public static ThreadInformationType Create(
        string? title,
        AuthorType? author,
        ThreadRange postRange)
    {
        if (string.IsNullOrEmpty(title))
            title = Strings.UntitledThread;
        author ??= Author.Unknown;

        return new ThreadInformationType(title, author, postRange);
    }
}
