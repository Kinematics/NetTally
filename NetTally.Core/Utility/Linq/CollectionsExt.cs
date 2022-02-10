using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NetTally.Extensions
{
    /// <summary>
    /// Extension methods for various collections.
    /// </summary>
    public static class CollectionsExt
    {
        /// <summary>
        /// Swap two values in a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list to swap data in.</param>
        /// <param name="firstIndex">The first index value being swapped.</param>
        /// <param name="secondIndex">The second index value being swapped.</param>
        public static void Swap<T>(this IList<T> list, int firstIndex, int secondIndex)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (firstIndex == secondIndex)
                return;
            if (firstIndex < 0 || firstIndex >= list.Count || secondIndex < 0 || secondIndex >= list.Count)
                return;

            T temp = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = temp;
        }

        /// <summary>
        /// Does an in-place sort the specified collection.
        /// </summary>
        /// <typeparam name="T">The type of object held in the collection.</typeparam>
        /// <param name="collection">The collection to be sorted.</param>
        public static void Sort<T>(this ObservableCollection<T> collection) where T : IComparable
        {
            var sorted = collection.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count(); i++)
            {
                int src = collection.IndexOf(sorted[i]);
                if (src != i)
                    collection.Move(src, i);
            }
        }

        /// <summary>
        /// Find the index of the requested object within the readonly list.
        /// Uses a sequential search.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list.</typeparam>
        /// <param name="list">The list being scanned.</param>
        /// <param name="obj">The object being searched for.</param>
        /// <returns>Returns the index the object was found at, or -1 if not found.</returns>
        public static int IndexOf<T>(this IReadOnlyList<T> list, T obj) where T: IEquatable<T>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(obj))
                    return i;
            }

            return -1;
        }

        public static TValue? GetValueOrDefault1<TKey, TValue> (this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class
        {
            return dictionary.TryGetValue(key, out TValue? value) ? value : default;
        }
    }
}
