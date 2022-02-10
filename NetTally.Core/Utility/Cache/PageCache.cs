using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally.SystemInfo;
using Nito.AsyncEx;

namespace NetTally.Cache
{
    /// <summary>
    /// Class to handle caching web content.
    /// Uses compression on cached web pages.
    /// </summary>
    public sealed class PageCache : ICache<string>, IDisposable
    {
        #region Lazy singleton creation
        static readonly Lazy<PageCache> lazy = new Lazy<PageCache>(() => new PageCache());
        public static PageCache Instance => lazy.Value;
        #endregion

        #region Constructor
        public PageCache(IClock? clock = null)
        {
            if (clock != null)
                SetClock(clock);
        }
        #endregion

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

        readonly AsyncReaderWriterLock cacheLock = new AsyncReaderWriterLock();

        const int maxCacheEntries = 100;

        Dictionary<string, CacheObject<byte[]>> gzPageCache { get; } = new Dictionary<string, CacheObject<byte[]>>(maxCacheEntries);

        readonly TimeSpan defaultExpirationDelay = TimeSpan.FromMinutes(60);

        IClock Clock { get; set; } = new SystemClock();

        #endregion

        #region Public interface
        /// <summary>
        /// The maximum number of entries this cache will hold.
        /// </summary>
        public int MaxCacheEntries => maxCacheEntries;

        /// <summary>
        /// The current number of entries held by the cache.
        /// </summary>
        public int Count => gzPageCache.Count;

        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            using (cacheLock.WriterLock())
            {
                gzPageCache.Clear();
            }
        }

        /// <summary>
        /// Set the clock that will be used by the cache to determine when an etry expires.
        /// </summary>
        /// <param name="clock">The clock interface that will be used to determine timestamps.</param>
        public void SetClock(IClock? clock)
        {
            using (cacheLock.ReaderLock())
            {
                Clock = clock ?? new SystemClock();
            }
        }

        /// <summary>
        /// Add a web document to the cache.
        /// </summary>
        /// <param name="key">The URL the document was retrieved from.</param>
        /// <param name="content">The HTML document text to cache.</param>
        public void Add(string key, string content, DateTime expires)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var now = Clock.Now;

            if (expires == CacheInfo.DefaultExpiration)
                expires = now.Add(defaultExpirationDelay);

            byte[] zipped = Compress(content);

            var toGZCache = new CacheObject<byte[]>(zipped, expires, now);

            using (cacheLock.WriterLock())
            {
                gzPageCache[key] = toGZCache;
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
            CacheObject<byte[]>? gzCache;

            using (cacheLock.ReaderLock())
            {
                if (gzPageCache.TryGetValue(key, out gzCache))
                {
                    if (gzCache.Expires > Clock.Now)
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
            var time = Clock.Now;

            using (cacheLock.WriterLock())
            {
                if (gzPageCache.Count > MaxCacheEntries)
                {
                    int toRemove = gzPageCache.Count - MaxCacheEntries;

                    var orderedCache = gzPageCache.OrderBy(p => p.Value.Expires);

                    var pagesToRemove = orderedCache.Where((page, index) => index < toRemove || page.Value.Expires < time).ToList();

                    foreach (var page in pagesToRemove)
                    {
                        gzPageCache.Remove(page.Key);
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
        private byte[] Compress(string input)
        {
            if (input == null)
                return new byte[0];

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    zs.Write(inputBytes, 0, inputBytes.Length);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets the uncompressed string.
        /// </summary>
        /// <param name="input">The input byte array.</param>
        /// <returns>Returns the uncompressed string.</returns>
        private string Decompress(byte[] input)
        {
            if (input == null)
                return string.Empty;

            using (MemoryStream mso = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(input))
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    zs.CopyTo(mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>Returns the string compressed into a GZipped byte array.</returns>
        private async Task<byte[]> CompressAsync(string input)
        {
            if (input == null)
                return new byte[0];

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    await zs.WriteAsync(inputBytes, 0, inputBytes.Length).ConfigureAwait(false);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets the uncompressed string.
        /// </summary>
        /// <param name="input">The input byte array.</param>
        /// <returns>Returns the uncompressed string.</returns>
        private async Task<string> DecompressAsync(byte[] input)
        {
            if (input == null)
                return string.Empty;

            using (MemoryStream mso = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(input))
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    await zs.CopyToAsync(mso).ConfigureAwait(false);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
        #endregion
    }
}
