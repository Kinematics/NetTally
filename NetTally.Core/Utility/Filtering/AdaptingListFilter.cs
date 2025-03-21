namespace NetTally.Utility.Filtering;

/// <summary>
/// An item filter that determines whether an object is allowed by
/// checking against either a whitelist or a blacklist.
/// </summary>
/// <typeparam name="T">The type of items to be filtered.</typeparam>
public sealed class AdaptingListFilter<T, U> : IAdaptingFilter<T, U>
{
    private readonly FilterType filterType;
    private readonly HashSet<T> filterItems;
    private readonly AdaptPredicate<T, U> comparer;

    /// <summary>
    /// A private constructor for the list filter.
    /// The only way to get a new list filter is through one of the factory methods.
    /// </summary>
    /// <param name="listFilterType">The type of filter process to use.</param>
    /// <param name="items">The list of items that defines the filter.</param>
    /// <param name="comparer">An optional equality comparer for the items.</param>
    private AdaptingListFilter(FilterType listFilterType,
                         IEnumerable<T> items,
                         AdaptPredicate<T, U> comparer)
    {
        filterType = listFilterType;
        filterItems = items.ToHashSet();
        this.comparer = comparer;
    }

    #region Factories functions for list filters.
    public static IAdaptingFilter<T, U> Whitelist(IEnumerable<T> list, AdaptPredicate<T, U> comparer) =>
        new AdaptingListFilter<T, U>(FilterType.Allow, list, comparer);
    public static IAdaptingFilter<T, U> Blacklist(IEnumerable<T> list, AdaptPredicate<T, U> comparer) =>
        new AdaptingListFilter<T, U>(FilterType.Block, list, comparer);

    public static IAdaptingFilter<T, U> AlwaysAllow { get; } = AlwaysFilter.AllowAll<T, U>();
    public static IAdaptingFilter<T, U> AlwaysBlock { get; } = AlwaysFilter.BlockAll<T, U>();

    #endregion

    public bool Allows(T item) => filterType switch
    {
        FilterType.Allow => filterItems.Contains(item),
        FilterType.Block => !filterItems.Contains(item),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };

    public bool Allows(U item) => filterType switch
    {
        FilterType.Allow => filterItems.Any(f => comparer(f, item)),
        FilterType.Block => !filterItems.Any(f => comparer(f, item)),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };

    public bool Blocks(T item) => filterType switch
    {
        FilterType.Allow => !filterItems.Contains(item),
        FilterType.Block => filterItems.Contains(item),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };

    public bool Blocks(U item) => filterType switch
    {
        FilterType.Allow => !filterItems.Any(f => comparer(f, item)),
        FilterType.Block => filterItems.Any(f => comparer(f, item)),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };
}
