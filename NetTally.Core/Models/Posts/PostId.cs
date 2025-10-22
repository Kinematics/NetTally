namespace NetTally.Models;

/// <summary>
/// A post identifier value.
/// </summary>
/// <param name="Value">The unique ID for a post on a forum.</param>
public sealed record PostId(long Value)
{
    public static implicit operator long(PostId postId) => postId.Value;
}
