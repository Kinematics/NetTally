using System;
using HtmlAgilityPack;
using NetTally.Utility;

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

        public CachedPage(HtmlDocument doc, IClock clock = null)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (clock == null)
                clock = new DefaultClock();

            Timestamp = clock.Now;
            Doc = doc;
            DocString = null;
        }

        public CachedPage(string html, IClock clock = null)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            if (clock == null)
                clock = new DefaultClock();

            Timestamp = clock.Now;
            DocString = html;
            Doc = null;
        }
    }
}
