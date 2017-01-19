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
        /// Group elements of a list together, given a comparison function that returns
        /// true if it's time to create a new group.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumerable list.</typeparam>
        /// <typeparam name="TKey">The type to use for the key to the group.</typeparam>
        /// <param name="source">Enumerable list we're working on.</param>
        /// <param name="keySelector">Function that converts an element of the list to a key value.</param>
        /// <param name="groupComparison">Function that returns true if a new source item should start a new group.</param>
        /// <returns>Returns an IEnumerable grouping of the provided source.</returns>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacentByComparison<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, Func<TSource, TKey, bool> groupComparison) where TKey : class
        {
            TKey lastKey = default(TKey);
            bool haveLast = false;
            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                TKey k = keySelector(s);
                if (haveLast)
                {
                    if (groupComparison(s, lastKey))
                    {
                        yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
                        list = new List<TSource> { s };
                        lastKey = k;
                    }
                    else
                    {
                        list.Add(s);
                    }
                }
                else
                {
                    list.Add(s);
                    lastKey = k;
                    haveLast = true;
                }
            }

            if (haveLast)
                yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
        }

        /// <summary>
        /// Group elements of a list together, given a comparison function that returns
        /// true if it's time to create a new group.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumerable list.</typeparam>
        /// <typeparam name="TKey">The type to use for the key to the group.</typeparam>
        /// <param name="source">Enumerable list we're working on.</param>
        /// <param name="keySelector">Function that converts an element of the list to a key value.</param>
        /// <param name="sourceFilter">Condition check on the current element and the last key element
        /// before allowing it to check for a key value.</param>
        /// <param name="newGroupTest">Function that returns true if a new source item should start a new group.</param>
        /// <returns>Returns an IEnumerable grouping of the provided source.</returns>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacentByComparison<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TSource, bool> sourceFilter,
            Func<TSource, TKey> keySelector, 
            Func<TSource, TKey, TSource, bool> newGroupTest) where TKey : class
        {
            TKey lastKey = default(TKey);
            TSource lastKeySource = default(TSource);
            bool haveLast = false;
            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                if (sourceFilter(s, lastKeySource))
                {
                    if (haveLast)
                    {
                        if (newGroupTest(s, lastKey, lastKeySource))
                        {
                            yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
                            list = new List<TSource> { s };
                            lastKey = keySelector(s);
                            lastKeySource = s;
                        }
                        else
                        {
                            list.Add(s);
                        }
                    }
                    else
                    {
                        list.Add(s);
                        lastKey = keySelector(s);
                        lastKeySource = s;
                        haveLast = true;
                    }
                }
            }

            if (haveLast)
                yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
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
