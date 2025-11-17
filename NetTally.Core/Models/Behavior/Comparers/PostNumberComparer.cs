namespace NetTally.Models;

public class PostNumberComparer : IComparer<PostNumber>
{
    public static PostNumberComparer Instance { get; } = new();

    public int Compare(PostNumber? x, PostNumber? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return x.Value.CompareTo(y.Value);
    }
}
