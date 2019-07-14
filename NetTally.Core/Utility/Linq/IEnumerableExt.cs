using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace NetTally.Extensions
{
    /// <summary>
    /// Class for generic LINQ extension methods.
    /// </summary>
    static class IEnumerableExt
    {
#nullable disable
        /// <summary>
        /// Extension method to get the object with the minimum value from an enumerable list.
        /// </summary>
        /// <typeparam name="T">The type of object the list contains.</typeparam>
        /// <param name="self">The list.</param>
        /// <param name="transform">Transform each T object to a U object for the sake of comparison.</param>
        /// <returns>Returns the object that has the lowest 'value'.</returns>
        public static T MinObject<T, U>(this IEnumerable<T> self, Func<T, U> transform) where U : IComparable<U> => MinObject(self, transform, null);

        /// <summary>
        /// Extension method to get the object with the minimum value from an enumerable list.
        /// Overload that allows explicitly providing a comparer.
        /// </summary>
        /// <typeparam name="T">The type of object the list contains.</typeparam>
        /// <param name="self">The list.</param>
        /// <param name="transform">Transform each T object to a U object for the sake of comparison.</param>
        /// <param name="comparer">Optional comparer object that can determine if one object is less than another.</param>
        /// <returns>Returns the object that has the lowest 'value'.</returns>
        public static T MinObject<T, U>(this IEnumerable<T> self, Func<T, U> transform, IComparer<U> comparer) where U : IComparable<U>
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            T min = default;
            U _min = default;
            bool first = true;

            foreach (T item in self)
            {
                U _item = transform(item);

                if (first)
                {
                    min = item;
                    _min = _item;
                    first = false;
                }
                else if (comparer != null)
                {
                    if (comparer.Compare(_item, _min) < 0)
                    {
                        min = item;
                        _min = _item;
                    }
                }
                else if (_item.CompareTo(_min) < 0)
                {
                    min = item;
                    _min = _item;
                }
            }

            return min;
        }

        /// <summary>
        /// Extension method to get the object with the maximum value from an enumerable list.
        /// </summary>
        /// <typeparam name="T">The type of object the list contains.</typeparam>
        /// <param name="self">The list.</param>
        /// <param name="transform">Transform each T object to a U object for the sake of comparison.</param>
        /// <returns>Returns the object that has the lowest 'value'.</returns>
        public static T MaxObject<T, U>(this IEnumerable<T> self, Func<T, U> transform) where U : IComparable<U> => MaxObject(self, transform, null);

        /// <summary>
        /// Extension method to get the object with the maximum value from an enumerable list.
        /// Overload that allows explicitly providing a comparer.
        /// </summary>
        /// <typeparam name="T">The type of object the list contains.</typeparam>
        /// <param name="self">The list.</param>
        /// <param name="transform">Transform each T object to a U object for the sake of comparison.</param>
        /// <param name="comparer">Optional comparer object that can determine if one object is greater than another.</param>
        /// <returns>Returns the object that has the highest 'value'.</returns>
        public static T MaxObject<T, U>(this IEnumerable<T> self, Func<T, U> transform, IComparer<U> comparer) where U : IComparable<U>
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            T max = default;
            U _max = default;
            bool first = true;

            foreach (T item in self)
            {
                U _item = transform(item);

                if (first)
                {
                    max = item;
                    _max = _item;
                    first = false;
                }
                else if (comparer != null)
                {
                    if (comparer.Compare(_item, _max) > 0)
                    {
                        max = item;
                        _max = _item;
                    }
                }
                else if (_item.CompareTo(_max) > 0)
                {
                    max = item;
                    _max = _item;
                }
            }

            return max;
        }

        /// <summary>
        /// Returns a collection of items from the provided enumerable that match the
        /// minimum result of the given transformation function.
        /// For example, given a collection of People objects, and a function that uses
        /// the Age as the comparison, it will return an enumerable of all People that
        /// have the minimum age in the collection.
        /// Uses the default comparer.
        /// </summary>
        /// <typeparam name="T">The type of object in the collection.</typeparam>
        /// <typeparam name="U">The type to compare for each object in the collection.</typeparam>
        /// <param name="self">The enumeration being filtered.</param>
        /// <param name="transform">The transform function.</param>
        /// <returns>Returns an enumeration of objects that have the minimum transform value.</returns>
        public static IEnumerable<T> WithMin<T, U>(this IEnumerable<T> self, Func<T, U> transform) where U : IComparable<U> =>
            WithMin(self, transform, null);

        /// <summary>
        /// Returns a collection of items from the provided enumerable that match the
        /// minimum result of the given transformation function.
        /// For example, given a collection of People objects, and a function that uses
        /// the Age as the comparison, it will return an enumerable of all People that
        /// have the minimum age in the collection.
        /// </summary>
        /// <typeparam name="T">The type of object in the collection.</typeparam>
        /// <typeparam name="U">The type to compare for each object in the collection.</typeparam>
        /// <param name="self">The enumeration being filtered.</param>
        /// <param name="transform">The transform function.</param>
        /// <returns>Returns an enumeration of objects that have the minimum transform value.</returns>
        public static IEnumerable<T> WithMin<T, U>(this IEnumerable<T> self, Func<T, U> transform, IComparer<U> comparer) where U : IComparable<U>
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));


            List<T> result = new List<T>();
            U min = default;
            bool first = true;

            foreach (T item in self)
            {
                U trans = transform(item);

                if (first)
                {
                    min = trans;
                    result.Add(item);
                    first = false;
                }
                else
                {
                    int comparison = 0;

                    if (comparer == null)
                        comparison = trans.CompareTo(min);
                    else
                        comparison = comparer.Compare(trans, min);

                    if (comparison == 0)
                    {
                        result.Add(item);
                    }
                    else if (comparison < 0)
                    {
                        min = trans;
                        result.Clear();
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a collection of items from the provided enumerable that match the
        /// minimum result of the given transformation function.
        /// For example, given a collection of People objects, and a function that uses
        /// the Age as the comparison, it will return an enumerable of all People that
        /// have the minimum age in the collection.
        /// Uses the default comparer.
        /// </summary>
        /// <typeparam name="T">The type of object in the collection.</typeparam>
        /// <typeparam name="U">The type to compare for each object in the collection.</typeparam>
        /// <param name="self">The enumeration being filtered.</param>
        /// <param name="transform">The transform function.</param>
        /// <returns>Returns an enumeration of objects that have the minimum transform value.</returns>
        public static IEnumerable<T> WithMax<T, U>(this IEnumerable<T> self, Func<T, U> transform) where U : IComparable<U> =>
            WithMax(self, transform, null);

        /// <summary>
        /// Returns a collection of items from the provided enumerable that match the
        /// minimum result of the given transformation function.
        /// For example, given a collection of People objects, and a function that uses
        /// the Age as the comparison, it will return an enumerable of all People that
        /// have the minimum age in the collection.
        /// </summary>
        /// <typeparam name="T">The type of object in the collection.</typeparam>
        /// <typeparam name="U">The type to compare for each object in the collection.</typeparam>
        /// <param name="self">The enumeration being filtered.</param>
        /// <param name="transform">The transform function.</param>
        /// <returns>Returns an enumeration of objects that have the minimum transform value.</returns>
        public static IEnumerable<T> WithMax<T, U>(this IEnumerable<T> self, Func<T, U> transform, IComparer<U> comparer) where U : IComparable<U>
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));


            List<T> result = new List<T>();
            U max = default;
            bool first = true;

            foreach (T item in self)
            {
                U trans = transform(item);

                if (first)
                {
                    max = trans;
                    result.Add(item);
                    first = false;
                }
                else
                {
                    int comparison = 0;

                    if (comparer == null)
                        comparison = trans.CompareTo(max);
                    else
                        comparison = comparer.Compare(trans, max);

                    if (comparison == 0)
                    {
                        result.Add(item);
                    }
                    else if (comparison > 0)
                    {
                        max = trans;
                        result.Clear();
                        result.Add(item);
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Traverses the list, and returns the list plus children in a flattened format.
        /// </summary>
        /// <typeparam name="T">The class type of the main list.</typeparam>
        /// <typeparam name="U">The type of object selected from each list item, to filter and return.</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="childSelector">A function to select child nodes of a given node.</param>
        /// <param name="nodeSelector">A function to select the part of the list item that you're filtering and returning.</param>
        /// <param name="filter">An optional predicate filter that will prevent traversal of any nodes or their children.</param>
        /// <returns>Returns a (potentially filtered) list of items, including all children of items from the initial list.</returns>
        /// <exception cref="System.ArgumentNullException">Throw if <paramref name="childSelector"/> or <paramref name="nodeSelector"/>
        /// is null.</exception>
        public static IEnumerable<U> TraverseList<T, U>(this IEnumerable<T> items,
            Func<T, IEnumerable<T>> childSelector, Func<T, U> nodeSelector, Func<U, bool> filter)
        {
            if (childSelector == null)
                throw new ArgumentNullException(nameof(childSelector));
            if (nodeSelector == null)
                throw new ArgumentNullException(nameof(nodeSelector));

            var list = new LinkedList<T>(items);
            while (list.Any())
            {
                var next = list.First();
                list.RemoveFirst();

                // Don't process children of any filtered nodes.
                if (filter == null || !filter(nodeSelector(next)))
                {
                    yield return nodeSelector(next);

                    // Reverse the childSelector's results when we push onto the list,
                    // because we want to pull them off of the list in the original order.
                    foreach (var child in childSelector(next).Reverse())
                        list.AddFirst(child);
                }
            }
        }

        /// <summary>
        /// Determine if most (as determined by the provided threshold) of the elements of
        /// an enumerated sequence pass a given check.
        /// Aside: "Most" would largely fall between 68% and 95%, centering around 82%.
        /// </summary>
        /// <typeparam name="T">The type of the enumerable.</typeparam>
        /// <param name="items">The list of items to check.</param>
        /// <param name="predicate">The predicate test to use on each item. Has a default value of 83% (5/6 success rate would pass).</param>
        /// <param name="threshold">What percentage of the checks must pass for the function to return true.</param>
        /// <returns>Returns true if most of the items in the sequence pass the predicate check.</returns>
        public static bool Most<T>(this IEnumerable<T> items, Func<T, bool> predicate, double threshold = 0.83)
        {
            int pass = 0;
            int fail = 0;

            foreach (var item in items)
            {
                if (predicate(item))
                    pass++;
                else
                    fail++;
            }

            if ((pass + fail) == 0)
                return false;

            return ((double)pass / (pass + fail) >= threshold);
        }
#nullable enable


        public static bool SequenceEquals<T, U>(this IEnumerable<T> list1, IEnumerable<T> list2, Func<T, U> selector, IComparer<U> comparer)
        {
            if (!list1.Any() && !list2.Any())
                return true;

            if (!list1.Any() || !list2.Any())
                return false;

            var e1 = list1.GetEnumerator();
            var e2 = list2.GetEnumerator();

            while (true)
            {
                bool move1 = e1.MoveNext();
                bool move2 = e2.MoveNext();

                if (move1 ^ move2)
                    return false;

                if (!move1)
                    break;

                if (!(comparer.Compare(selector(e1.Current), selector(e2.Current)) == 0))
                    return false;
            }

            return true;
        }

    }
}
