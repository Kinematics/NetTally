namespace NetTally.Models.Posts;

/// <summary>
/// A post identifier value.
/// </summary>
/// <param name="Value">The unique ID for a post on a forum.</param>
public sealed record PostId(long Value)
{
    public static implicit operator long(PostId postId) => postId.Value;
}

public static class PredefinedPostIds
{
    extension(PostId)
    {
        public static PostId Zero => _zero;
    }

    public static readonly PostId _zero = new(0);
}
