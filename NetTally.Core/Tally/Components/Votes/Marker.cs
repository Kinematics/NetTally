using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NetTally.Enums;
using NetTally.Utility;

namespace NetTally.Tally.Components.Votes;

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

    [GeneratedRegex(@"^(?<marker>(?<vote>[xX✓✔✗✘Х☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$")]
    private static partial Regex MarkerRegex { get; }

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
        if (string.IsNullOrEmpty(value))
            return null;

        value = value.Trim();

        Match m = MarkerRegex.Match(value);

        if (m.Success)
        {
            // Can't have rank and score valid at the same time
            if (m.Groups["rank"].Success && m.Groups["score"].Success)
                return null;

            MarkerType markerType = true switch
            {
                _ when m.Groups["vote"].Success => MarkerType.Vote,
                _ when m.Groups["approval"].Success => MarkerType.Approval,
                _ when m.Groups["score"].Success => MarkerType.Score,
                _ when m.Groups["rank"].Success => MarkerType.Rank,
                _ => MarkerType.Rank
            };

            int markerValue = markerType switch
            {
                MarkerType.Vote => 100,
                MarkerType.Approval => value == "+" ? 80 : 20,
                MarkerType.Rank => Math.Clamp(
                    int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture),
                    1, 99),
                MarkerType.Score => Math.Clamp(
                    int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture),
                    0, 100),
                _ => 100
            };

            return new MarkerData(markerType, markerValue, value);
        }

        return null;
    }
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
        if (x.MarkerType == MarkerType.None || y.MarkerType == MarkerType.None) return 0;

        // MarkerType.Plan should be ignored.
        if (x.MarkerType == MarkerType.Plan || y.MarkerType == MarkerType.Plan) return 0;

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
}

/// <summary>
/// Extension class for <see cref="MarkerData"/> objects.
/// </summary>
public static class MarkerExtensions
{
    /// <summary>
    /// Determine if the marker is considered a positive result or not.
    /// </summary>
    /// <param name="marker">The <see cref="MarkerData"/> object to check.</param>
    /// <returns><c>True</c> if the marker is a positive result, <c>false</c> if it is a negative result,
    /// or <c>null</c> if it cannot be evaluated.</returns>
    public static bool? IsPositive(this MarkerData marker)
    {
        return marker.MarkerType switch
        {
            MarkerType.Rank => null,
            MarkerType.Vote => true,
            _ => marker.MarkerValue > 50
        };
    }
}
