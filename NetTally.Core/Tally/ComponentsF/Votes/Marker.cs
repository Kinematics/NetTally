using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NetTally.Enums;
using NetTally.Utility;

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
    #region Public predefined markers
    /// <summary>
    /// An empty <see cref="MarkerData"/> object.
    /// </summary>
    public static MarkerData Empty { get; } = new MarkerData(MarkerType.None, 0, "");
    /// <summary>
    /// A basic <see cref="MarkerData"/> object for a plan.
    /// </summary>
    public static MarkerData PlanMarker { get; } = new MarkerData(MarkerType.Plan, 0, Strings.PlanNameMarker);
    /// <summary>
    /// A basic <see cref="MarkerData"/> object for a vote.
    /// </summary>
    public static MarkerData VoteMarker { get; } = new MarkerData(MarkerType.Vote, 0, Strings.VoteMarker);
    /// <summary>
    /// A basic <see cref="MarkerData"/> object for an approval vote.
    /// </summary>
    public static MarkerData ApprovalMarker { get; } = new MarkerData(MarkerType.Approval, 0, Strings.ApprovalMarker);
    /// <summary>
    /// A basic <see cref="MarkerData"/> object for a score vote.
    /// </summary>
    public static MarkerData ScoreMarker { get; } = new MarkerData(MarkerType.Score, 0, Strings.ScoreMarker);
    /// <summary>
    /// A basic <see cref="MarkerData"/> object for a rank vote.
    /// </summary>
    public static MarkerData RankMarker { get; } = new MarkerData(MarkerType.Rank, 0, Strings.RankMarker);
    #endregion Public predefined markers

    #region Regexes
    static readonly Regex markerRegex = MarkerRegex();

    [GeneratedRegex(@"^(?<marker>(?<vote>[xX✓✔✗✘Х☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$")]
    private static partial Regex MarkerRegex();
    #endregion Regexes

    #region Marker creation
    /// <summary>
    /// Create a marker using the provided numeric value.
    /// </summary>
    /// <param name="value">The value for the marker to display.</param>
    /// <returns>A new <see cref="MarkerData"/> object for the provided value.</returns>
    public static MarkerData? Create(int value)
    {
        return new MarkerData(MarkerType.None, value, value.ToString());
    }

    /// <summary>
    /// Create a marker using the provided string value.
    /// </summary>
    /// <param name="value">The value for the marker to display.</param>
    /// <returns>A new <see cref="MarkerData"/> object for the provided value.</returns>
    public static MarkerData? Create(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            value = value.Trim();

            Match m = markerRegex.Match(value);

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
                    markerValue = value == "+" ? 80 : 20;
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

                return new MarkerData(markerType, markerValue, value);
            }
        }

        return null;
    }
    #endregion Marker creation
}

/// <summary>
/// Comparer class for <see cref="MarkerData"/> objects.
/// </summary>
public class MarkerComparer : IEqualityComparer<MarkerData>, IComparer<MarkerData>
{
    public static MarkerComparer Instance { get; } = new();

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
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] MarkerData obj)
    {
        return obj.MarkerValue.GetHashCode();
    }

    public static bool? IsPositive(MarkerData marker)
    {
        return marker.MarkerType switch
        {
            MarkerType.Rank => null,
            MarkerType.Vote => true,
            _ => marker.MarkerValue > 50
        };
    }
}
