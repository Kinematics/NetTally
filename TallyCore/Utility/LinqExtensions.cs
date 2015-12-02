using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Utility
{
    public class GroupOfAdjacent<TSource, TKey> : IEnumerable<TSource>, IGrouping<TKey, TSource>
    {
        public TKey Key { get; }
        private List<TSource> GroupList { get; }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TSource>)this).GetEnumerator();

        public IEnumerator<TSource> GetEnumerator()
        {
            foreach (var s in GroupList)
                yield return s;
        }

        public GroupOfAdjacent(List<TSource> source, TKey key)
        {
            GroupList = source;
            Key = key;
        }
    }

    public static class LinqExtensions
    {
        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacent<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            TKey last = default(TKey);
            bool haveLast = false;
            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                TKey k = keySelector(s);
                if (haveLast)
                {
                    if (!k.Equals(last))
                    {
                        yield return new GroupOfAdjacent<TSource, TKey>(list, last);
                        list = new List<TSource>();
                        list.Add(s);
                        last = k;
                    }
                    else
                    {
                        list.Add(s);
                        last = k;
                    }
                }
                else
                {
                    list.Add(s);
                    last = k;
                    haveLast = true;
                }
            }

            if (haveLast)
                yield return new GroupOfAdjacent<TSource, TKey>(list, last);
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacentBySub<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TKey> nonNullKeySelector)
        {
            TKey last = default(TKey);
            bool haveLast = false;
            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                TKey k = keySelector(s);
                if (haveLast)
                {
                    if (k == null)
                    {
                        list.Add(s);
                    }
                    else
                    {
                        yield return new GroupOfAdjacent<TSource, TKey>(list, last);
                        list = new List<TSource>();
                        list.Add(s);
                        last = k;
                    }
                }
                else
                {
                    list.Add(s);
                    if (k == null)
                        k = nonNullKeySelector(s);
                    last = k;
                    haveLast = true;
                }
            }

            if (haveLast)
                yield return new GroupOfAdjacent<TSource, TKey>(list, last);
        }


        /// <summary>
        /// Extension method to get the object with the minimum value from an enumerable list.
        /// </summary>
        /// <typeparam name="T">The type of object the list contains.</typeparam>
        /// <param name="self">The list.</param>
        /// <param name="comparer">Optional comparer object that can determine if one object is less than another.</param>
        /// <returns>Returns the object that has the lowest 'value'.</returns>
        public static T MinObject<T, U>(this IEnumerable<T> self, Func<T, U> transform, IComparer<U> comparer = null) where U : IComparable<U>
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            T min = default(T);
            U _min = default(U);
            bool first = true;

            foreach (T item in self)
            {
                U _item = transform(item);

                if (first)
                {
                    min = item;
                    _min = _item;
                    first = false;
                }
                else if (comparer != null)
                {
                    if (comparer.Compare(_item, _min) < 0)
                    {
                        min = item;
                        _min = _item;
                    }
                }
                else if (_item.CompareTo(_min) < 0)
                {
                    min = item;
                    _min = _item;
                }
            }

            return min;
        }

        /// <summary>
        /// Extension method to get the object with the maximum value from an enumerable list.
        /// </summary>
        /// <typeparam name="T">The type of object the list contains.</typeparam>
        /// <param name="self">The list.</param>
        /// <param name="comparer">Optional comparer object that can determine if one object is greater than another.</param>
        /// <returns>Returns the object that has the highest 'value'.</returns>
        public static T MaxObject<T, U>(this IEnumerable<T> self, Func<T, U> transform, IComparer<U> comparer = null) where U : IComparable<U>
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            T max = default(T);
            U _max = default(U);
            bool first = true;

            foreach (T item in self)
            {
                U _item = transform(item);

                if (first)
                {
                    max = item;
                    _max = _item;
                    first = false;
                }
                else if (comparer != null)
                {
                    if (comparer.Compare(_item, _max) > 0)
                    {
                        max = item;
                        _max = _item;
                    }
                }
                else if (_item.CompareTo(_max) > 0)
                {
                    max = item;
                    _max = _item;
                }
            }

            return max;
        }
    }
}
