namespace NetTally.Utility.Filtering;

/// <summary>
/// An item filter that determines whether an object is allowed by
/// evaluating a predicate that analyzes the object.
/// </summary>
/// <typeparam name="T">The type of items to be filtered.</typeparam>
public sealed class PredicateFilter<T> : IItemFilter<T>
{
    private readonly FilterType filterType;
    private readonly Func<T, bool> predicate;

    private PredicateFilter(FilterType filterType, Func<T, bool> predicate)
    {
        this.predicate = predicate;
        this.filterType = filterType;
    }

    #region Factories used to construct varying types of filters.
    public static IItemFilter<T> Allow(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new PredicateFilter<T>(FilterType.Allow, predicate);
    }

    public static IItemFilter<T> Block(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new PredicateFilter<T>(FilterType.Block, predicate);
    }
    #endregion

    /// <summary>
    /// Determines whether the filter allows the item provided to pass through the filter.
    /// </summary>
    /// <param name="item">The item to be checked.</param>
    /// <returns>True if the filter allows the item, or false if not.</returns>
    public bool Allows(T item) => filterType switch
    {
        FilterType.Allow => predicate(item),
        FilterType.Block => !predicate(item),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };

    /// <summary>
    /// Determines whether the filter blocks the item provided.
    /// </summary>
    /// <param name="item">The item to be checked.</param>
    /// <returns>True if the filter blocks the item, or false if not.</returns>
    public bool Blocks(T item) => filterType switch
    {
        FilterType.Allow => !predicate(item),
        FilterType.Block => predicate(item),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };
}
