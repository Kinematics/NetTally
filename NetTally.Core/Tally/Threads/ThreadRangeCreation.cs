using NetTally.Tally.Posts.Component;

namespace NetTally.Tally.Threads;

/// <summary>
/// Extension class that handles creating new <see cref="ThreadRange"/> objects.
/// </summary>
public static class ThreadRangeCreation
{
    extension(ThreadRange)
    {
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
}