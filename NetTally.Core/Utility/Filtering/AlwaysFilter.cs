namespace NetTally.Utility.Filtering;
/// <summary>
/// Get a filter that always allows or always blocks all items.
/// </summary>
public class AlwaysFilter
{
    public static IItemFilter<T> AllowAll<T>() => new AlwaysFilterInternal<T>(FilterType.Allow);
    public static IItemFilter<T> BlockAll<T>() => new AlwaysFilterInternal<T>(FilterType.Block);

    private sealed class AlwaysFilterInternal<T>(FilterType filterType) : IItemFilter<T>
    {
        public bool Allows(T item) => filterType switch
        {
            FilterType.Allow => true,
            _ => false
        };

        public bool Blocks(T item) => filterType switch
        {
            FilterType.Block => true,
            _ => false
        };
    }
}