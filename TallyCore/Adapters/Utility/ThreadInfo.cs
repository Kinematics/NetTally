using System;

namespace NetTally.Adapters
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
    }
}
