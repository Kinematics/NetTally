using System.Collections.Generic;
using System.Diagnostics.Contracts;

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
            Contract.Requires(list != null);

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
