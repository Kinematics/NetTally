using NetTally.Models.Posts;

namespace NetTally.Models.Threads;

/// <summary>
/// A post range for use in thread information.
/// </summary>
public abstract record ThreadRange();

/// <summary>
/// A range of posts that start with a <see cref="PostId"/>.
/// </summary>
/// <param name="StartingPostId">The starting post in the range.</param>
/// <param name="StartPage">The starting page to read.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByStartingId(PostId StartingPostId, int StartPage, int PagesInThread) : ThreadRange;

/// <summary>
/// A range of posts defined by start and end posts.
/// </summary>
/// <param name="StartPostNumber">The first post in the range.</param>
/// <param name="EndPostNumber">The last post in the range. 0 means end of thread.</param>
/// <param name="PostsPerPage">The number of posts on each page of the thread.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByPostRange(PostNumber StartPostNumber, PostNumber EndPostNumber,
    int PostsPerPage, int PagesInThread) : ThreadRange;

/// <summary>
/// A range of posts defined by a starting post number.
/// </summary>
/// <param name="StartPostNumber">The first post in the range.</param>
/// <param name="PostsPerPage">The number of posts on each page of the thread.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByStartingPost(PostNumber StartPostNumber,
    int PostsPerPage, int PagesInThread) : ThreadRange;

