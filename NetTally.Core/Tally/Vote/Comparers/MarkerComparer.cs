using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.Vote.Components;

namespace NetTally.Tally.Vote.Comparers;

/// <summary>
/// Comparer class for <see cref="Marker"/> objects.
/// </summary>
public class MarkerComparer : IEqualityComparer<Marker>, IComparer<Marker>
{
    public static MarkerComparer Instance { get; } = new();

    public int Compare(Marker? x, Marker? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // MarkerType.None matches anything.
        if (x is NoMarker || y is NoMarker) return 0;

        // MarkerType.Plan should be ignored.
        if (x is PlanMarker || y is PlanMarker) return 0;

        if (x.Type == y.Type)
            return x.Value.CompareTo(y.Value);

        // Ranks should get sorted before other types.
        if (x is RankMarker) return -1;
        if (y is RankMarker) return 1;

        // Otherwise just compare the values.
        return x.Value.CompareTo(y.Value);
    }

    public bool Equals(Marker? x, Marker? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Marker obj)
    {
        return obj.Value.GetHashCode();
    }
}
