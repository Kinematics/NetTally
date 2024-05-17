using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NetTally.Tally.ComponentsF.Votes;
/// <summary>
/// Data type for vote lines.
/// </summary>
/// <param name="Prefix">The prefix on the vote line.</param>
/// <param name="Marker">The voting marker.</param>
/// <param name="Task">The task assigned to the vote line.</param>
/// <param name="Content">The contents of the vote line.</param>
public record VoteLineType(PrefixType Prefix, MarkerData Marker, VoteTaskType Task, VoteContentType Content)
{
    public int Depth => Prefix.Depth;
    public bool HasTask => Task.Name.Length > 0;
}

/// <summary>
/// Static class for creating and modifying <see cref="VoteLineType"/> objects.
/// </summary>
public static class VoteLine
{
    public static VoteLineType Empty { get; } =
        new VoteLineType(Prefix.Empty, Marker.Empty, VoteTask.Empty, VoteContent.Empty);

    public static VoteLineType? Create(
        PrefixType? prefix,
        MarkerData? marker,
        VoteTaskType? task,
        VoteContentType? content)
    {
        if (prefix == null) return null;
        if (marker == null) return null;
        if (task == null) return null;
        if (content == null) return null;

        if (content == VoteContent.Empty) return null;

        return new VoteLineType(prefix, marker, task, content);
    }


    public static VoteLineType Promote(VoteLineType input, int promoteDepth = 1)
    {
        return input with { Prefix = Prefix.Reduce(input.Prefix, promoteDepth) };
    }

    public static VoteLineType FullPromote(VoteLineType input)
    {
        if (input.Depth > 0)
            return input with { Prefix = Prefix.Empty };

        return input;
    }
}

/// <summary>
/// Comparer class for <see cref="VoteLineType"/> objects.
/// </summary>
public class VoteLineComparer : IEqualityComparer<VoteLineType>, IComparer<VoteLineType>
{
    public static VoteLineComparer Instance { get; } = new();

    public int Compare(VoteLineType? x, VoteLineType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int compare = VoteTaskComparer.Instance.Compare(x.Task, y.Task);

        if (compare != 0) return compare;

        return VoteContentComparer.Instance.Compare(x.Content, y.Content);
    }

    public bool Equals(VoteLineType? x, VoteLineType? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteLineType obj)
    {
        return VoteContentComparer.Instance.GetHashCode(obj.Content);
    }
}
