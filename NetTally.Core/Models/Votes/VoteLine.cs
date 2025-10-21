using System.Diagnostics;
using NetTally.Models.Behavior;

namespace NetTally.Models.Votes;
/// <summary>
/// Data type for vote lines.
/// </summary>
/// <param name="Prefix">The prefix on the vote line.</param>
/// <param name="Marker">The voting marker.</param>
/// <param name="Task">The task assigned to the vote line.</param>
/// <param name="Content">The contents of the vote line.</param>
[DebuggerDisplay("{DebugDisplayString}")]
public sealed record VoteLine(Prefix Prefix, Marker Marker, VoteTask Task, VoteContent Content)
{
    private string DebugDisplayString => $"{{ {Prefix.Indent}[{Marker.Display()}][{Task.Name}] {Content.CleanContent} }}";
}
