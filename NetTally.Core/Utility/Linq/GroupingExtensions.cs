using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace NetTally.Extensions
{
    /// <summary>
    /// Extension methods to allow grouping collections in various ways.
    /// </summary>
    static class GroupingExtensions
    {

        // Group an enumerable list in various ways, in a single pass of reading the enumerable:
        //
        // 1) Group adjacent elements when the key each element generates is the same as a prior key.
        //  EG: Key(2) = Even, Key(4) = Even, Key(5) = Odd
        //  The key is defined by something intrinsic to the element, and the same key means the same group.
        //
        // 2) Group elements as long as a new 'acceptable' key is not found.
        //  EG: Key([x] One) = Parent, Key(-[x] Two) = Child, Key(-[x] Three) = Child, Key([x] Four) = Parent
        //  What counts as a key is defined, and anything that is not a new key is grouped with the previous key.
        //
        // 3) Same as #2, but grouped elements need to match some aspect of the parent element as well.
        //


        /// <summary>
        /// Groups elements of an enumeration together when adjacent elements generate the
        /// same grouping key value.
        /// This is a type 1 grouping.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumeration.</typeparam>
        /// <typeparam name="TKey">The type of grouping key object to create.</typeparam>
        /// <param name="source">The enumerable source list.</param>
        /// <param name="keySelector">The function that generates a key from a source element.</param>
        /// <returns>Returns an enumeration of grouped collections.</returns>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacentBySimilarKey<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector) where TKey : IEquatable<TKey>
        {
            TKey lastKey = default;
            bool haveKey = false;

            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                TKey k = keySelector(s);

                if (haveKey)
                {
                    if (k.Equals(lastKey))
                    {
                        list.Add(s);
                    }
                    else
                    {
                        yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
                        list = new List<TSource> { s };
                        lastKey = k;
                    }
                }
                else
                {
                    list.Add(s);
                    lastKey = k;
                    haveKey = true;
                }
            }

            if (haveKey)
                yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
        }


        /// <summary>
        /// Group elements of an enumeration together based on instances of elements that qualify
        /// as keys.  All following elements that do not qualify as their own keys get added to 
        /// the previous key.
        /// This is a type 2 grouping.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumeration.</typeparam>
        /// <typeparam name="TKey">The type of grouping key object to create.</typeparam>
        /// <param name="source">The enumerable source list.</param>
        /// <param name="hasKey">Function to indicate whether a source element has a key.</param>
        /// <param name="defaultKey">The default key if no prior key exists.</param>
        /// <param name="keySelector">The function that generates a key from a source element.</param>
        /// <returns>Returns an enumeration of grouped collections.</returns>
        public static IEnumerable<GroupOfAdjacent<TSource, TKey>> GroupAdjacentToPreviousKey<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> hasKey,
            Func<TSource, TKey> defaultKey,
            Func<TSource, TKey> keySelector)
        {
            TKey lastKey = default;
            bool haveKey = false;

            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                if (haveKey)
                {
                    if (hasKey(s))
                    {
                        yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
                        lastKey = keySelector(s);
                        list = new List<TSource> { s };
                    }
                    else
                    {
                        list.Add(s);
                    }
                }
                else
                {
                    lastKey = hasKey(s) ? keySelector(s) : defaultKey(s);

                    list.Add(s);
                    haveKey = true;
                }
            }

            if (haveKey)
                yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
        }


        /// <summary>
        /// Groups elements of an enumeration together based on whether a complete comparison
        /// between the current element and the most recent key element match in a sufficient
        /// manner.
        /// This is a type 3 grouping.
        /// </summary>
        /// <typeparam name="TSource">The type of data in the enumeration.</typeparam>
        /// <typeparam name="TKey">The type of grouping key object to create.</typeparam>
        /// <param name="source">The enumerable source list.</param>
        /// <param name="keySelector">The function that generates a key from a source element.</param>
        /// <param name="sourceMatches">The function that compares the most recent key source
        /// to the current key source, and determines whether the current source can be added
        /// to the group.</param>
        /// <returns>Returns an enumeration of grouped collections.</returns>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacentToPreviousSource<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TSource, bool> sourceMatches
            )
        {
            TKey lastKey = default;
            bool haveKey = false;
            TSource lastKeySource = default;

            List<TSource> list = new List<TSource>();

            foreach (TSource s in source)
            {
                if (haveKey)
                {
                    if (sourceMatches(lastKeySource, s))
                    {
                        list.Add(s);
                    }
                    else
                    {
                        yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
                        list = new List<TSource> { s };
                        lastKey = keySelector(s);
                        lastKeySource = s;
                    }
                }
                else
                {
                    list.Add(s);
                    lastKey = keySelector(s);
                    lastKeySource = s;
                    haveKey = true;
                }
            }

            if (haveKey)
                yield return new GroupOfAdjacent<TSource, TKey>(list, lastKey);
        }
    }
}
