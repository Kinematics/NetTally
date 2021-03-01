using System;
using System.Collections.Generic;
using System.Linq;

namespace NetTally.Utility.Filtering
{
    /// <summary>
    /// An item filter that determines whether an object is allowed by
    /// checking against either a whitelist or a blacklist.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListFilter<T> : IItemFilter<T>
    {
        protected readonly ListFilterType listFilterType;
        protected readonly HashSet<T> filterList;

        /// <summary>
        /// A private constructor for the list filter.
        /// The only way to get a new list filter is through one of the factory methods.
        /// </summary>
        /// <param name="listFilterType">The type of filter process to use.</param>
        /// <param name="list">The list of items that defines the filter.</param>
        protected ListFilter(ListFilterType listFilterType, IEnumerable<T> list)
        {
            this.listFilterType = listFilterType;
            filterList = list.ToHashSet();
        }

        #region Factories used to construct varying types of list filters.
        public static ListFilter<T> Whitelist(IEnumerable<T> list) => new ListFilter<T>(ListFilterType.Include, list);
        public static ListFilter<T> Blacklist(IEnumerable<T> list) => new ListFilter<T>(ListFilterType.Exclude, list);
        public static ListFilter<T> WhitelistAll() => new ListFilter<T>(ListFilterType.Exclude, Enumerable.Empty<T>());
        public static ListFilter<T> BlacklistAll() => new ListFilter<T>(ListFilterType.Include, Enumerable.Empty<T>());
        public static ListFilter<T> IgnoreAll() => new ListFilter<T>(ListFilterType.Ignore, Enumerable.Empty<T>());
        #endregion


        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows(T item)
        {
            return listFilterType switch
            {
                ListFilterType.Ignore => true,
                ListFilterType.Include => filterList.Contains(item),
                ListFilterType.Exclude => !filterList.Contains(item),
                _ => throw new InvalidOperationException($"Unknown filter type: {listFilterType}")
            };
        }

        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <typeparam name="U">The type of object being passed in.</typeparam>
        /// <param name="item">The item being checked.</param>
        /// <param name="map">A function that maps a U to a string.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows<U>(U item, Func<U, T> map)
        {
            return Allows(map(item));
        }
    }
}
