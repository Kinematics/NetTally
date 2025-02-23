using NetTally.Tally.Components.Posts;

namespace NetTally.Tally.Components.Threads;

/// <summary>
/// An abstract post range for use in thread information.
/// </summary>
public abstract record ThreadRange()
{
    public abstract int GetStartPage();
    public abstract int GetEndPage();
}

/// <summary>
/// A range of posts that start with a <see cref="Posts.PostId"/>.
/// </summary>
/// <param name="PostId">The starting post in the range.</param>
/// <param name="StartPage">The starting page to read.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeById(PostId PostId, int StartPage, int PagesInThread) : ThreadRange
{
    public override int GetStartPage() => StartPage;
    public override int GetEndPage() => PagesInThread;
}

/// <summary>
/// A range of posts defined by start and end posts.
/// </summary>
/// <param name="StartPostNumber">The first post in the range.</param>
/// <param name="EndPostNumber">The last post in the range. 0 means end of thread.</param>
/// <param name="PostsPerPage">The number of posts on each page of the thread.</param>
/// <param name="PagesInThread">The number of pages in the thread.</param>
public sealed record ThreadRangeByPosts(int StartPostNumber, int EndPostNumber, int PostsPerPage, int PagesInThread) : ThreadRange
{
    public override int GetStartPage() => 
        GetPageNumberOfPost(StartPostNumber, PostsPerPage);

    public override int GetEndPage() => EndPostNumber == 0 
        ? PagesInThread
        : Math.Min(GetPageNumberOfPost(EndPostNumber, PostsPerPage), PagesInThread);

    private static int GetPageNumberOfPost(int postNumber, int postsPerPage) =>
        ((postNumber - 1) / postsPerPage) + 1;

}

/// <summary>
/// Static class that handles creating new <see cref="ThreadRange"/> objects.
/// </summary>
public static class ThreadRanges
{
    public static ThreadRange None { get; } = new ThreadRangeByPosts(0, 0, 20, 1);

    public static ThreadRange CreateByPostId(PostId postId, int startPage, int pagesInThread)
    {
        if (startPage < 1)
            startPage = 1;
        if (pagesInThread < 1)
            pagesInThread = 1;

        return new ThreadRangeById(postId, startPage, pagesInThread);
    }

    public static ThreadRange CreateByStartOfRange(int startPost, int postsPerPage, int pagesInThread)
    {
        if (startPost < 1)
            startPost = 1;
        if (postsPerPage < 1)
            postsPerPage = 20;
        if (pagesInThread < 1)
            pagesInThread = 1;

        return new ThreadRangeByPosts(startPost, 0, postsPerPage, pagesInThread);
    }

    public static ThreadRange CreateByRange(int startPost, int endPost, int postsPerPage, int pagesInThread)
    {
        if (startPost < 1)
            startPost = 1;
        if (endPost < 0)
            endPost = 0;
        if (postsPerPage < 1)
            postsPerPage = 20;
        if (pagesInThread < 1)
            pagesInThread = 1;

        return new ThreadRangeByPosts(startPost, endPost, postsPerPage, pagesInThread);
    }
}