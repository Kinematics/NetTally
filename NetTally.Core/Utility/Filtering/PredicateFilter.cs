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
        private readonly Func<T, bool> predicate;

        public PredicateFilter(Func<T, bool> predicate)
        {
            this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows(T item)
        {
            return predicate(item);
        }
    }
}
