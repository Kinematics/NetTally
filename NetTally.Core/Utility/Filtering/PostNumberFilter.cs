using System.Text.RegularExpressions;

namespace NetTally.Utility.Filtering;
public static partial class PostNumberFilter
{
    private static readonly Regex postFilterRegex = PostFilterRegex();

    [GeneratedRegex(@"(?<range>(?<r1>\d+)\s*-\s*(?<r2>\d+))|(?<num>\d+)",
        RegexOptions.None, 50)]
    private static partial Regex PostFilterRegex();

    public static IAdaptingFilter<Range, long> Create(string value)
    {
        value = value.RemoveUnsafeCharacters().Trim();

        if (string.IsNullOrEmpty(value))
        {
            return AdaptingListFilter<Range, long>.AlwaysAllow;
        }

        bool invert = value[0] == '!';
        if (invert)
        {
            value = value[1..].TrimStart();

            if (string.IsNullOrEmpty(value))
            {
                return AdaptingListFilter<Range, long>.AlwaysBlock;
            }
        }

        List<Range> ranges = [];

        MatchCollection ms = postFilterRegex.Matches(value);

        foreach (Match mm in ms)
        {
            if (mm.Groups["range"].Success)
            {
                if (int.TryParse(mm.Groups["r1"].Value, out int startRange) &&
                    int.TryParse(mm.Groups["r2"].Value, out int endRange))
                {
                    Range range = new(startRange, endRange);
                    ranges.Add(range);
                }
            }
            else if (mm.Groups["num"].Success)
            {
                if (int.TryParse(mm.Groups["num"].Value, out int num))
                {
                    Range range = new(num, num);
                    ranges.Add(range);
                }
            }
        }

        return invert
            ? AdaptingListFilter<Range, long>.Whitelist(ranges, IsValueInRange)
            : AdaptingListFilter<Range, long>.Blacklist(ranges, IsValueInRange);
    }

    private static bool IsValueInRange(Range range, long item)
    {
        int value = (int)item;

        return value >= range.Start.Value && value <= range.End.Value;
    }
}
