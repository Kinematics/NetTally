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

        public CachedPage(HtmlDocument doc)
        {
            Doc = doc;
        }
    }

    /// <summary>
    /// Class to handle caching web content.
    /// </summary>
    public class WebCache
    {
        Dictionary<string, CachedPage> PageCache { get; }
        Dictionary<string, int> LastPageLoadedFor { get; }

        int MaxCacheEntries { get; }

        /// <summary>
        /// Constructor.
        /// Allow specifying the max number of cache entries on creation.
        /// </summary>
        /// <param name="maxCacheEntries">Max number of entries the cache will retain.</param>
        public WebCache(int maxCacheEntries = 50)
        {
            MaxCacheEntries = maxCacheEntries;

            PageCache = new Dictionary<string, CachedPage>();
            LastPageLoadedFor = new Dictionary<string, int>();
        }

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
    }
}
