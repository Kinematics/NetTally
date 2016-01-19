using System;
using HtmlAgilityPack;

namespace NetTally.Web
{
    /// <summary>
    /// Class to hold a web page, and the time at which it was loaded.
    /// </summary>
    public struct CachedPage
    {
        public DateTime Timestamp { get; }
        public HtmlDocument Doc { get; }
        public string DocString { get; }

        public CachedPage(HtmlDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            Timestamp = DateTime.Now;
            Doc = doc;
            DocString = null;
        }

        public CachedPage(string html)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            Timestamp = DateTime.Now;
            DocString = html;
            Doc = null;
        }
    }
}
