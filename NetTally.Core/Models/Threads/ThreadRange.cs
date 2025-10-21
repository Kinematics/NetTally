using NetTally.Models.Posts;

namespace NetTally.Models.Threads;

/// <summary>
/// An abstract post range for use in thread information.
/// </summary>
public abstract record ThreadRange();

/// <summary>
/// A range of posts that start with a <see cref="Posts.Component.PostId"/>.
/// </summary>
/// <param name="PostId">The starting post in the range.</param>
/// <param name="StartPage">The starting page to read.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeById(PostId PostId, int StartPage, int PagesInThread) : ThreadRange;

/// <summary>
/// A range of posts defined by start and end posts.
/// </summary>
/// <param name="StartPostNumber">The first post in the range.</param>
/// <param name="EndPostNumber">The last post in the range. 0 means end of thread.</param>
/// <param name="PostsPerPage">The number of posts on each page of the thread.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByPosts(int StartPostNumber, int EndPostNumber,
    int PostsPerPage, int PagesInThread) : ThreadRange;

public static class PredefinedThreadRange
{
    extension(ThreadRange)
    {
        public static ThreadRange None => _none;
    }

    private static readonly ThreadRange _none =
        new ThreadRangeByPosts(0, 0, 20, 1);
}

