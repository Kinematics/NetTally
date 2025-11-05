namespace NetTally.Models;

/// <summary>
/// Information about a thread, and the range of posts that should be tallied.
/// </summary>
/// <param name="Title">The title of the thread.</param>
/// <param name="Author">The original author of the thread.</param>
/// <param name="ThreadRange">The range of posts in the thread to be tallied.</param>
public sealed record ThreadInfo(string Title, Author Author, ThreadRange ThreadRange);
