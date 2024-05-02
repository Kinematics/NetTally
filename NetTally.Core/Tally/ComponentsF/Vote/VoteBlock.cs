using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.ComponentsF.Vote;
/// <summary>
/// Data type for a set of vote lines that can be processed as a block.
/// </summary>
/// <param name="Lines">The vote lines being tracked.</param>
/// <param name="Marker">The marker that the block as a whole has.</param>
/// <param name="Task">The task that the block as a whole has.</param>
public record VoteBlockType(List<VoteLineType> Lines, MarkerData Marker, VoteTaskType Task)
    : IEnumerable<VoteLineType>
{
    public IEnumerator<VoteLineType> GetEnumerator()
    {
        if (Lines.Count == 0)
            yield break;

        var firstLine = Lines[0];

        yield return VoteLine.WithMarkerAndTask(firstLine, Marker, Task);

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
    public static VoteBlockType? Create(IEnumerable<VoteLineType> lines)
    {
        List<VoteLineType> listOfLines = lines.ToList();

        if (listOfLines.Count == 0)
        {
            return null;
        }

        return new VoteBlockType(listOfLines, listOfLines[0].Marker, listOfLines[0].Task);
    }

    public static VoteBlockType? Create(IEnumerable<VoteBlockType> blocks)
    {
        var lines = blocks.SelectMany(x => x.Lines);
        return Create(lines);
    }

    public static VoteBlockType? Create(VoteLineType line)
    {
        return Create([line]);
    }

    public static VoteBlockType Clone(VoteBlockType block)
    {
        return new VoteBlockType([.. block.Lines], block.Marker, block.Task);
    }

    public static VoteBlockType WithTask(VoteBlockType block, VoteTaskType voteTask)
    {
        return block with { Task = voteTask };
    }

    public static VoteBlockType WithMarker(VoteBlockType block, MarkerData marker)
    {
        return block with { Marker = marker };
    }
}

/// <summary>
/// Comparer class for <see cref="VoteBlockType"/> objects.
/// </summary>
public class VoteBlockComparer : IEqualityComparer<VoteBlockType>, IComparer<VoteBlockType>
{
    static readonly VoteBlockComparer voteBlockComparer = new();
    public static int CompareWith(VoteBlockType? x, VoteBlockType? y) =>
        voteBlockComparer.Compare(x, y);
    public static bool AreEqual(VoteBlockType? x, VoteBlockType? y) =>
        voteBlockComparer.Equals(x, y);

    public int Compare(VoteBlockType? x, VoteBlockType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int compare = VoteTaskComparer.CompareWith(x.Task, y.Task);

        if (compare != 0) return compare;

        var zip = x.Lines.Zip(y, (a, b) => (X: a, Y: b));

        var matches = zip.Select(z => VoteLineComparer.CompareWith(z.X, z.Y));

        if (matches.All(m => m == 0))
        {
            if (x.Lines.Count == y.Lines.Count)
            {
                //return MarkerComparer.CompareWith(x.Marker, y.Marker);
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
        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteBlockType obj)
    {
        return VoteLineComparer.GetHashCodeFor(obj.Lines[0]);
    }
}