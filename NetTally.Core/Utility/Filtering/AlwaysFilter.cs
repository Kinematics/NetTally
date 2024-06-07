namespace NetTally.Utility.Filtering;
/// <summary>
/// A filter that always allows or always blocks all items.
/// </summary>
/// <typeparam name="T">The type of items to be filtered.</typeparam>
public sealed class AlwaysFilter<T> : IItemFilter<T>
{
    private readonly FilterType _filterType;

    private AlwaysFilter(FilterType filterType)
    {
        _filterType = filterType;
    }

    public static IItemFilter<T> Allow() => new AlwaysFilter<T>(FilterType.Allow);
    public static IItemFilter<T> Block() => new AlwaysFilter<T>(FilterType.Block);

    public bool Allows(T item) => _filterType switch
    {
        FilterType.Allow => true,
        _ => false
    };

    public bool Blocks(T item) => _filterType switch
    {
        FilterType.Block => true,
        _ => false
    };
}
