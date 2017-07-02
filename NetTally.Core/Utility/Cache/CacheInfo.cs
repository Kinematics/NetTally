using System;

namespace NetTally.Cache
{
    /// <summary>
    /// Global static values for cache classes.
    /// </summary>
    public static class CacheInfo
    {
        /// <summary>
        /// Value to use for 'expires' when you want an automatic selection of the expiration time for the cached item.
        /// </summary>
        public static readonly DateTime DefaultExpiration = DateTime.MinValue;
    }
}
