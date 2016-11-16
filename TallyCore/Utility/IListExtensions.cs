using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            //Contract.Requires(firstIndex >= 0 && firstIndex < list.Count);
            //Contract.Requires(secondIndex >= 0 && secondIndex < list.Count);

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
