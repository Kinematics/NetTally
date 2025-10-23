using System.Diagnostics.CodeAnalysis;

namespace NetTally.Models;

public class OriginDetailComparer : IEqualityComparer<OriginDetail>, IComparer<OriginDetail>
{
    public static OriginDetailComparer Instance { get; } = new();

    public int Compare(OriginDetail? x, OriginDetail? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            (NoOriginDetail, NoOriginDetail) => 0,
            (NoOriginDetail, _) => 1,
            (_, NoOriginDetail) => -1,
            (OriginSource xs, OriginSource ys) => Compare(xs, ys),
            _ => throw new NotImplementedException($"Unknown OriginDetail type: {x.GetType()}, {y.GetType()}")
        };
    }

    private static int Compare(OriginSource x, OriginSource y)
    {
        int result = x.Thread.AbsoluteUri.CompareTo(y.Thread.AbsoluteUri);

        if (result == 0)
        {
            result = PostIdComparer.Instance.Compare(x.PostId, y.PostId);
        }

        return result;
    }

    public bool Equals(OriginDetail? x, OriginDetail? y)
    {
        return (x, y) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            (NoOriginDetail, NoOriginDetail) => true,
            (NoOriginDetail, _) => false,
            (_, NoOriginDetail) => false,
            _ => Compare(x, y) == 0
        };
    }

    public int GetHashCode([DisallowNull] OriginDetail obj)
    {
        return obj switch
        {
            NoOriginDetail n => n.GetHashCode(),
            OriginSource s => s.Permalink.GetHashCode(),
            _ => throw new NotImplementedException($"Unknown OriginDetail type: {obj.GetType()}")
        };
    }
}
