using System;

namespace NetTally.Forums
{
    public class ThreadInfo
    {
        public string Title { get; }
        public string Author { get; }
        public int Pages { get; }

        /// <summary>
        /// Constructor for class.
        /// Title must be valid.
        /// Pages must not be negative.
        /// </summary>
        /// <param name="title">The title of the thread.  Cannot be empty or null.</param>
        /// <param name="author">The author of the thread.  May be empty or null.</param>
        /// <param name="pages">The number of pages in the thread.</param>
        /// <exception cref="ArgumentNullException">If title is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If pages is negative.</exception>
        public ThreadInfo(string title, string author, int pages)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentNullException(nameof(title));
            if (pages < 0)
                throw new ArgumentOutOfRangeException(nameof(pages), pages, "Pages cannot be negative.");
            if (author == null)
                author = "";

            Title = title;
            Author = author;
            Pages = pages;
        }

        /// <summary>
        /// Utility function to get the page number of a post in a thread,  based on
        /// quest info available.
        /// </summary>
        /// <param name="postNumber">The post number being queried. Must be at least 1.</param>
        /// <param name="quest">The quest that the post number came from.</param>
        /// <returns>Returns the page number that the post should be on.</returns>
        public static int GetPageNumberOfPost(int postNumber, IQuest quest)
        {
            if (postNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(postNumber), "Post number cannot be less than 1.");
            if (quest.PostsPerPage < 1)
                throw new ArgumentOutOfRangeException(nameof(quest.PostsPerPage), "Posts per page cannot be less than 1.");

            int pageNumber = ((postNumber - 1) / quest.PostsPerPage) + 1;

            return pageNumber;
        }

    }
}
