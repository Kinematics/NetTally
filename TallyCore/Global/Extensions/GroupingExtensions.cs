using System;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Extensions
{
    /// <summary>
    /// Extension methods to allow grouping collections in various ways.
    /// </summary>
    public static class GroupingExtensions
    {
        /// <summary>
        /// Group elements of a list together when a selector key is the same for each.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumerable list.</typeparam>
        /// <typeparam name="TKey">The type to use for the key to the group.</typeparam>
        /// <param name="source">Enumerable list we're working on.</param>
        /// <param name="keySelector">Function that converts an element of the list to a key value.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Group elements of a list together, where all null key results are added to any
        /// initial non-null key element.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumerable list.</typeparam>
        /// <typeparam name="TKey">The type to use for the key to the group.</typeparam>
        /// <param name="source">Enumerable list we're working on.</param>
        /// <param name="keySelector">Function that converts an element of the list to a key value.</param>
        /// <param name="nonNullKeySelector">Function that converts an element of the list to a key value,
        /// when the normal function would have returned null. Only used for the very first element of the list.</param>
        /// <returns></returns>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacentBySub<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TKey> nonNullKeySelector) where TKey : class
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
        /// Group elements of a list together when a selector key is the same for each.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumerable list.</typeparam>
        /// <typeparam name="int">The type to use for the key to the group.</typeparam>
        /// <param name="source">Enumerable list we're working on.</param>
        /// <param name="levelIndicator">Function that converts an element of the list to a key value.</param>
        /// <returns></returns>
        public static IEnumerable<IList<TSource>> GroupBlocks<TSource>(this IEnumerable<TSource> source,
            Func<TSource, int> levelIndicator)
        {
            int? parentLevel = null;
            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                int k = levelIndicator(s);

                if (parentLevel.HasValue)
                {
                    if (k > parentLevel)
                    {
                        list.Add(s);
                    }
                    else
                    {
                        yield return list;

                        list = new List<TSource> { s };
                        parentLevel = k;
                    }
                }
                else
                {
                    list.Add(s);
                    parentLevel = k;
                }
            }

            if (parentLevel.HasValue)
                yield return list;
        }
    }
}
