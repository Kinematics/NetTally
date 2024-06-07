using System.Text.RegularExpressions;

namespace NetTally.Utility.Filtering;
/// <summary>
/// Factory class to create a filter for testing post numbers.
/// </summary>
public static partial class PostNumberFilter
{
    private static readonly Regex postFilterRegex = PostFilterRegex();
    private static readonly char[] separator = [','];
    private static readonly Range FailRange = new(0, 0);

    [GeneratedRegex(@"^((?<range>(?<r1>\d+)\s*-\s*(?<r2>\d+))|(?<num>\d+))$",
        RegexOptions.ExplicitCapture, 50)]
    private static partial Regex PostFilterRegex();

    public static PostNumFilter AlwaysAllow =>
        AdaptingListFilter<Range, long>.AlwaysAllow;
    public static PostNumFilter AlwaysBlock =>
        AdaptingListFilter<Range, long>.AlwaysBlock;

    public static PostNumFilter Create(string value)
    {
        value = value.RemoveUnsafeCharacters().Trim();

        if (string.IsNullOrEmpty(value))
        {
            return AdaptingListFilter<Range, long>.AlwaysAllow;
        }

        bool invert = value[0] == '!';
        if (invert)
        {
            value = value[1..];

            if (string.IsNullOrWhiteSpace(value))
            {
                return AdaptingListFilter<Range, long>.AlwaysBlock;
            }
        }

        var ranges = value
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => postFilterRegex.Match(s))
            .Where(m => m.Success)
            .Select(ConvertToRange)
            .Where(r => r.Start.Value != FailRange.Start.Value);

        return invert
            ? AdaptingListFilter<Range, long>.Whitelist(ranges, IsValueInRange)
            : AdaptingListFilter<Range, long>.Blacklist(ranges, IsValueInRange);
    }

    private static Range ConvertToRange(Match m)
    {
        if (m.Groups["range"].Success)
        {
            if (int.TryParse(m.Groups["r1"].Value, out int startRange) &&
                int.TryParse(m.Groups["r2"].Value, out int endRange))
            {
                return new(startRange, endRange);
            }
        }
        else if (m.Groups["num"].Success)
        {
            if (int.TryParse(m.Groups["num"].Value, out int num))
            {
                return new(num, num);
            }
        }

        return FailRange;
    }

    private static bool IsValueInRange(Range range, long item)
    {
        int value = (int)item;

        return value >= range.Start.Value && value <= range.End.Value;
    }
}
