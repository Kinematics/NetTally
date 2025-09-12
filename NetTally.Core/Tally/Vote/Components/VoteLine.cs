namespace NetTally.Tally.Vote.Components;
/// <summary>
/// Data type for vote lines.
/// </summary>
/// <param name="Prefix">The prefix on the vote line.</param>
/// <param name="Marker">The voting marker.</param>
/// <param name="Task">The task assigned to the vote line.</param>
/// <param name="Content">The contents of the vote line.</param>
public record VoteLine(Prefix Prefix, Marker Marker, VoteTask Task, VoteContent Content)
{
    /// <summary>
    /// ToString implementation to make debugging easier, by formatting the
    /// data in an easy-to-read manner.
    /// </summary>
    public override string ToString()
    {
        return $"{{{Prefix.Indent}[{Marker.Display()}][{Task.Name}] {Content.CleanContent}}}";
    }
}
