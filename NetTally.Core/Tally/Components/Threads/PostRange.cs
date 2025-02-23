using NetTally.Tally.Components.Posts;

namespace NetTally.Tally.Components.Threads;

public abstract record PostRange()
{
    public abstract int GetStartPage();
    public abstract int GetEndPage();
}

public record PostRangeById(PostIdType PostId, int StartPage, int PagesInThread) : PostRange
{
    public override int GetStartPage() => StartPage;
    public override int GetEndPage() => PagesInThread;
}

public record PostRangeByPost(int StartPostNumber, int EndPostNumber, int PostsPerPage, int PagesInThread) : PostRange
{
    public override int GetStartPage() => GetPageNumberOfPost(StartPostNumber, PostsPerPage);
    public override int GetEndPage() => EndPostNumber == 0 
        ? PagesInThread
        : Math.Min(GetPageNumberOfPost(EndPostNumber, PostsPerPage), PagesInThread);

    private static int GetPageNumberOfPost(int postNumber, int postsPerPage) =>
        ((postNumber - 1) / postsPerPage) + 1;

}

public static class PostRanges
{
    public static readonly PostRange None = new PostRangeByPost(0, 0, 20, 1);

    public static PostRange CreateByPostId(PostIdType postId, int startPage, int pagesInThread)
    {
        if (startPage < 1)
            startPage = 1;
        if (pagesInThread < 1)
            pagesInThread = 1;

        return new PostRangeById(postId, startPage, pagesInThread);
    }

    public static PostRange CreateByRange(int startPost, int endPost, int postsPerPage, int pagesInThread)
    {
        if (startPost < 1)
            startPost = 1;
        if (endPost < 0)
            endPost = 0;
        if (postsPerPage < 1)
            postsPerPage = 20;
        if (pagesInThread < 1)
            pagesInThread = 1;

        return new PostRangeByPost(startPost, endPost, postsPerPage, pagesInThread);
    }
}