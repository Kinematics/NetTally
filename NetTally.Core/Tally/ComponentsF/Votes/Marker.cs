using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NetTally.Enums;

namespace NetTally.Tally.ComponentsF.Votes;
/// <summary>
/// Data type to store marker information.
/// </summary>
/// <param name="MarkerType">The type of marker.</param>
/// <param name="MarkerValue">The numeric value of the marker.</param>
/// <param name="MarkerSymbol">The marker text.</param>
public record MarkerData(MarkerType MarkerType, int MarkerValue, string MarkerSymbol);

/// <summary>
/// Static class to handle creation of <see cref="MarkerData"/> objects.
/// </summary>
public static partial class Marker
{
    public static MarkerData Empty { get; } = new MarkerData(MarkerType.None, 0, "");

    static readonly Regex markerRegex = MarkerRegex();

    [GeneratedRegex(@"^(?<marker>(?<vote>[xX✓✔✗✘Х☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$")]
    private static partial Regex MarkerRegex();

    public static MarkerData? Create(string marker)
    {
        if (!string.IsNullOrWhiteSpace(marker))
        {
            marker = marker.Trim();

            Match m = markerRegex.Match(marker);

            if (m.Success)
            {
                MarkerType markerType;
                int markerValue = 0;

                if (m.Groups["vote"].Success)
                {
                    markerType = MarkerType.Vote;
                }
                else if (m.Groups["rank"].Success &&
                         m.Groups["score"].Success)
                {
                    // Can't have #19%
                    return null;
                }
                else if (m.Groups["rank"].Success)
                {
                    markerType = MarkerType.Rank;
                }
                else if (m.Groups["score"].Success)
                {
                    markerType = MarkerType.Score;
                }
                else if (m.Groups["value"].Success)
                {
                    // Default type if we have a value, but no # or % was used.
                    markerType = MarkerType.Rank;
                }
                else if (m.Groups["approval"].Success)
                {
                    markerType = MarkerType.Approval;
                }
                else
                {
                    // Shouldn't be possible to get here, but if we do, it's invalid.
                    return null;
                }

                if (markerType == MarkerType.Vote)
                {
                    markerValue = 100;
                }
                else if (markerType == MarkerType.Approval)
                {
                    markerValue = marker == "+" ? 80 : 20;
                }
                else if (m.Groups["value"].Success)
                {
                    markerValue = int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture);

                    if (markerType == MarkerType.Rank)
                    {
                        if (markerValue < 1)
                            markerValue = 1;
                        if (markerValue > 99)
                            markerValue = 99;
                    }
                    else if (markerType == MarkerType.Score)
                    {
                        if (markerValue < 0)
                            markerValue = 0;
                        if (markerValue > 100)
                            markerValue = 100;
                    }
                }

                return new MarkerData(markerType, markerValue, marker);
            }
        }

        return null;
    }
}

/// <summary>
/// Comparer class for <see cref="MarkerData"/> objects.
/// </summary>
public class MarkerComparer : IEqualityComparer<MarkerData>, IComparer<MarkerData>
{
    static readonly MarkerComparer markerComparer = new();
    public static bool AreEqual(MarkerData? a, MarkerData? b) => markerComparer.Equals(a, b);
    public static int CompareWith(MarkerData? a, MarkerData? b) => markerComparer.Compare(a, b);

    public int Compare(MarkerData? x, MarkerData? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // MarkerType.None matches anything.
        if (x.MarkerType == MarkerType.None ||  y.MarkerType == MarkerType.None) return 0;

        // MarkerType.Plan should be ignored.
        if (x.MarkerType == MarkerType.Plan ||  y.MarkerType == MarkerType.Plan) return 0;

        if (x.MarkerType == y.MarkerType) 
            return x.MarkerValue.CompareTo(y.MarkerValue);

        if (x.MarkerType == MarkerType.Rank) return -1;
        if (y.MarkerType == MarkerType.Rank) return 1;

        return x.MarkerValue.CompareTo(y.MarkerValue);
    }

    public bool Equals(MarkerData? x, MarkerData? y)
    {
        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] MarkerData obj)
    {
        return obj.MarkerValue.GetHashCode();
    }
}
