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

        #region Constructors
        public CachedPage(HtmlDocument doc)
            : this(doc, new DefaultClock())
        {

        }

        public CachedPage(string html)
            : this(html, new DefaultClock())
        {

        }

        public CachedPage(HtmlDocument doc, IClock clock)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            Timestamp = clock.Now;
            Doc = doc;
            DocString = null;
        }

        public CachedPage(string html, IClock clock)
        {
            if (html == null)
                throw new ArgumentNullException(nameof(html));

            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            Timestamp = clock.Now;
            DocString = html;
            Doc = null;
        }
        #endregion

        #region Equality overrides
        public override int GetHashCode()
        {
            if (!string.IsNullOrEmpty(DocString))
            {
                return DocString.GetHashCode();
            }
            if (Doc != null)
            {
                return Doc.GetHashCode();
            }

            return Timestamp.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CachedPage))
                return false;

            return Equals((CachedPage)obj);
        }

        public bool Equals(CachedPage other)
        {
            if (!string.IsNullOrEmpty(DocString) && DocString == other.DocString)
                return true;

            if (Doc != null && Doc == other.Doc)
                return true;

            return false;
        }

        public static bool operator ==(CachedPage page1, CachedPage page2) => page1.Equals(page2);

        public static bool operator !=(CachedPage page1, CachedPage page2) => !page1.Equals(page2);
        #endregion
    }
}
