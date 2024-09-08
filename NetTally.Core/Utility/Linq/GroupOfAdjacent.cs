using System.Collections;

namespace NetTally.Extensions
{
    /// <summary>
    /// Generic class for containing a group of items that are identified with the same key value.
    /// Used with GroupingExtensions.
    /// </summary>
    /// <typeparam name="TSource">Type of objects to place in the group list.</typeparam>
    /// <typeparam name="TKey">Type of object used as a key for the grouping.</typeparam>
    public class GroupOfAdjacent<TSource, TKey>(List<TSource> source, TKey key)
        : IEnumerable<TSource>, IGrouping<TKey, TSource>
    {
        public TKey Key { get; } = key;
        private List<TSource> GroupList { get; } = source;

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TSource>)this).GetEnumerator();

        public IEnumerator<TSource> GetEnumerator()
        {
            foreach (var s in GroupList)
                yield return s;
        }
    }
}
