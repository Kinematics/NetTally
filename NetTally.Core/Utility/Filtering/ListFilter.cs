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
        protected readonly FilterType filterType;
        protected readonly HashSet<T> filterList;

        /// <summary>
        /// A private constructor for the list filter.
        /// The only way to get a new list filter is through one of the factory methods.
        /// </summary>
        /// <param name="listFilterType">The type of filter process to use.</param>
        /// <param name="list">The list of items that defines the filter.</param>
        protected ListFilter(IEnumerable<T> list, FilterType listFilterType)
        {
            this.filterType = listFilterType;
            filterList = list.ToHashSet();
        }

        #region Factories used to construct varying types of list filters.
        public static ListFilter<T> Whitelist(IEnumerable<T> list) => new(list, FilterType.Allow);
        public static ListFilter<T> Blacklist(IEnumerable<T> list) => new(list, FilterType.Block);

        public static readonly ListFilter<T> AllowAll = new(Enumerable.Empty<T>(), FilterType.Block);
        public static readonly ListFilter<T> BlockAll = new(Enumerable.Empty<T>(), FilterType.Allow);
        #endregion


        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows(T item)
        {
            return filterType switch
            {
                FilterType.Allow => filterList.Contains(item),
                FilterType.Block => !filterList.Contains(item),
                _ => throw new InvalidOperationException($"Invalid filter type: {filterType}")
            };
        }

        /// <summary>
        /// Determines whether the filter blocks the item provided.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter blocks the item, or false if not.</returns>
        public bool Blocks(T item)
        {
            return filterType switch
            {
                FilterType.Allow => !filterList.Contains(item),
                FilterType.Block => filterList.Contains(item),
                _ => throw new InvalidOperationException($"Invalid filter type: {filterType}")
            };
        }
    }
}
