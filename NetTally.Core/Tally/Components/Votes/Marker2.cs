using System.Text.RegularExpressions;

namespace NetTally.Tally.Components.Votes;


public abstract record MarkerBase();

public record VoteMarker() : MarkerBase
{
    public override string ToString() => "X";
}

public record ApprovalMarker(bool Approve) : MarkerBase
{
    public override string ToString() => Approve ? "+" : "-";
}

public record ScoreMarker(int Score) : MarkerBase
{
    public override string ToString() => $"{Score}%";
}

public record RankMarker(int Rank) : MarkerBase
{
    public override string ToString() => $"#{Rank}";
}

public record NoMarker() : MarkerBase
{
    public override string ToString() => "";
}


/// <summary>
/// Class for creating <see cref="MarkerBase"/> objects.
/// </summary>
public static partial class Markers
{
    public static MarkerBase Empty { get; } = new NoMarker();

    [GeneratedRegex(@"^(?<marker>(?<vote>[xX✓✔✗✘Х☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$")]
    private static partial Regex MarkerRegex { get; }

    public static MarkerBase? Create(string? markerText)
    {
        if (string.IsNullOrWhiteSpace(markerText))
            return Empty;

        markerText = markerText.Trim();

        Match m = MarkerRegex.Match(markerText);

        if (m.Success)
        {
            // Can't have rank and score valid at the same time
            if (m.Groups["rank"].Success && m.Groups["score"].Success)
                return null;

            return true switch
            {
                _ when m.Groups["vote"].Success => new VoteMarker(),
                _ when m.Groups["approval"].Success => new ApprovalMarker(
                    m.Groups["approval"].Value == "+"),
                _ when m.Groups["score"].Success => new ScoreMarker(
                    Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 0, 100)),
                _ when m.Groups["rank"].Success => new RankMarker(
                    Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 1, 99)),
                _ => new VoteMarker()
            };

        }

        return null;
    }
}

/// <summary>
/// Class containing mapping function for subclasses of <see cref="MarkerBase"/> objects.
/// </summary>
public static class MarkerMapping
{
    /// <summary>
    /// Map function that defines how to implement a function that can apply to different
    /// subclasses of <see cref="MarkerBase"/>.
    /// </summary>
    /// <typeparam name="T">The function return type.</typeparam>
    /// <param name="origin">The <see cref="Origin"/> that this extension method applies to.</param>
    /// <param name="voteMap">What to do if the <see cref="MarkerBase"/> is a <see cref="VoteMarker"/></param>
    /// <param name="rankMap">What to do if the <see cref="MarkerBase"/> is a <see cref="RankMarker"/></param>
    /// <param name="scoreMap">What to do if the <see cref="MarkerBase"/> is a <see cref="ScoreMarker"/></param>
    /// <param name="approvalMap">What to do if the <see cref="MarkerBase"/> is a <see cref="ApprovalMarker"/></param>
    /// <returns>The result of whichever function got applied.</returns>
    /// <exception cref="InvalidOperationException">Will trigger if another subclass is
    /// ever created, but this function hasn't been updated.</exception>
    public static T Map<T>(this MarkerBase marker,
        Func<VoteMarker, T> voteMap,
        Func<RankMarker, T> rankMap,
        Func<ScoreMarker, T> scoreMap,
        Func<ApprovalMarker, T> approvalMap) =>
        marker switch
        {
            VoteMarker voteMarker => voteMap(voteMarker),
            RankMarker rankMaker => rankMap(rankMaker),
            ScoreMarker scoreMarker => scoreMap(scoreMarker),
            ApprovalMarker approvalMarker => approvalMap(approvalMarker),
            _ => throw new InvalidOperationException("Unknown Marker type.")
        };
}

public static partial class MarkerExtensions
{
    /// <summary>
    /// Get a numeric value representing a marker.
    /// </summary>
    /// <param name="marker">The marker to get a value for.</param>
    /// <returns>Returns an integer value based on the marker's derived class and state.</returns>
    public static int GetValue(this MarkerBase marker) => marker.Map(
        voteMarker => 100,
        rankMarker => rankMarker.Rank,
        scoreMarker => scoreMarker.Score,
        approvalMarker => approvalMarker.Approve ? 80 : 20);

    /// <summary>
    /// Gets whether the marker's current state can be considered a 'positive' result.
    /// </summary>
    /// <param name="marker">The marker to examine.</param>
    /// <returns>Returns <c>true</c> if the marker is positive, <c>false</c> if
    /// it is not, or <c>null</c> if there is no meaningful way to answer.</returns>
    public static bool? IsPositive(this MarkerBase marker) => marker.Map(
        voteMarker => true,
        rankMarker => (bool?)null,
        scoreMarker => scoreMarker.Score > 50,
        approvalMarker => approvalMarker.Approve);
}
