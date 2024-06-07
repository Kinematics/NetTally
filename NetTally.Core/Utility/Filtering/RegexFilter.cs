using System.Text.RegularExpressions;

namespace NetTally.Utility.Filtering;

/// <summary>
/// An item filter that determines whether an object is allowed by
/// running a regex test against a string extraction of the object.
/// </summary>
public sealed class RegexFilter : IItemFilter<string>
{
    private readonly FilterType filterType;
    private readonly List<RegexPattern> patterns = [];

    /// <summary>
    /// Construct a new regex filter using the provided regex patterns.
    /// </summary>
    /// <param name="patterns"></param>
    private RegexFilter(FilterType filterType, IEnumerable<RegexPattern> patterns)
    {
        this.patterns.AddRange(patterns);
        this.filterType = filterType;
    }

    #region Factories used to construct varying types of filters.
    public static IItemFilter<string> Allow(params RegexPattern[] patterns)
    {
        return new RegexFilter(FilterType.Allow, patterns);
    }

    public static IItemFilter<string> Block(params RegexPattern[] patterns)
    {
        return new RegexFilter(FilterType.Block, patterns);
    }

    public static IItemFilter<string> Allow(params Regex[] regexes)
    {
        var patterns = regexes
            .Select(r => new RegexPattern(r));

        return new RegexFilter(FilterType.Allow, patterns);
    }
    public static IItemFilter<string> Block(params Regex[] regexes)
    {
        var patterns = regexes
            .Select(r => new RegexPattern(r));

        return new RegexFilter(FilterType.Block, patterns);
    }
    #endregion


    /// <summary>
    /// Determines whether the filter allows the item provided to pass through the filter.
    /// </summary>
    /// <param name="item">The item to be checked.</param>
    /// <returns>True if the filter allows the item, or false if not.</returns>
    public bool Allows(string item) => filterType switch
    {
        FilterType.Allow => patterns.Any(a => a.IsMatch(item)),
        FilterType.Block => !patterns.Any(a => a.IsMatch(item)),
        _ => throw new InvalidOperationException($"Invalid filter type: {filterType}")
    };

    public bool Blocks(string item) => filterType switch
    {
        FilterType.Allow => !patterns.Any(a => a.IsMatch(item)),
        FilterType.Block => patterns.Any(a => a.IsMatch(item)),
        _ => throw new InvalidOperationException($"Invalid filter type: {filterType}")
    };
}
