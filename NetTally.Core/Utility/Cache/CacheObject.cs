namespace NetTally.Cache
{
    /// <summary>
    /// Class to save an object with an associated timestamp and expiration.
    /// Typical use would be to cache loaded web pages.
    /// </summary>
    public class CacheObject<T>(T store, DateTimeOffset expires, DateTimeOffset timestamp)
        : IEquatable<CacheObject<T>>, IEquatable<T>
        where T : class
    {
        public T Store { get; } = store;
        public DateTimeOffset Timestamp { get; } = timestamp;
        public DateTimeOffset Expires { get; } = expires;

        public CacheObject(T store)
            : this(store, CacheInfo.DefaultExpiration, CacheInfo.TimeProvider.GetLocalNow())
        { }

        public CacheObject(T store, DateTimeOffset expires)
            : this(store, expires, CacheInfo.TimeProvider.GetLocalNow())
        { }

        public CacheObject(T store, TimeSpan expiresIn)
            : this(store, CacheInfo.TimeProvider.GetLocalNow().Add(expiresIn), CacheInfo.TimeProvider.GetLocalNow())
        { }

        public override int GetHashCode()
        {
            return Store.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is CacheObject<T> cacheObject)
            {
                return Equals(cacheObject);
            }
            else if (obj is T objStore)
            {
                return Equals(objStore);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(CacheObject<T>? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null)
                return false;

            return Store == other.Store;
        }

        public bool Equals(T? other)
        {
            if (other is null)
                return false;

            return Store == other;
        }
    }
}
