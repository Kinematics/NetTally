using System.Text.RegularExpressions;

namespace NetTally.Utility.Filtering;

/// <summary>
/// An item filter that determines whether an object is allowed by
/// running a regex test against a string extraction of the object.
/// </summary>
public sealed class RegexFilter : IItemFilter<string>
{
    private readonly FilterType filterType;
    private readonly List<RegexPattern> patterns;

    private RegexFilter(FilterType filterType, IEnumerable<RegexPattern> patterns)
    {
        this.patterns = patterns.ToList();
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
            .Select(r => RegexPattern.Create(r));

        return new RegexFilter(FilterType.Allow, patterns);
    }
    public static IItemFilter<string> Block(params Regex[] regexes)
    {
        var patterns = regexes
            .Select(r => RegexPattern.Create(r));

        return new RegexFilter(FilterType.Block, patterns);
    }

    public static IItemFilter<string> AlwaysAllow { get; } = AlwaysFilter.AllowAll<string>();
    public static IItemFilter<string> AlwaysBlock { get; } = AlwaysFilter.BlockAll<string>();

    #endregion

    public bool Allows(string item) => filterType switch
    {
        FilterType.Allow => patterns.Any(a => a.IsMatch(item)),
        FilterType.Block => !patterns.Any(a => a.IsMatch(item)),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };

    public bool Blocks(string item) => filterType switch
    {
        FilterType.Allow => !patterns.Any(a => a.IsMatch(item)),
        FilterType.Block => patterns.Any(a => a.IsMatch(item)),
        _ => throw new InvalidOperationException($"Unknown filter type: {filterType}")
    };

    public static IItemFilter<string> DefaultThreadmarkFilter { get; } =
        Block(RegexPattern.Create(Strings.OmakeFilter));
}
