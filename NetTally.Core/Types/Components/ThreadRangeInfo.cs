using System;

namespace NetTally.Types.Components
{
    /// <summary>
    /// Class to store info about where to start tallying a thread, and possibly
    /// how many pages are in the thread based on initial scans.
    /// </summary>
    public class ThreadRangeInfo
    {
        public bool ByNumber { get; }
        public int Number { get; }
        public int Page { get; }
        public int ID { get; }
        public int Pages { get; }

        public static readonly ThreadRangeInfo Empty = new ThreadRangeInfo(false, 0, 0, 0, 0);

        /// <summary>
        /// Constructor for thread range info.
        /// Only specifies whether the range is specified by post number, and what that post number is.
        /// </summary>
        /// <param name="byNumber">Whether the range is specified by post number.</param>
        /// <param name="number">The post number that begins the range.</param>
        public ThreadRangeInfo(bool byNumber, int number) : this(byNumber, number, 0, 0, 0) { }

        /// <summary>
        /// Constructor for thread range info.
        /// Specifies whether the start data is by post number or post ID.
        /// Specifies the starting post number, if applicable.
        /// Specifies the page number the post is located on (or 0 if unknown).
        /// Specifies the ID of the starting post (or -1 if unknown).
        /// </summary>
        /// <param name="byNumber">Whether the starting post is specified by post number.</param>
        /// <param name="number">The post number that begins the range, if applicable.  0, if not.</param>
        /// <param name="page">The page the starting post is located on.  0, if not known.</param>
        /// <param name="id">The ID of the starting post.  -1 if not known.</param>
        public ThreadRangeInfo(bool byNumber, int number, int page, int id)
            : this(byNumber, number, page, id, 0) { }

        /// <summary>
        /// Constructor for thread range info.
        /// Specifies whether the start data is by post number or post ID.
        /// Specifies the starting post number, if applicable.
        /// Specifies the page number the post is located on (or 0 if unknown).
        /// Specifies the ID of the starting post (or 0 if unknown).
        /// Specifies the number of pages in the thread.
        /// </summary>
        /// <param name="byNumber">Whether the starting post is specified by post number.</param>
        /// <param name="number">The post number that begins the range, if applicable.  0, if not.</param>
        /// <param name="page">The page the starting post is located on.  0, if not known.</param>
        /// <param name="id">The ID of the starting post.  0 if not known.</param>
        /// <param name="pages">The number of pages in the thread.</param>
        /// <exception cref="ArgumentOutOfRangeException">If any parameter is out of range (negative).</exception>
        public ThreadRangeInfo(bool byNumber, int number, int page, int id, int pages)
        {
            if (number < 0)
                throw new ArgumentOutOfRangeException(nameof(number), number, "Post number cannot be negative.");
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page), page, "Page number cannot be negative.");
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Post ID cannot be negative.");
            if (pages < 0)
                throw new ArgumentOutOfRangeException(nameof(pages), pages, "Page count cannot be negative.");

            ByNumber = byNumber;
            Number = number;
            Page = page;
            ID = id;
            Pages = pages;
        }

        /// <summary>
        /// Get the starting page for tallying for the provided quest, using either
        /// the explicitly determined page number, or the calculated page number 
        /// based on the starting post and the quest's post per page.
        /// </summary>
        /// <param name="quest">The quest this info is for.</param>
        /// <returns>Returns the page number to start tallying.</returns>
        public int GetStartPage(Quest quest)
        {
            if (ByNumber)
            {
                return GetPageNumberOfPost(Number, quest.PostsPerPage);
            }

            return Page;
        }

        /// <summary>
        /// Utility function to get the page number of a post in a thread,  based on
        /// quest info available.
        /// </summary>
        /// <param name="postNumber">The post number being queried. Must be at least 1.</param>
        /// <param name="quest">The quest that the post number came from.</param>
        /// <returns>Returns the page number that the post should be on.</returns>
        public static int GetPageNumberOfPost(int postNumber, int postsPerPage)
        {
            if (postNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(postNumber), "Post number cannot be less than 1.");
            if (postsPerPage < 1)
                throw new ArgumentOutOfRangeException(nameof(postsPerPage), "Posts per page cannot be less than 1.");

            int pageNumber = ((postNumber - 1) / postsPerPage) + 1;

            return pageNumber;
        }

        /// <summary>
        /// Gets whether this thread range info must be the result of a threadmark search.
        /// A threadmark search will set either or both of the Page and ID values.
        /// If both of those are 0, this is not a threadmark search result.
        /// </summary>
        public bool IsThreadmarkSearchResult => !(Page == 0 && ID == 0);

        /// <summary>
        /// String representation of ThreadRangeInfo.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ByNumber}:{Number}, {Page}/{Pages}, {ID}";
        }
    }
}
