using System.Diagnostics.CodeAnalysis;

namespace NetTally.Models;

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