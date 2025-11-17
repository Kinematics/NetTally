using System.Diagnostics.CodeAnalysis;

namespace NetTally.Models;

public class OriginNameComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    public static OriginNameComparer Instance { get; } = new();

    public int Compare(Origin? x, Origin? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            (NoOrigin, NoOrigin) => 0,
            (NoOrigin, _) => 1,
            (_, NoOrigin) => -1,
            (PlanOrigin, UserOrigin) => -1,
            (UserOrigin, PlanOrigin) => 1,
            _ => AuthorComparer.Instance.Compare(x.Name, y.Name)
        };
    }

    public bool Equals(Origin? x, Origin? y)
    {
        return (x, y) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            (NoOrigin, NoOrigin) => true,
            (NoOrigin, _) => false,
            (_, NoOrigin) => false,
            (PlanOrigin, UserOrigin) => false,
            (UserOrigin, PlanOrigin) => false,
            _ => Compare(x, y) == 0
        };
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Name);
    }
}


