using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Extensions;
using NetTally.Utility;
using Nito.AsyncEx;

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
            SetClock(null);
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
            }

            _disposed = true;
        }
        #endregion

        #region Local fields
        bool _disposed;

        IClock Clock { get; set; }

        const int MaxCacheEntries = 50;
        readonly TimeSpan maxCacheDuration = TimeSpan.FromMinutes(30);

        Dictionary<string, CacheObject<string>> PageCache { get; } = new Dictionary<string, CacheObject<string>>(MaxCacheEntries);

        readonly AsyncReaderWriterLock cacheLock = new AsyncReaderWriterLock();
        #endregion

        #region Public functions
        /// <summary>
        /// Allow setting the clock interface to be used by the cache.
        /// </summary>
        /// <param name="clock">The clock interface that will be used to determine timestamps.</param>
        public void SetClock(IClock clock)
        {
            using (cacheLock.ReaderLock())
            {
                Clock = clock ?? new DefaultClock();
            }
        }

        /// <summary>
        /// Add the original HTML string to the cache.
        /// </summary>
        /// <param name="url">The URL the document was retrieved from.</param>
        /// <param name="html">The HTML string to cache.</param>
        public void Add(string url, string html)
        {
            AddCachedPage(url, new CacheObject<string>(html));
        }

        /// <summary>
        /// Handle adding a CachedPage to the cache dictionary, with locking.
        /// </summary>
        /// <param name="url">The URL the page was retrieved from.</param>
        /// <param name="cachedPage">The object to cache.</param>
        void AddCachedPage(string url, CacheObject<string> cachedPage)
        {
            using (cacheLock.WriterLock())
            { 
                PageCache[url] = cachedPage;

                if (PageCache.Count > MaxCacheEntries)
                {
                    var oldestEntry = PageCache.MinObject(p => p.Value.Timestamp);
                    PageCache.Remove(oldestEntry.Key);
                }
            }
        }

        /// <summary>
        /// Try to get a cached document for a specified URL.
        /// </summary>
        /// <param name="url">The URL being checked.</param>
        /// <returns>Returns the document for the URL if it's available and less than 30 minutes old.
        /// Otherwise returns null.</returns>
        public async Task<HtmlDocument> GetAsync(string url)
        {
            using (cacheLock.ReaderLock())
            { 
                if (PageCache.TryGetValue(url, out CacheObject<string> cache))
                {
                    var cacheAge = Clock.Now - cache.Timestamp;

                    if (cacheAge < maxCacheDuration)
                    {
                        HtmlDocument doc = new HtmlDocument();
                        await Task.Run(() => doc.LoadHtml(cache.Store)).ConfigureAwait(false);
                        return doc;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            using (cacheLock.WriterLock())
            {
                PageCache.Clear();
            }
        }

        /// <summary>
        /// Remove all entries that are older than our defined time limit to retain cached pages.
        /// </summary>
        /// <param name="time">The reference time to use when determining the age of a page.</param>
        public void ExpireCache(DateTime time)
        {
            DateTime oldestAllowedTime = time - maxCacheDuration;

            using (cacheLock.WriterLock())
            {
                var pagesToRemove = PageCache.Where(p => p.Value.Timestamp <= oldestAllowedTime).ToList();

                foreach (var page in pagesToRemove)
                {
                    PageCache.Remove(page.Key);
                }
            }
        }
        #endregion
    }
}
