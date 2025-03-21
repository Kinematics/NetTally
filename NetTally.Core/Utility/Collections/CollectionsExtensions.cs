using System.Collections.ObjectModel;

namespace NetTally.Utility.Collections;

/// <summary>
/// Extension methods for various collections.
/// </summary>
public static class CollectionsExtensions
{
    /// <summary>
    /// Swap two values in a list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list">The list to swap data in.</param>
    /// <param name="firstIndex">The first index value being swapped.</param>
    /// <param name="secondIndex">The second index value being swapped.</param>
    /// <returns><c>True</c> if the items were swapped, or <c>false</c> if they were not.</returns>
    public static bool Swap<T>(this IList<T> list, int firstIndex, int secondIndex)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (firstIndex == secondIndex)
            return false;

        if (firstIndex < 0 || firstIndex >= list.Count || secondIndex < 0 || secondIndex >= list.Count)
            return false;

        (list[secondIndex], list[firstIndex]) = (list[firstIndex], list[secondIndex]);

        return true;
    }

    /// <summary>
    /// Does an in-place sort of the specified collection.
    /// </summary>
    /// <typeparam name="T">The type of object held in the collection.</typeparam>
    /// <param name="collection">The collection to be sorted.</param>
    public static void Sort<T>(this ObservableCollection<T> collection,
        bool descending = false) where T : IComparable
    {
        var sorted = descending ? [.. collection.OrderDescending()] : collection.Order().ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            collection[i] = sorted[i];
        }
    }
}
