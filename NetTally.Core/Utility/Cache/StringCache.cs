using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally.Extensions;
using NetTally.SystemInfo;
using Nito.AsyncEx;

namespace NetTally.Cache
{
    /// <summary>
    /// Class to handle caching web content.
    /// Uses compression on cached web pages.
    /// </summary>
    public sealed class GZStringCache : IDisposable, ICache<string>
    {
        #region Lazy singleton creation
        static readonly Lazy<GZStringCache> lazy = new Lazy<GZStringCache>(() => new GZStringCache());
        public static GZStringCache Instance => lazy.Value;

        GZStringCache()
        {
            SetClock(null);
        }
        #endregion

        #region Disposal
        ~GZStringCache()
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

        IClock Clock { get; set; } = new SystemClock();

        const int MaxCacheEntries = 100;
        readonly TimeSpan defaultExpirationDelay = TimeSpan.FromMinutes(60);
        Dictionary<string, CacheObject<byte[]>> GZPageCache { get; } = new Dictionary<string, CacheObject<byte[]>>(MaxCacheEntries);

        readonly AsyncReaderWriterLock cacheLock = new AsyncReaderWriterLock();
        #endregion

        #region Public functions
        /// <summary>
        /// Allow setting the clock interface to be used by the cache.
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
        /// Add the original HTML string to the cache.
        /// </summary>
        /// <param name="key">The URL the document was retrieved from.</param>
        /// <param name="content">The HTML string to cache.</param>
        public async Task AddAsync(string key, string content, DateTime expires)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var now = Clock.Now;

            if (expires == CacheInfo.DefaultExpiration)
                expires = now.Add(defaultExpirationDelay);

            byte[] zipped = await Compress(content);

            var toGZCache = new CacheObject<byte[]>(zipped, expires, now);

            using (cacheLock.WriterLock())
            {
                GZPageCache[key] = toGZCache;
            }
        }

        /// <summary>
        /// Try to get a cached document for a specified URL.
        /// </summary>
        /// <param name="key">The URL being checked.</param>
        /// <returns>Returns the document for the URL if it's available and less than 30 minutes old.
        /// Otherwise returns null.</returns>
        public async Task<(bool found, string content)> GetAsync(string key)
        {
            bool found = false;
            CacheObject<byte[]> gzCache;

            using (cacheLock.ReaderLock())
            {
                found = GZPageCache.TryGetValue(key, out gzCache);
            }

            if (found)
            { 
                if (gzCache.Expires > Clock.Now)
                {
                    string content = await Decompress(gzCache.Store).ConfigureAwait(false);

                    return (true, content);
                }
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// If our cache count is higher than our limit, then remove all expired entries,
        /// and a minimum number of pages to bring out count back down to the limit.
        /// </summary>
        public void InvalidateCache()
        {
            var time = Clock.Now;

            using (cacheLock.WriterLock())
            {
                if (GZPageCache.Count > MaxCacheEntries)
                {
                    int toRemove = GZPageCache.Count - MaxCacheEntries;

                    var orderedCache = GZPageCache.OrderBy(p => p.Value.Expires);

                    var pagesToRemove = orderedCache.Where((page, index) => index < toRemove || page.Value.Expires < time).ToList();

                    foreach (var page in pagesToRemove)
                    {
                        GZPageCache.Remove(page.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Clear the current cache.
        /// </summary>
        public void Clear()
        {
            using (cacheLock.WriterLock())
            {
                GZPageCache.Clear();
            }
        }
        #endregion

        #region Private functions        
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>Returns the string compressed into a GZipped byte array.</returns>
        private async Task<byte[]> Compress(string input)
        {
            if (input == null)
                return null;

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
        private async Task<string> Decompress(byte[] input)
        {
            if (input == null)
                return null;

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
