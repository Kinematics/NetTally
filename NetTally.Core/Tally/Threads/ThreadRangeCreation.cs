using NetTally.Tally.Posts.Component;

namespace NetTally.Tally.Threads;

/// <summary>
/// Extension class that handles creating new <see cref="ThreadRange"/> objects.
/// </summary>
public static class ThreadRangeCreation
{
    extension(ThreadRange)
    {
        /// <summary>
        /// Define a <see cref="ThreadRange"/> using a <see cref="PostId"/> and page information.
        /// </summary>
        /// <param name="postId">The ID of the first post in the thread range.</param>
        /// <param name="startPage">The page on which the post was found.</param>
        /// <param name="pagesInThread">How many pages there are in the thread.</param>
        /// <returns></returns>
        public static ThreadRange CreateByPostId(PostId postId, int startPage, int pagesInThread)
        {
            if (startPage < 1)
                startPage = 1;
            if (pagesInThread < 1)
                pagesInThread = 1;

            return new ThreadRangeById(postId, startPage, pagesInThread);
        }

        /// <summary>
        /// Define a <see cref="ThreadRange"/> using a post number, with the page
        /// derived by the defined posts per page.
        /// </summary>
        /// <param name="startPost">The number (in thread) of the post to start with.</param>
        /// <param name="postsPerPage">How many posts are available per page.</param>
        /// <param name="pagesInThread">How many pages there are in the thread.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Define a <see cref="ThreadRange"/> using a start and end post number,
        /// with the pages derived by the defined posts per page.
        /// </summary>
        /// <param name="startPost"></param>
        /// <param name="endPost"></param>
        /// <param name="postsPerPage"></param>
        /// <param name="pagesInThread"></param>
        /// <returns></returns>
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