using System.Diagnostics.CodeAnalysis;

namespace NetTally.Models;

public class OriginNameComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    public static OriginNameComparer Instance { get; } = new();

    public int Compare(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x is UserOrigin ^ y is UserOrigin)
        {
            return x is PlanOrigin ? -1 : 1;
        }

        return AuthorComparer.Instance.Compare(x.GetName(), y.GetName());
    }

    public bool Equals(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.GetName());
    }
}


