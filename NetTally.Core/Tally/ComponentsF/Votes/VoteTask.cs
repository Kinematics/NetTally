using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NetTally.Utility;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.ComponentsF.Votes;
/// <summary>
/// Data type to store a vote task.
/// </summary>
/// <param name="Name">The name of the task.</param>
public record VoteTaskType(string Name);

/// <summary>
/// Static class for creating <see cref="VoteTaskType"/> objects.
/// </summary>
public static class VoteTask
{
    public static VoteTaskType Empty { get; } = new VoteTaskType("");

    public static VoteTaskType Create(string task)
    {
        if (string.IsNullOrWhiteSpace(task))
            return Empty;

        task = task.RemoveUnsafeCharacters().Trim();

        return new VoteTaskType(task);
    }
}

/// <summary>
/// Comparer class for <see cref="VoteTaskType"/> objects.
/// </summary>
public class VoteTaskComparer : IEqualityComparer<VoteTaskType>, IComparer<VoteTaskType>
{
    public static VoteTaskComparer Instance { get; } = new();

    public int Compare(VoteTaskType? x, VoteTaskType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name);
    }

    public bool Equals(VoteTaskType? x, VoteTaskType? y)
    {
        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteTaskType obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Name);
    }
}
