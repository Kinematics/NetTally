namespace NetTally.Models;

/// <summary>
/// The uniquely identifying information for the source of a post or other object.
/// </summary>
public abstract record Source();

/// <summary>
/// Represents a source that does not provide any data or content.
/// </summary>
/// <remarks>Use this type to indicate the absence of a source in scenarios where a source is required by the API
/// but no actual data should be supplied. This can be useful for disabling source-based operations or as a default
/// placeholder.</remarks>
public sealed record NoSource() : Source;

/// <summary>
/// The source location of a post or other object.
/// </summary>
/// <param name="Thread">The thread the post was part of.</param>
/// <param name="Permalink">A permalink to the post.</param>
/// <param name="PostId">The id number of the post, generally unique per forum.</param>
/// <param name="PostNumber">The post number of the post within the thread it was a part of.</param>
public sealed record SourceLocation(Uri Thread, Uri Permalink, PostId PostId, PostNumber PostNumber) : Source;
