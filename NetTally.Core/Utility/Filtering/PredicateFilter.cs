using System;

namespace NetTally.Utility.Filtering
{
    /// <summary>
    /// An item filter that determines whether an object is allowed by
    /// evaluating a predicate that analyzes the object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PredicateFilter<T> : IItemFilter<T>
    {
        protected readonly FilterType filterType;
        protected readonly Func<T, bool> predicate;

        protected PredicateFilter(Func<T, bool> predicate, FilterType filterType)
        {
            this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            this.filterType = filterType;
        }

        #region Factories used to construct varying types of list filters.
        public static PredicateFilter<T> Allow(Func<T, bool> predicate) => new(predicate, FilterType.Allow);
        public static PredicateFilter<T> Block(Func<T, bool> predicate) => new(predicate, FilterType.Block);

        public static readonly PredicateFilter<T> AllowAll = new((a) => true, FilterType.Allow);
        public static readonly PredicateFilter<T> BlockAll = new((a) => true, FilterType.Block);
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
                FilterType.Allow => predicate(item),
                FilterType.Block => !predicate(item),
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
                FilterType.Allow => !predicate(item),
                FilterType.Block => predicate(item),
                _ => throw new InvalidOperationException($"Invalid filter type: {filterType}")
            };
        }
    }
}
