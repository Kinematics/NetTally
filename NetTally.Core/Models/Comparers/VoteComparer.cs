using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.Processing;

namespace NetTally.Models.Comparers;

public class VoteComparer : IEqualityComparer<Vote>, IEqualityComparer<VoteToProcess>
{
    public static VoteComparer Instance { get; } = new();

    public bool Equals(Vote? x, Vote? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return OriginComparer.Instance.Equals(x.Origin, y.Origin) &&
            x.VoteLines.SequenceEqual(y.VoteLines, VoteLineComparer.Instance);
    }

    public bool Equals(VoteToProcess? x, VoteToProcess? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Equals(x.Vote, y.Vote);
    }

    public int GetHashCode([DisallowNull] Vote obj)
    {
        return OriginComparer.Instance.GetHashCode(obj.Origin);
    }

    public int GetHashCode([DisallowNull] VoteToProcess obj)
    {
        return GetHashCode(obj.Vote);
    }
}
