using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HtmlAgilityPack;
using NetTally.Utility;

namespace NetTally.Web
{
    /// <summary>
    /// Class to handle caching web content.
    /// </summary>
    public sealed class WebCache : IDisposable
    {
        #region Lazy singleton creation
        static readonly Lazy<WebCache> lazy = new Lazy<WebCache>(() => new WebCache());

        public static WebCache Instance => lazy.Value;

        WebCache()
        {
        }
        #endregion

        #region Disposal
        ~WebCache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true); //I am calling you from Dispose, it's safe
            GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
        }

        void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                Clear();
                cacheLock?.Dispose();
            }

            _disposed = true;
        }
        #endregion

        #region Local storage
        bool _disposed = false;

        const int MaxCacheEntries = 50;
        readonly TimeSpan maxCacheDuration = TimeSpan.FromMinutes(30);

        Dictionary<string, CachedPage> PageCache { get; } = new Dictionary<string, CachedPage>(MaxCacheEntries);
        readonly ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        #endregion

        #region Public functions
        /// <summary>
        /// Add a document to the cache.
        /// </summary>
        /// <param name="url">The URL the document was retrieved from.</param>
        /// <param name="doc">The HTML document being cached.</param>
        public void Add(string url, HtmlDocument doc)
        {
            AddCachedPage(url, new CachedPage(doc));
        }

        /// <summary>
        /// Add the original HTML string to the cache.
        /// </summary>
        /// <param name="url">The URL the document was retrieved from.</param>
        /// <param name="docString">The HTML string to cache.</param>
        public void Add(string url, string docString)
        {
            AddCachedPage(url, new CachedPage(docString));
        }

        /// <summary>
        /// Handle adding a CachedPage to the cache dictionary, with locking.
        /// </summary>
        /// <param name="url">The URL the page was retrieved from.</param>
        /// <param name="cachedPage">The object to cache.</param>
        void AddCachedPage(string url, CachedPage cachedPage)
        {
            cacheLock.EnterWriteLock();
            try
            {
                PageCache[url] = cachedPage;

                if (PageCache.Count > MaxCacheEntries)
                {
                    var oldestEntry = PageCache.MinObject(p => p.Value.Timestamp);
                    PageCache.Remove(oldestEntry.Key);
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
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

            cacheLock.EnterReadLock();
            try
            {
                if (PageCache.TryGetValue(url, out cache))
                {
                    var cacheAge = DateTime.Now - cache.Timestamp;

                    if (cacheAge < maxCacheDuration)
                    {
                        if (cache.Doc != null)
                        {
                            return cache.Doc;
                        }

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(cache.DocString);
                        return doc;
                    }
                }
            }
            finally
            {
                cacheLock.ExitReadLock();
            }

            return null;
        }

        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            cacheLock.EnterWriteLock();
            try
            {
                PageCache.Clear();
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Remove all entries that are older than our defined time limit to retain cached pages.
        /// </summary>
        /// <param name="time">The reference time to use when determining the age of a page.</param>
        public void ExpireCache(DateTime time)
        {
            DateTime expireLimitTime = time - maxCacheDuration;

            cacheLock.EnterWriteLock();
            try
            {
                var pagesToRemove = PageCache.Where(p => p.Value.Timestamp <= expireLimitTime).ToList();

                foreach (var page in pagesToRemove)
                {
                    PageCache.Remove(page.Key);
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }
        #endregion
    }
}
