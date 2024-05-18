using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NetTally.Enums;

namespace NetTally.Tally.ComponentsF.Votes;

/// <summary>
/// Data type for a set of vote lines that can be processed as a block.
/// </summary>
/// <param name="Lines">The vote lines being tracked.</param>
/// <param name="Marker">The marker that the block as a whole has.</param>
/// <param name="Task">The task that the block as a whole has.</param>
public record VoteBlockType(List<VoteLineType> Lines, MarkerData Marker, VoteTaskType Task)
    : IEnumerable<VoteLineType>
{
    /// <summary>
    /// A mutable category (<see cref="MarkerType"/>) that this vote block can belong to.
    /// </summary>
    public MarkerType Category { get; set; } = MarkerType.None;

    public IEnumerator<VoteLineType> GetEnumerator()
    {
        if (Lines.Count == 0)
            yield break;

        var firstLine = Lines[0];

        yield return firstLine with { Marker = Marker, Task = Task };

        var remainingLines = Lines.Skip(1);

        foreach (var line in remainingLines)
            yield return line;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Static class to create and modify <see cref="VoteBlockType"/> objects.
/// </summary>
public static class VoteBlock
{
    /// <summary>
    /// A basic empty <see cref="VoteBlockType"/>.
    /// </summary>
    public static VoteBlockType Empty { get; } = new VoteBlockType([], Marker.Empty, VoteTask.Empty);

    /// <summary>
    /// Create a vote block with the given vote lines.
    /// </summary>
    /// <param name="lines">The vote lines to add to the vote block.</param>
    /// <returns>A new <see cref="VoteBlockType"/>, or null if there were no vote lines.</returns>
    public static VoteBlockType? Create(IEnumerable<VoteLineType> lines)
    {
        List<VoteLineType> listOfLines = lines.ToList();

        if (listOfLines.Count == 0)
        {
            return null;
        }

        return new VoteBlockType(listOfLines, listOfLines[0].Marker, listOfLines[0].Task);
    }

    /// <summary>
    /// Create a vote block with the given vote line.
    /// </summary>
    /// <param name="line">The line to add to the vote block.</param>
    /// <returns>A new <see cref="VoteBlockType"/></returns>
    public static VoteBlockType Create(VoteLineType line)
    {
        return new VoteBlockType([line], line.Marker, line.Task);
    }

    /// <summary>
    /// Create a vote block containing all the vote lines of the provided vote blocks.
    /// </summary>
    /// <param name="blocks">A collection of vote blocks that will be used as the source for this one.</param>
    /// <returns>A new <see cref="VoteBlockType"/>, or null if there were no vote lines.</returns>
    public static VoteBlockType? Create(IEnumerable<VoteBlockType> blocks)
    {
        var lines = blocks.SelectMany(x => x.Lines);
        return Create(lines);
    }

    /// <summary>
    /// Create a deep copy of the provided vote block.
    /// </summary>
    /// <param name="block">The vote block to copy.</param>
    /// <returns>A <see cref="VoteBlockType"/> will all the same lines, marker, and task as the original.</returns>
    public static VoteBlockType Clone(VoteBlockType block)
    {
        return new VoteBlockType([.. block.Lines], block.Marker, block.Task);
    }
}

/// <summary>
/// Display class for <see cref="VoteBlockType"/> objects.
/// </summary>
public static class VoteBlockDisplay
{
    public static string ToString(VoteBlockType block)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToOverrideString(a, block.Marker.MarkerSymbol, block.Task.Name)
                : VoteLineDisplay.ToString(a))
            .Aggregate((a, b) => $"{a}\n{b}");
    }

    public static string ToOutputString(VoteBlockType block, string? marker = null, string? subMarker = null)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToOutputString(a, marker, block.Task.Name)
                : VoteLineDisplay.ToOutputString(a, subMarker))
            .Aggregate((a, b) => $"{a}\n{b}");
    }

    public static string ToManageVotesString(VoteBlockType block)
    {
        return ToOutputString(block, string.Empty, string.Empty);
    }

    public static string ToComparableString(VoteBlockType block)
    {
        return block.Lines
            .Select((a, b) => b == 0
                ? VoteLineDisplay.ToComparableString(a, block.Task.Name)
                : VoteLineDisplay.ToComparableString(a))
            .Aggregate((a, b) => $"{a}\n{b}");
    }
}

/// <summary>
/// Comparer class for <see cref="VoteBlockType"/> objects.
/// </summary>
public class VoteBlockComparer : IEqualityComparer<VoteBlockType>, IComparer<VoteBlockType>
{
    public static VoteBlockComparer Instance { get; } = new();

    public int Compare(VoteBlockType? x, VoteBlockType? y)
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
            if (x.Lines.Count == y.Lines.Count)
            {
                //return MarkerComparer.Instance.Compare(x.Marker, y.Marker);
                return 0;
            }
            else
            {
                return x.Lines.Count.CompareTo(y.Lines.Count);
            }
        }

        var firstDiff = matches.First(m => m != 0);

        return firstDiff;
    }

    public bool Equals(VoteBlockType? x, VoteBlockType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteBlockType obj)
    {
        return VoteLineComparer.Instance.GetHashCode(obj.Lines[0]);
    }
}