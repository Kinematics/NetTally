using System.Text.RegularExpressions;
using NetTally.Configure;

namespace NetTally.Utility.Filtering;

/// <summary>
/// An item filter that determines whether an object is allowed by
/// running a regex test against a string extraction of the object.
/// </summary>
public sealed class RegexFilter : TextFilter
{
    private readonly FilterType filterType;
    private readonly List<RegexPattern> patterns;

    private RegexFilter(FilterType filterType, IEnumerable<RegexPattern> patterns)
    {
        this.patterns = patterns.ToList();
        this.filterType = filterType;
    }

    #region Factories used to construct varying types of filters.
    public static TextFilter Allow(params RegexPattern[] patterns)
    {
        return new RegexFilter(FilterType.Allow, patterns);
    }

    public static TextFilter Block(params RegexPattern[] patterns)
    {
        return new RegexFilter(FilterType.Block, patterns);
    }

    public static TextFilter Allow(params Regex[] regexes)
    {
        var patterns = regexes
            .Select(r => RegexPattern.Create(r));

        return new RegexFilter(FilterType.Allow, patterns);
    }
    public static TextFilter Block(params Regex[] regexes)
    {
        var patterns = regexes
            .Select(r => RegexPattern.Create(r));

        return new RegexFilter(FilterType.Block, patterns);
    }

    public static TextFilter Allow(params string[] patterns)
    {
        var p = patterns
            .Select(r => RegexPattern.Create(r));

        return new RegexFilter(FilterType.Allow, p);
    }
    public static TextFilter Block(params string[] patterns)
    {
        var p = patterns
            .Select(r => RegexPattern.Create(r));

        return new RegexFilter(FilterType.Block, p);
    }

    public static TextFilter AlwaysAllow { get; } = AlwaysFilter.AllowAll<string>();
    public static TextFilter AlwaysBlock { get; } = AlwaysFilter.BlockAll<string>();

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

    public static TextFilter DefaultThreadmarkFilter { get; } =
        Block(RegexPattern.Create(Strings.OmakeFilter));
}
