using System;
using System.Threading.Tasks;
using NetTally.SystemInfo;

namespace NetTally.Cache
{
    /// <summary>
    /// Interaface for a class that handles caching data of arbitrary type.
    /// </summary>
    /// <typeparam name="T">The type of data that is cached.</typeparam>
    public interface ICache<T>
    {
        /// <summary>
        /// The maximum number of entries this cache will hold.
        /// </summary>
        int MaxCacheEntries { get; }
        /// <summary>
        /// The current number of entries held by the cache.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Clear the current cache contents.
        /// </summary>
        void Clear();

        /// <summary>
        /// Add an entry to the cache.
        /// </summary>
        /// <param name="key">The identifier for the cached object.</param>
        /// <param name="content">The object to cache.</param>
        void Add(string key, T content, DateTime expires);
        /// <summary>
        /// Try to get a cached object.
        /// </summary>
        /// <param name="key">The identifier of the object being requested.</param>
        /// <returns>Returns a tuple indicating whether the requested object was
        /// found, and cached object if available.</returns>
        (bool found, T content) Get(string key);

        /// <summary>
        /// Set the clock that will be used by the cache to determine when an etry expires.
        /// </summary>
        /// <param name="clock">The clock interface that will be used to determine timestamps.</param>
        void SetClock(IClock? clock);
        /// <summary>
        /// If our cache count is higher than our limit, then remove all expired entries,
        /// and a minimum number of pages to bring our count back down to the limit.
        /// </summary>
        void InvalidateCache();
    }
}
