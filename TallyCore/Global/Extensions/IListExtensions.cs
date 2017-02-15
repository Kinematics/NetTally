using System;
using System.Collections.Generic;

namespace NetTally.Utility
{
    public static class IListExtensions
    {
        public static void Swap<T>(
            this IList<T> list,
            int firstIndex,
            int secondIndex
            )
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
