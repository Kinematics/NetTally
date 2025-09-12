using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using NetTally.Enums;

namespace NetTally.Tally.Vote.Components;

/// <summary>
/// Data type for a set of vote lines that can be processed as a block.
/// </summary>
/// <param name="Lines">The vote lines being tracked.</param>
/// <param name="Marker">The marker that the block as a whole has.</param>
/// <param name="Task">The task that the block as a whole has.</param>
public record VoteBlock(ImmutableArray<VoteLine> Lines, Marker Marker, VoteTask Task)
    : IEnumerable<VoteLine>
{
    public int LineCount => Lines.Length;

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

/// <summary>
/// Extension class for the creation of <see cref="VoteBlock"/> objects.
/// </summary>
public static class VoteBlockCreation
{
    extension(VoteBlock)
    {
        public static VoteBlock Empty => _empty;

        /// <summary>
        /// Create a <see cref="VoteBlock"> with the given <see cref="VoteLine">s.
        /// </summary>
        /// <param name="lines">The vote lines to add to the vote block.</param>
        /// <returns>A new <see cref="VoteBlock"/></returns>
        public static VoteBlock Create(IEnumerable<VoteLine> lines)
        {
            List<VoteLine> listOfLines = [.. lines];

            if (listOfLines.Count == 0)
            {
                return VoteBlock.Empty;
            }

            return new VoteBlock([.. listOfLines],
                                     listOfLines[0].Marker,
                                     listOfLines[0].Task);
        }

        /// <summary>
        /// Create a <see cref="VoteBlock"> with the given <see cref="VoteLine">.
        /// </summary>
        /// <param name="line">The vote line to add to the vote block.</param>
        /// <returns>A new <see cref="VoteBlock"/></returns>
        public static VoteBlock Create(VoteLine line)
        {
            return new VoteBlock([line], line.Marker, line.Task);
        }

        /// <summary>
        /// Create a <see cref="VoteBlock"/> containing all the vote lines of
        /// the provided <see cref="VoteBlock"/>s.
        /// </summary>
        /// <param name="blocks">A collection of <see cref="VoteBlock"/>s that will
        /// be used as the source for this one.</param>
        /// <returns>A new <see cref="VoteBlock"/>.</returns>
        public static VoteBlock Create(IEnumerable<VoteBlock> blocks)
        {
            var lines = blocks.SelectMany(x => x.Lines);
            return Create(lines);
        }
    }

    extension (VoteBlock voteBlock)
    {
        /// <summary>
        /// Create a deep copy of the provided <see cref="VoteBlock"/>.
        /// </summary>
        /// <returns>A <see cref="VoteBlock"/> will all the same lines,
        /// marker, and task as the original.</returns>
        public VoteBlock Clone()
        {
            return new VoteBlock([.. voteBlock.Lines], voteBlock.Marker, voteBlock.Task);
        }
    }

    private static readonly VoteBlock _empty = new([], Marker.Empty, VoteTask.Empty);
}


/// <summary>
/// Display class for <see cref="VoteBlock"/> objects.
/// </summary>
public static class VoteBlockDisplay
{
    public static string ToString(VoteBlock block)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToOverrideString(a, block.Marker.Display(), block.Task.Name)
                : VoteLineDisplay.ToString(a))
            .Aggregate((a, b) => $"{a}\n{b}");
    }

    public static string ToOutputString(VoteBlock block, string? marker = null, string? subMarker = null)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToOutputString(a, marker, block.Task.Name)
                : VoteLineDisplay.ToOutputString(a, subMarker))
            .Aggregate((a, b) => $"{a}\n{b}");
    }

    public static string ToManageVotesString(VoteBlock block)
    {
        return ToOutputString(block, marker: "", subMarker: "");
    }

    public static string ToComparableString(VoteBlock block)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToComparableString(a, block.Task.Name)
                : VoteLineDisplay.ToComparableString(a))
            .Aggregate((a, b) => $"{a}\r\n{b}");
    }
}

/// <summary>
/// Comparer class for <see cref="VoteBlock"/> objects.
/// </summary>
public class VoteBlockComparer : IEqualityComparer<VoteBlock>, IComparer<VoteBlock>
{
    public static VoteBlockComparer Instance { get; } = new();

    public int Compare(VoteBlock? x, VoteBlock? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int compare = VoteTaskComparer.Instance.Compare(x.Task, y.Task);

        if (compare != 0) return compare;

        var zip = x.Lines.Zip(y, (a, b) => (X: a, Y: b));

        var matches = zip.Select(z => VoteLineComparer.Instance.Compare(z.X, z.Y));

        if (matches.All(m => m == 0))
        {
            if (x.LineCount == y.LineCount)
            {
                //return MarkerComparer.Instance.Compare(x.Marker, y.Marker);
                return 0;
            }
            else
            {
                return x.LineCount.CompareTo(y.LineCount);
            }
        }

        var firstDiff = matches.First(m => m != 0);

        return firstDiff;
    }

    public bool Equals(VoteBlock? x, VoteBlock? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteBlock obj)
    {
        return VoteLineComparer.Instance.GetHashCode(obj.Lines[0]);
    }
}