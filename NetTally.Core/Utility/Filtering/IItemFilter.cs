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
        /// Allows passing a non-T item to the filter check, if you also
        /// pass a function that will convert the item to a T.
        /// </summary>
        /// <typeparam name="U">The type of the item being passed.</typeparam>
        /// <param name="item">The item to be checked.</param>
        /// <param name="map">A mapping function to turn a U into a T.</param>
        /// <returns>True if the filter allows the item, or false if not.</returns>
        public bool Allows<U>(U item, Func<U, T> map);
    }
}
