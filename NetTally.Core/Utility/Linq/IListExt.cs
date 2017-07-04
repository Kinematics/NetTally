using System;
using System.Collections.Generic;

namespace NetTally.Extensions
{
    public static class IListExt
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
    }
}
