using System.Collections.ObjectModel;
using System.Linq;

namespace NetTally.Utility.Filtering;

/// <summary>
/// An item filter that determines whether an object is allowed by
/// checking against either a whitelist or a blacklist.
/// </summary>
/// <typeparam name="T">The type of items to be filtered.</typeparam>
public sealed class ListFilter<T> : IItemFilter<T>
{
    private readonly FilterType filterType;
    private readonly HashSet<T> filterItems;
    private readonly IEqualityComparer<T> comparer;

    /// <summary>
    /// A private constructor for the list filter.
    /// The only way to get a new list filter is through one of the factory methods.
    /// </summary>
    /// <param name="listFilterType">The type of filter process to use.</param>
    /// <param name="items">The list of items that defines the filter.</param>
    /// <param name="comparer">An optional equality comparer for the items.</param>
    private ListFilter(FilterType listFilterType,
                         IEnumerable<T> items,
                         IEqualityComparer<T>? comparer = null)
    {
        filterType = listFilterType;
        filterItems = items.ToHashSet();
        this.comparer = comparer ?? EqualityComparer<T>.Default;
    }

    #region Factories functions for list filters.
    public static IItemFilter<T> Whitelist(IEnumerable<T> list, IEqualityComparer<T>? comparer = null) =>
        new ListFilter<T>(FilterType.Allow, list, comparer);
    public static IItemFilter<T> Blacklist(IEnumerable<T> list, IEqualityComparer<T>? comparer = null) =>
        new ListFilter<T>(FilterType.Block, list, comparer);

    public static IItemFilter<T> AlwaysAllow { get; } = AlwaysFilter.AllowAll<T>();
    public static IItemFilter<T> AlwaysBlock { get; } = AlwaysFilter.BlockAll<T>();

    #endregion

    /// <summary>
    /// Determines whether the filter allows the item provided to pass through the filter.
    /// </summary>
    /// <param name="item">The item to be checked.</param>
    /// <returns>True if the filter allows the item, or false if not.</returns>
    public bool Allows(T item) => filterType switch
    {
        FilterType.Allow => filterItems.Any(f => comparer.Equals(f, item)),
        FilterType.Block => !filterItems.Any(f => comparer.Equals(f, item)),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };

    /// <summary>
    /// Determines whether the filter blocks the item provided.
    /// </summary>
    /// <param name="item">The item to be checked.</param>
    /// <returns>True if the filter blocks the item, or false if not.</returns>
    public bool Blocks(T item) => filterType switch
    {
        FilterType.Allow => !filterItems.Any(f => comparer.Equals(f, item)),
        FilterType.Block => filterItems.Any(f => comparer.Equals(f, item)),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };
}
