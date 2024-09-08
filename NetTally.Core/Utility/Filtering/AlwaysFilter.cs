namespace NetTally.Utility.Filtering;
/// <summary>
/// Get a filter that always allows or always blocks all items.
/// </summary>
public class AlwaysFilter
{
    public static IItemFilter<T> AllowAll<T>() => new AlwaysFilterInternal<T>(FilterType.Allow);
    public static IItemFilter<T> BlockAll<T>() => new AlwaysFilterInternal<T>(FilterType.Block);

    public static IAdaptingFilter<T, U> AllowAll<T, U>() =>
        new AlwaysFilterAdaptInternal<T, U>(FilterType.Allow);
    public static IAdaptingFilter<T, U> BlockAll<T, U>() =>
        new AlwaysFilterAdaptInternal<T, U>(FilterType.Block);

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

    private sealed class AlwaysFilterAdaptInternal<T, U>(FilterType filterType) : IAdaptingFilter<T, U>
    {
        public bool Allows(U item) => filterType switch
        {
            FilterType.Allow => true,
            _ => false
        };

        public bool Blocks(U item) => filterType switch
        {
            FilterType.Block => true,
            _ => false
        };
    }
}