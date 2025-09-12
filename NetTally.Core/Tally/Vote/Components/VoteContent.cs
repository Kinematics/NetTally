namespace NetTally.Tally.Vote.Components;

/// <summary>
/// Data type for vote content.
/// </summary>
/// <param name="Content">The full content of a vote line.</param>
/// <param name="CleanContent">The content of a vote line with BBCode removed.</param>
public sealed record VoteContent(string Content, string CleanContent);
