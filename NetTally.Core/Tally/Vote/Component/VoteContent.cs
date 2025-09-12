namespace NetTally.Tally.Vote.Component;

/// <summary>
/// Data type for vote content.
/// </summary>
/// <param name="Content">The full content of a vote line.</param>
/// <param name="CleanContent">The content of a vote line with BBCode removed.</param>
public sealed record VoteContent(string Content, string CleanContent);

public static class VoteContentPredefined
{
    extension(VoteContent)
    {
        public static VoteContent Empty => _empty;
    }

    private static readonly VoteContent _empty = new("", "");
}
