using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    /// <summary>
    /// Class to save a web page, and the time at which it was loaded.
    /// </summary>
    public class CachedPage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public HtmlDocument Doc { get; }

        public CachedPage(HtmlDocument doc)
        {
            Doc = doc;
        }
    }
}
