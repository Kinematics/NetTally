using System.Diagnostics.CodeAnalysis;

namespace NetTally.Models;

public class SourceComparer : IEqualityComparer<Source>, IComparer<Source>
{
    public static SourceComparer Instance { get; } = new();

    public int Compare(Source? x, Source? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            (NoSource, NoSource) => 0,
            (NoSource, _) => 1,
            (_, NoSource) => -1,
            (SourceLocation xs, SourceLocation ys) => Compare(xs, ys),
            _ => throw new NotImplementedException($"Unknown OriginDetail type: {x.GetType()}, {y.GetType()}")
        };
    }

    private static int Compare(SourceLocation x, SourceLocation y)
    {
        int result = x.Thread.AbsoluteUri.CompareTo(y.Thread.AbsoluteUri);

        if (result == 0)
        {
            result = PostIdComparer.Instance.Compare(x.PostId, y.PostId);
        }

        return result;
    }

    public bool Equals(Source? x, Source? y)
    {
        return (x, y) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            (NoSource, NoSource) => true,
            (NoSource, _) => false,
            (_, NoSource) => false,
            _ => Compare(x, y) == 0
        };
    }

    public int GetHashCode([DisallowNull] Source obj)
    {
        return obj switch
        {
            NoSource n => n.GetHashCode(),
            SourceLocation s => s.Permalink.GetHashCode(),
            _ => throw new NotImplementedException($"Unknown Source type: {obj.GetType()}")
        };
    }
}
