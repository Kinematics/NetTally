using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetTally.Adapters
{
    public class ThreadInfo
    {
        public string Title { get; }
        public string Author { get; }
        public int Pages { get; }

        /// <summary>
        /// Constructor for class.
        /// </summary>
        /// <param name="title">The title of the thread.  Cannot be empty or null.</param>
        /// <param name="author">The author of the thread.  May be empty or null.</param>
        /// <param name="pages">The number of pages in the thread.</param>
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

    public static class ThreadmarkFilter
    {
        static readonly Regex omakeRegex = new Regex(@"\bomake\b", RegexOptions.IgnoreCase);

        public static bool Filter(string title)
        {
            if (string.IsNullOrEmpty(title))
                return false;

            return omakeRegex.Match(title).Success;
        }
    }

    public class ThreadStartValue
    {
        public bool ByNumber { get; }
        public int Number { get; }
        public int Page { get; }
        public int ID { get; }

        public ThreadStartValue(bool byNumber, int number = 0, int page = 0, int id = 0)
        {
            ByNumber = byNumber;
            Number = number;
            Page = page;
            ID = id;
        }
    }
}
