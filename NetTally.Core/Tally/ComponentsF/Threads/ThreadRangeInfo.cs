using System;
using NetTally.Tally.ComponentsF.Posts;

namespace NetTally.Tally.ComponentsF.Threads;

public enum ThreadRangeRangeType
{
    ByPostNumber,
    ByPostId
}

/// <summary>
/// Data type for thread range information.
/// </summary>
/// <param name="RangeType">What type of range is recorded.</param>
/// <param name="StartPostNumber">The starting post number (if by post number)</param>
/// <param name="PageNumber">The page number the post ID is on (if by post id)</param>
/// <param name="PostId">The starting post ID (if by post id)</param>
/// <param name="PageCount"></param>
public record ThreadRangeType(
    ThreadRangeRangeType RangeType,
    int StartPostNumber,
    PostIdType PostId,
    int PageNumber,
    int PageCount)
{
    public override string ToString()
    {
        if (RangeType == ThreadRangeRangeType.ByPostNumber)
            return $"{RangeType}: Post #{StartPostNumber}, Pages: {PageCount}";
        else
            return $"{RangeType}: ID #{PostId.Id} on Page {PageNumber}, Pages: {PageCount}";
    }
}


public static class ThreadRange
{
    public static ThreadRangeType Empty { get; } = new ThreadRangeType(ThreadRangeRangeType.ByPostId,
        0, PostId.Zero, 0, 0);
    public static ThreadRangeType StartOfThread { get; } = new ThreadRangeType(ThreadRangeRangeType.ByPostNumber,
        1, PostId.Zero, 0, 0);

    public static ThreadRangeType CreateRangeByPost(int startPostNumber = 0, int pageCount = 0)
    {
        if (startPostNumber < 1)
            return StartOfThread;

        return new ThreadRangeType(ThreadRangeRangeType.ByPostNumber, startPostNumber, PostId.Zero, 0, pageCount);
    }

    public static ThreadRangeType CreateRangeFromPostId(PostIdType postId, int page, int pageCount = 0)
    {
        if (page < 1)
            page = 1;

        return new ThreadRangeType(ThreadRangeRangeType.ByPostId, 0, postId, page, pageCount);
    }


    public static int GetStartPage(ThreadRangeType threadRange, Quest quest)
    {
        if (threadRange.RangeType == ThreadRangeRangeType.ByPostId)
            return threadRange.PageNumber;

        return GetPageNumberOfPost(threadRange.StartPostNumber, quest.PostsPerPage);
    }

    public static int GetEndPage(ThreadRangeType threadRange, Quest quest)
    {
        // ByPostId means we got the starting point from the threadmark,
        // and will therefore be reading to the end of the thread.
        // Otherwise it's by post number, and we need to check the Quest.
        if (threadRange.RangeType == ThreadRangeRangeType.ByPostId || quest.ReadToEndOfThread)
        {
            if (threadRange.PageCount > 0)
            {
                return threadRange.PageCount;
            }
        }

        // If we get here, we're using ByPostNumber, and there is a specific end post.
        int endPostPage = GetPageNumberOfPost(quest.EndPost, quest.PostsPerPage);

        // Make sure we don't go past the end of the thread, if we know the number of pages.
        if (threadRange.PageCount > 0)
        {
            endPostPage = Math.Min(endPostPage, threadRange.PageCount);
        }

        return endPostPage;
    }

    public static int GetPageNumberOfPost(int postNumber, int postsPerPage)
    {
        return ((postNumber - 1) / postsPerPage) + 1;
    }
}
