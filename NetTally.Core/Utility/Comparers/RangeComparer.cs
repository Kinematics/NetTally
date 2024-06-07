using System.Diagnostics.CodeAnalysis;
using NetTally.Tally.Components.Posts;

namespace NetTally.Utility.Comparers;
public class RangeComparer : IEqualityComparer<Range>
{
    public static readonly RangeComparer Instance = new();

    public bool Equals(Range x, Range y)
    {
        return (x.Start.Value <= y.Start.Value && x.End.Value >= y.End.Value) ||
            (y.Start.Value <= x.Start.Value && y.End.Value >= x.End.Value);
    }

    public int GetHashCode([DisallowNull] Range obj)
    {
        return obj.GetHashCode();
    }
}

public static class RangeConverterExt
{
    public static Range AsRange(this int value) => new(value, value);
    public static Range AsRange(this PostIdType value) => new((int)value.Id, (int)value.Id);
}
