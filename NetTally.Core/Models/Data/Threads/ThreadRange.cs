namespace NetTally.Models;

/// <summary>
/// A range of posts to tally.
/// </summary>
public abstract record ThreadRange();

/// <summary>
/// A range of posts that start with a <see cref="PostId"/>, and
/// continues to the end of the thread.
/// </summary>
/// <param name="StartingPostId">The starting post in the range.</param>
/// <param name="StartPage">The starting page to read.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByStartingId(PostId StartingPostId,
    int StartPage, int PagesInThread) : ThreadRange;

/// <summary>
/// A range of posts that start with a <see cref="PostNumber">, and
/// continues to the end of the thread.
/// </summary>
/// <param name="StartPostNumber">The first post in the range.</param>
/// <param name="PostsPerPage">The number of posts on each page of the thread.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByStartingPost(PostNumber StartPostNumber,
    int PostsPerPage, int PagesInThread) : ThreadRange;

/// <summary>
/// A range of posts defined by start and end <see cref="PostNumber">s.
/// </summary>
/// <param name="StartPostNumber">The first post in the range.</param>
/// <param name="EndPostNumber">The last post in the range.</param>
/// <param name="PostsPerPage">The number of posts on each page of the thread.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByPostRange(PostNumber StartPostNumber, PostNumber EndPostNumber,
    int PostsPerPage, int PagesInThread) : ThreadRange;

