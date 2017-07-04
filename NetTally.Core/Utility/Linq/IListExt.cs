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
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
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
    }
}
