﻿using System;

namespace NetTally.Cache
{
    /// <summary>
    /// Class to save an object with an associated timestamp and expiration.
    /// Typical use would be to cache loaded web pages.
    /// TODO: Create unit test
    /// </summary>
    public class CacheObject<T> where T : class
    {
        public T Store { get; }
        public DateTime Timestamp { get; }
        public DateTime Expires { get; }

        public CacheObject(T store)
            : this(store, CacheInfo.DefaultExpiration, DateTime.Now)
        {
        }

        public CacheObject(T store, DateTime expires)
            : this(store, expires, DateTime.Now)
        {
        }

        public CacheObject(T store, DateTime expires, DateTime timestamp)
        {
            Store = store;
            Expires = expires;
            Timestamp = timestamp;
        }

        public override int GetHashCode()
        {
            return Store.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is CacheObject<T> cObj)
            {
                if (ReferenceEquals(this, cObj))
                    return true;
                else if (Store.Equals(cObj.Store))
                    return true;
                else
                    return false;
            }
            else if (obj is T objStore)
            {
                return Store.Equals(objStore);
            }
            else
            {
                return false;
            }
        }
    }
}
