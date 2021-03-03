using System;

namespace NetTally.Utility.Filtering
{
    /// <summary>
    /// Interface for a generic item filter.
    /// </summary>
    /// <typeparam name="T">The type of item being filtered.</typeparam>
    public interface IItemFilter<T>
    {
        /// <summary>
        /// Determines whether the filter allows the item provided to pass through the filter.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows(T item);

        /// <summary>
        /// Determines whether the filter blocks the item provided.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>True if the filter blocks the item, or false if not.</returns>
        public bool Blocks(T item);
    }
}
