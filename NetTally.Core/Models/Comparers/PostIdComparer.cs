using NetTally.Models.Posts;

namespace NetTally.Models.Comparers;

public class PostIdComparer : IComparer<PostId>
{
    public static PostIdComparer Instance { get; } = new();

    public int Compare(PostId? x, PostId? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return x.Value.CompareTo(y.Value);
    }
}
