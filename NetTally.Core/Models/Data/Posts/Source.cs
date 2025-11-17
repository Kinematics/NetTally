namespace NetTally.Models;

/// <summary>
/// A record containing identifying information for the source of a post or other object.
/// </summary>
public abstract record Source();

/// <summary>
/// A type indicating that no source is available.
/// </summary>
public sealed record NoSource() : Source;

/// <summary>
/// The source location of a post or other object.
/// </summary>
/// <param name="Thread">The thread the post was part of.</param>
/// <param name="Permalink">A permalink to the post.</param>
/// <param name="PostId">The id number of the post, generally unique per forum.</param>
/// <param name="PostNumber">The post number of the post within the thread it was a part of.</param>
public sealed record SourceLocation(Uri Thread, Uri Permalink, PostId PostId, PostNumber PostNumber) : Source;
