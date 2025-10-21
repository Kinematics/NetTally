using NetTally.Models.Defaults;
using NetTally.Models.Posts;
using NetTally.Models.Threads;

namespace NetTally.Models.Creation;

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

            return new ThreadRangeByStartingId(postId, startPage, pagesInThread);
        }

        /// <summary>
        /// Define a <see cref="ThreadRange"/> using a post number, with the page
        /// derived by the defined posts per page.
        /// </summary>
        /// <param name="startPost">The number (in thread) of the post to start with.</param>
        /// <param name="postsPerPage">How many posts are available per page.</param>
        /// <param name="pagesInThread">How many pages there are in the thread.</param>
        /// <returns></returns>
        public static ThreadRange? CreateByStartOfRange(int startPost, int postsPerPage, int pagesInThread)
        {
            if (startPost < 1)
                startPost = 1;
            var startPostNumber = PostNumber.Create(startPost);
            if (startPostNumber is null)
                return null;

            return CreateByStartOfRange(startPostNumber, postsPerPage, pagesInThread);
        }

        public static ThreadRange CreateByStartOfRange(PostNumber startPost, int postsPerPage, int pagesInThread)
        {
            if (postsPerPage < 1)
                postsPerPage = 20;
            if (pagesInThread < 1)
                pagesInThread = 1;

            return new ThreadRangeByStartingPost(startPost, postsPerPage, pagesInThread);
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
        public static ThreadRange? CreateByRange(int startPost, int endPost, int postsPerPage, int pagesInThread)
        {
            if (startPost < 1)
                startPost = 1;
            var startPostNumber = PostNumber.Create(startPost);
            if (startPostNumber is null)
                return null;

            if (endPost < 1)
                return CreateByStartOfRange(startPost, postsPerPage, pagesInThread);

            var endPostNumber = PostNumber.Create(endPost);
            if (endPostNumber is null)
                return null;

            return CreateByRange(startPostNumber, endPostNumber, postsPerPage, pagesInThread);
        }

        public static ThreadRange? CreateByRange(PostNumber startPost, PostNumber endPost, int postsPerPage, int pagesInThread)
        {
            if (endPost == PostNumber.Zero)
                return CreateByStartOfRange(startPost, postsPerPage, pagesInThread);

            if (postsPerPage < 1)
                postsPerPage = 20;
            if (pagesInThread < 1)
                pagesInThread = 1;

            return new ThreadRangeByPostRange(startPost, endPost, postsPerPage, pagesInThread);
        }
    }
}