using System.Diagnostics.CodeAnalysis;

namespace NetTally.Models;

/// <summary>
/// Comparer class for <see cref="VoteLine"/> objects.
/// </summary>
public class VoteLineComparer : IEqualityComparer<VoteLine>, IComparer<VoteLine>
{
    public static VoteLineComparer Instance { get; } = new();

    public int Compare(VoteLine? x, VoteLine? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int compare = VoteTaskComparer.Instance.Compare(x.Task, y.Task);

        if (compare != 0) return compare;

        return VoteContentComparer.Instance.Compare(x.Content, y.Content);
    }

    public bool Equals(VoteLine? x, VoteLine? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteLine obj)
    {
        return VoteContentComparer.Instance.GetHashCode(obj.Content);
    }
}
