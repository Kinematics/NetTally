using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Extensions
{
    /// <summary>
    /// Generic class for containing a group of items that are identified with the same key value.
    /// Used with GroupingExtensions.
    /// </summary>
    /// <typeparam name="TSource">Type of objects to place in the group list.</typeparam>
    /// <typeparam name="TKey">Type of object used as a key for the grouping.</typeparam>
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
}
