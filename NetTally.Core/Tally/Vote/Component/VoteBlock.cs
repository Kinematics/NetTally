using System.Collections;
using System.Collections.Immutable;
using NetTally.Enums;

namespace NetTally.Tally.Vote.Component;

/// <summary>
/// Data type for a set of vote lines that can be processed as a block.
/// </summary>
/// <param name="Lines">The vote lines being tracked.</param>
/// <param name="Marker">The marker that the block as a whole has.</param>
/// <param name="Task">The task that the block as a whole has.</param>
public record VoteBlock(ImmutableArray<VoteLine> Lines, Marker Marker, VoteTask Task)
    : IEnumerable<VoteLine>
{
    /// <summary>
    /// A mutable category (<see cref="MarkerType"/>) that this vote block can belong to.
    /// </summary>
    public MarkerType Category { get; set; } = MarkerType.None;

    public IEnumerator<VoteLine> GetEnumerator()
    {
        if (Lines.Length == 0)
            yield break;

        var firstLine = Lines[0];

        yield return firstLine with { Marker = Marker, Task = Task };

        var remainingLines = Lines.Skip(1);

        foreach (var line in remainingLines)
            yield return line;
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    public override string ToString() =>
        $"{{[{Marker.Display()}][{Task.Name}]||{Lines[0]}}}";

    public string ManageVotesDisplay =>
        VoteBlockDisplay.ToOutputString(this, marker: "", subMarker: "");
}
