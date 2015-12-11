using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using NetTally.Utility;

namespace NetTally
{
    /// <summary>
    /// Class to hold a web page, and the time at which it was loaded.
    /// </summary>
    public class CachedPage
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public HtmlDocument Doc { get; }
        public string DocString { get; }

        public CachedPage(HtmlDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            Doc = doc;
            DocString = null;
        }

        public CachedPage(string docString)
        {
            if (docString == null)
                throw new ArgumentNullException(nameof(docString));

            DocString = docString;
            Doc = null;
        }
    }

    /// <summary>
    /// Class to handle caching web content.
    /// </summary>
    public sealed class WebCache
    {
        #region Lazy singleton creation
        private static readonly Lazy<WebCache> lazy = new Lazy<WebCache>(() => new WebCache());

        public static WebCache Instance => lazy.Value;

        private WebCache()
        {
        }
        #endregion

        #region Local storage
        const int MaxCacheEntries = 50;
        readonly TimeSpan cacheDuration = TimeSpan.FromMinutes(30);
        static readonly object _lock = new object();

        Dictionary<string, CachedPage> PageCache { get; } = new Dictionary<string, CachedPage>(MaxCacheEntries);
        #endregion

        #region Public functions
        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            lock(_lock)
            {
                PageCache.Clear();
            }
        }

        /// <summary>
        /// Add a document to the cache.
        /// </summary>
        /// <param name="url">The URL the document was retrieved from.</param>
        /// <param name="doc">The HTML document being cached.</param>
        public void Add(string url, HtmlDocument doc)
        {
            ExpireCache(DateTime.Now);

            lock(_lock)
            {
                PageCache[url] = new CachedPage(doc);
            }
        }

        /// <summary>
        /// Add the original HTML string to the cache.
        /// </summary>
        /// <param name="url">The URL the document was retrieved from.</param>
        /// <param name="docString">The HTML string to cache.</param>
        public void Add(string url, string docString)
        {
            ExpireCache(DateTime.Now);

            lock (_lock)
            {
                PageCache[url] = new CachedPage(docString);
            }
        }

        /// <summary>
        /// Try to get a cached document for a specified URL.
        /// </summary>
        /// <param name="url">The URL being checked.</param>
        /// <returns>Returns the document for the URL if it's available and less than 30 minutes old.
        /// Otherwise returns null.</returns>
        public HtmlDocument Get(string url)
        {
            CachedPage cache;

            lock(_lock)
            {
                if (PageCache.TryGetValue(url, out cache))
                {
                    var cacheAge = DateTime.Now - cache.Timestamp;

                    if (cacheAge.TotalMinutes < 30)
                    {
                        if (cache.Doc != null)
                        {
                            return cache.Doc;
                        }

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(cache.DocString);
                        return doc;
                    }

                    // Purge the cached page if it's older than 30 minutes.
                    PageCache.Remove(url);
                }
            }

            return null;
        }

        #endregion

        #region Private functions
        /// <summary>
        /// Run through the cache and expire anything that is too old, or
        /// that goes over the maximum allowed cache items.
        /// </summary>
        /// <param name="time">The time this function is being called.</param>
        void ExpireCache(DateTime time)
        {
            DateTime expireTime = time - cacheDuration;

            int maxKept = MaxCacheEntries - 1;

            lock (_lock)
            {
                var pagesToRemove = PageCache.OrderByDescending(p => p.Value.Timestamp).Where((p, i) => p.Value.Timestamp <= expireTime || i > maxKept).ToList();

                foreach (var page in pagesToRemove)
                {
                    PageCache.Remove(page.Key);
                }
            }
        }

        #endregion
    }
}
