using System;

namespace NetTally.Utility
{
    /// <summary>
    /// Class to save an object with an associated timestamp.
    /// Default use would be to cache loaded web pages.
    /// </summary>
    public class CacheObject<T> where T : class
    {
        public DateTime Timestamp { get; }
        public DateTime Expires { get; }
        public T Store { get; }

        public static readonly DateTime DefaultExpiration = DateTime.MinValue;

        readonly TimeSpan defaultExpirationDelay = TimeSpan.FromMinutes(30);


        public CacheObject(T store)
            : this(store, new SystemClock(), DefaultExpiration)
        {
        }

        public CacheObject(T store, IClock clock)
            : this(store, clock, DefaultExpiration)
        {
        }

        public CacheObject(T store, IClock clock, DateTime expires)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Timestamp = clock?.Now ?? throw new ArgumentNullException(nameof(clock));

            if (expires == DefaultExpiration)
                expires = Timestamp.Add(defaultExpirationDelay);

            Expires = expires;
        }

        public override int GetHashCode()
        {
            return Store.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Store.Equals(obj);
        }
    }
}
