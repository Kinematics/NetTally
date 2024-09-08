using NetTally.Enums;
using NetTally.Tally.Components.Posts;
using NetTally.Utility;

namespace NetTally.Tally.Components.Threads;
public record ThreadInformationType(
    string Title,
    AuthorType Author,
    ThreadRangeRangeType RangeType,
    PostIdType StartPostId,
    int StartPostNumber,
    int PageNumberOfStartPost,
    int PagesInThread);

public static class ThreadInformation
{
    public static ThreadInformationType None { get; } =
        new ThreadInformationType(string.Empty, Author.None, ThreadRangeRangeType.ByPostNumber,
            PostId.Zero, 0, 0, 0);

    public static ThreadInformationType Create(
        string? title,
        AuthorType? author,
        ThreadRangeRangeType rangeType,
        PostIdType startPostId,
        int startPostNumber,
        int pageNumberOfStartPost,
        int pagesInThread)
    {
        if (string.IsNullOrEmpty(title))
            title = Strings.UntitledThread;
        author ??= Author.Unknown;
        if (startPostNumber < 0)
            startPostNumber = 0;
        if (pageNumberOfStartPost < 0)
            pageNumberOfStartPost = 0;

        return new ThreadInformationType(title, author,
            rangeType, startPostId, startPostNumber,
            pageNumberOfStartPost, pagesInThread);
    }

    public static ThreadInformationType CreateByPostNumber(
        string? title,
        AuthorType? author,
        int startPostNumber,
        int pagesInThread)
    {
        if (startPostNumber < 1)
            startPostNumber = 1;

        return Create(title, author,
            ThreadRangeRangeType.ByPostNumber, PostId.Zero,
            startPostNumber, 0, pagesInThread);
    }

    public static ThreadInformationType CreateByPostId(
        string? title,
        AuthorType? author,
        PostIdType postId,
        int pageNumberOfStartPost,
        int pagesInThread)
    {
        return Create(title, author,
            ThreadRangeRangeType.ByPostId, postId,
            0, pageNumberOfStartPost, pagesInThread);
    }

    public static int GetStartPage(ThreadInformationType threadInfo, Quest quest)
    {
        if (threadInfo.RangeType == ThreadRangeRangeType.ByPostId)
            return threadInfo.PageNumberOfStartPost;

        return GetPageNumberOfPost(threadInfo.StartPostNumber, quest.PostsPerPage);
    }

    public static int GetEndPage(ThreadInformationType threadInfo, Quest quest)
    {
        // ByPostId means we got the starting point from the threadmark,
        // and will therefore be reading to the end of the thread.
        // Otherwise it's by post number, and we need to check the Quest.
        if (threadInfo.RangeType == ThreadRangeRangeType.ByPostId || quest.ReadToEndOfThread)
        {
            if (threadInfo.PagesInThread > 0)
            {
                return threadInfo.PagesInThread;
            }
        }

        // If we get here, we're using ByPostNumber, and there is a specific end post.
        int endPostPage = GetPageNumberOfPost(quest.EndPost, quest.PostsPerPage);

        // Make sure we don't go past the end of the thread, if we know the number of pages.
        if (threadInfo.PagesInThread > 0)
        {
            endPostPage = Math.Min(endPostPage, threadInfo.PagesInThread);
        }

        return endPostPage;
    }

    public static int GetPageNumberOfPost(int postNumber, int postsPerPage)
    {
        return ((postNumber - 1) / postsPerPage) + 1;
    }
}