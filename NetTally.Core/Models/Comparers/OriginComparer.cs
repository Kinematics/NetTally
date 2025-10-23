using System.Diagnostics.CodeAnalysis;

namespace NetTally.Models;

public class OriginComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    public static OriginComparer Instance { get; } = new();

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
            (UserOrigin, PlanOrigin) => -1,
            (PlanOrigin, UserOrigin) => 1,
            _ => CompareWithDetails(x, y)
        };

        static int CompareWithDetails(Origin x, Origin y)
        {
            int result = AuthorComparer.Instance.Compare(x.GetName(), y.GetName());

            if (result == 0)
            {
                var xDetails = x.GetDetails();
                var yDetails = y.GetDetails();

                if (xDetails is not NoOriginDetail &&  yDetails is not NoOriginDetail)
                    result = OriginDetailComparer.Instance.Compare(xDetails, yDetails);
            }

            return result;
        }
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
            _ => Compare(x, y) == 0
        };
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.GetName());
    }
}


