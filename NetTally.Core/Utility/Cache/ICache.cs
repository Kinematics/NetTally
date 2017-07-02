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
        void SetClock(IClock clock);

        Task AddAsync(string key, T content, DateTime expires);
        Task<(bool found, T content)> GetAsync(string key);

        void InvalidateCache();
        void Clear();
    }
}
