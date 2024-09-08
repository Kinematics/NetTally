namespace NetTally.Cache
{
    /// <summary>
    /// Global static values for cache classes.
    /// </summary>
    public static class CacheInfo
    {
        /// <summary>
        /// A default time provider, which can be modified if testing.
        /// </summary>
        public static TimeProvider TimeProvider { get; set; } = TimeProvider.System;

        /// <summary>
        /// Value to use for 'expires' when you want an automatic selection of the expiration time for the cached item.
        /// </summary>
        public static readonly DateTimeOffset DefaultExpiration = DateTimeOffset.MinValue;
    }
}
