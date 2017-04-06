using System;

namespace NetTally.Utility
{
    /// <summary>
    /// Class to save an object with an associated timestamp.
    /// Default use would be to cache loaded web pages.
    /// </summary>
    public class CacheObject<T>
    {
        public DateTime Timestamp { get; }
        public T Store { get; }

        public CacheObject(T store)
            : this(store, new DefaultClock())
        {
        }

        public CacheObject(T store, IClock clock)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            Timestamp = clock.Now;
            Store = store;
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
