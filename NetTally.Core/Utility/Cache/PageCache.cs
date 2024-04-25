using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Nito.AsyncEx;

namespace NetTally.Cache
{
    /// <summary>
    /// Class to handle caching web content.
    /// Uses compression on cached web pages.
    /// </summary>
    public sealed class PageCache(TimeProvider timeProvider) : ICache<string>, IDisposable
    {
        #region Disposal
        ~PageCache()
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

        readonly AsyncReaderWriterLock cacheLock = new();

        const int maxCacheEntries = 100;

        const int defaultExpirationInMinutes = 60;

        Dictionary<string, CacheObject<byte[]>> GzPageCache { get; } = new Dictionary<string, CacheObject<byte[]>>(maxCacheEntries);

        readonly TimeProvider timeProvider = timeProvider;
        #endregion

        #region Public interface
        /// <summary>
        /// The maximum number of entries this cache will hold.
        /// </summary>
        public int MaxCacheEntries => maxCacheEntries;

        /// <summary>
        /// The current number of entries held by the cache.
        /// </summary>
        public int Count => GzPageCache.Count;

        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            using (cacheLock.WriterLock())
            {
                GzPageCache.Clear();
            }
        }

        /// <summary>
        /// Add a web document to the cache.
        /// </summary>
        /// <param name="key">The URL the document was retrieved from.</param>
        /// <param name="content">The HTML document text to cache.</param>
        public void Add(string key, string content, DateTimeOffset expires)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var now = timeProvider.GetUtcNow();

            if (expires == CacheInfo.DefaultExpiration)
                expires = now.AddMinutes(defaultExpirationInMinutes);

            byte[] zipped = Compress(content);

            var toGZCache = new CacheObject<byte[]>(zipped, expires, now);

            using (cacheLock.WriterLock())
            {
                GzPageCache[key] = toGZCache;
            }
        }

        /// <summary>
        /// Try to get a cached document for a specified URL.
        /// </summary>
        /// <param name="key">The URL being checked.</param>
        /// <returns>Returns a tuple indicating whether the requested document was
        /// found, and cached document if available.</returns>
        public (bool found, string content) Get(string key)
        {
            using (cacheLock.ReaderLock())
            {
                if (GzPageCache.TryGetValue(key, out CacheObject<byte[]>? gzCache))
                {
                    if (gzCache.Expires > timeProvider.GetUtcNow())
                    {
                        string content = Decompress(gzCache.Store);

                        return (true, content);
                    }
                }
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// If our cache count is higher than our limit, then remove all expired entries,
        /// and a minimum number of pages to bring our count back down to the limit.
        /// </summary>
        public void InvalidateCache()
        {
            var time = timeProvider.GetUtcNow();

            using (cacheLock.WriterLock())
            {
                if (GzPageCache.Count > MaxCacheEntries)
                {
                    int toRemove = GzPageCache.Count - MaxCacheEntries;

                    var orderedCache = GzPageCache.OrderBy(p => p.Value.Expires);

                    var pagesToRemove = orderedCache.Where((page, index) => index < toRemove || page.Value.Expires < time).ToList();

                    foreach (var page in pagesToRemove)
                    {
                        GzPageCache.Remove(page.Key);
                    }
                }
            }
        }
        #endregion

        #region Private functions        
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>Returns the string compressed into a GZipped byte array.</returns>
        private static byte[] Compress(string input)
        {
            if (input == null)
                return [];

            using MemoryStream ms = new();
            using (GZipStream zs = new(ms, CompressionMode.Compress, true))
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                zs.Write(inputBytes, 0, inputBytes.Length);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Gets the uncompressed string.
        /// </summary>
        /// <param name="input">The input byte array.</param>
        /// <returns>Returns the uncompressed string.</returns>
        private static string Decompress(byte[] input)
        {
            if (input == null)
                return string.Empty;

            using MemoryStream mso = new();
            using (MemoryStream ms = new(input))
            using (GZipStream zs = new(ms, CompressionMode.Decompress, true))
            {
                zs.CopyTo(mso);
            }

            return Encoding.UTF8.GetString(mso.ToArray());
        }
        #endregion
    }
}
