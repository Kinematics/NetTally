using System.Diagnostics.CodeAnalysis;
using NetTally.Models.Votes;
using NetTally.Utility.Comparers;

namespace NetTally.Models.Comparers;

/// <summary>
/// Comparer class for <see cref="VoteTask"/> objects.
/// </summary>
public class VoteTaskComparer : IEqualityComparer<VoteTask>, IComparer<VoteTask>
{
    public static VoteTaskComparer Instance { get; } = new();

    public int Compare(VoteTask? x, VoteTask? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Agnostic.CaseInsensitiveComparer.Compare(x.Name, y.Name);
    }

    public bool Equals(VoteTask? x, VoteTask? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteTask obj)
    {
        return Agnostic.CaseInsensitiveComparer.GetHashCode(obj.Name);
    }
}
