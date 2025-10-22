using System.Diagnostics.CodeAnalysis;
using NetTally.Configure;

namespace NetTally.Models;

public class OriginComparer : IEqualityComparer<Origin>, IComparer<Origin>
{
    public static OriginComparer Instance { get; } = new();

    public int Compare(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x is UserOrigin ^ y is UserOrigin)
        {
            return x is PlanOrigin ? -1 : 1;
        }

        int result = AuthorComparer.Instance.Compare(x.GetName(), y.GetName());

        if (result == 0)
        {
            if (x.Thread.AbsoluteUri != Strings.ExampleUri.AbsoluteUri &&
                y.Thread.AbsoluteUri != Strings.ExampleUri.AbsoluteUri)
            {
                result = x.Thread.AbsoluteUri.CompareTo(y.Thread.AbsoluteUri);

                if (result == 0)
                {
                    result = PostIdComparer.Instance.Compare(x.PostId, y.PostId);
                }
            }
        }

        return result;
    }

    public bool Equals(Origin? x, Origin? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Origin obj)
    {
        return AuthorComparer.Instance.GetHashCode(obj.Author);
    }
}


