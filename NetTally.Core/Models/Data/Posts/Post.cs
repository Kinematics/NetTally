namespace NetTally.Models;

/// <summary>
/// A forum post.
/// </summary>
/// <param name="Origin">Origin information of the post.</param>
/// <param name="Text">Text contents of the post.</param>
public sealed record Post(Origin Origin, string Text);
