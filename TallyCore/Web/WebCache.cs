using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

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

        Dictionary<string, CachedPage> PageCache { get; } = new Dictionary<string, CachedPage>(MaxCacheEntries);
        Dictionary<string, int> LastPageLoadedFor { get; } = new Dictionary<string, int>();
        #endregion

        #region Public functions
        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            PageCache.Clear();
            LastPageLoadedFor.Clear();
        }

        /// <summary>
        /// Add a document to the cache.
        /// </summary>
        /// <param name="url">The URL the document was retrieved from.</param>
        /// <param name="doc">The HTML document being cached.</param>
        public void Add(string url, HtmlDocument doc)
        {
            PageCache[url] = new CachedPage(doc);
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

            if (PageCache.TryGetValue(url, out cache))
            {
                var cacheAge = DateTime.Now - cache.Timestamp;

                if (cacheAge.TotalMinutes < 30)
                {
                    return cache.Doc;
                }

                // Purge the cached page if it's older than 30 minutes.
                PageCache.Remove(url);
            }

            return null;
        }

        /// <summary>
        /// Update the cache with the last page number loaded for a given thread.
        /// On update, will initiate a cleaning of the cache to prevent memory bloat.
        /// </summary>
        /// <param name="threadName">The name of the thread that was loaded.</param>
        /// <param name="lastPageNumber">The last page number loaded for that thread.</param>
        public void Update(string threadName, int lastPageNumber)
        {
            LastPageLoadedFor[threadName] = lastPageNumber;
            CleanCache();
        }

        /// <summary>
        /// Get the last page loaded for the given thread name, if we have that info.
        /// </summary>
        /// <param name="threadName">The name of the thread being checked.</param>
        /// <returns>Returns the last page number, if found, or null if not.</returns>
        public int? GetLastPageLoaded(string threadName)
        {
            if (LastPageLoadedFor.ContainsKey(threadName))
                return LastPageLoadedFor[threadName];

            return null;
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Clean the cache to prevent it from growing too large.
        /// </summary>
        private void CleanCache()
        {
            if (PageCache.Count > MaxCacheEntries)
            {
                var newestEntries = PageCache.OrderByDescending(p => p.Value.Timestamp).Take(MaxCacheEntries);
                var olderEntries = PageCache.Except(newestEntries).ToList();

                foreach (var entry in olderEntries)
                {
                    PageCache.Remove(entry.Key);
                }

                GC.Collect();
            }
        }
        #endregion
    }
}
