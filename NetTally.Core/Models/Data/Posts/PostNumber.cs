namespace NetTally.Models;

/// <summary>
/// A number of a post within a thread.
/// </summary>
/// <param name="Value">The number of a post within a thread.</param>
public sealed record PostNumber(long Value)
{
    public static implicit operator long(PostNumber postId) => postId.Value;
}
