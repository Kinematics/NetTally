using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NetTally.Enums;
using NetTally.Utility;

namespace NetTally.Tally.Components.Votes;


public abstract record Marker();

public sealed record VoteMarker() : Marker;

public sealed record ApprovalMarker(bool Approve) : Marker;

public sealed record ScoreMarker(int Score) : Marker;

public sealed record RankMarker(int Rank) : Marker;

public sealed record NoMarker() : Marker;

public sealed record PlanMarker() : Marker;


/// <summary>
/// Extension class for <see cref="Marker"/> creation.
/// </summary>
public static partial class MarkerCreation
{
    extension(Marker)
    {
        public static Marker Empty => _empty;
        public static Marker PlanMarker => _planMarker;

        /// <summary>
        /// Create a new <see cref="Marker"/> based on the provided text.
        /// Invalid values will return a <see cref="NoMarker"/> type.
        /// </summary>
        /// <param name="markerText">The text that defines the <see cref="Marker"/>.</param>
        /// <returns>A new <see cref="Marker"/></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static Marker Create(string? markerText)
        {
            if (string.IsNullOrWhiteSpace(markerText))
                return Marker.Empty;

            markerText = markerText.Trim();

            Match m = MarkerRegex.Match(markerText);

            if (m.Success)
            {
                // Can't have rank and score valid at the same time
                if (m.Groups["rank"].Success && m.Groups["score"].Success)
                    return Marker.Empty;

                return true switch
                {
                    _ when m.Groups["vote"].Success => new VoteMarker(),
                    _ when m.Groups["approval"].Success => new ApprovalMarker(
                        m.Groups["approval"].Value == "+"),
                    _ when m.Groups["rank"].Success => new RankMarker(
                        Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 1, 99)),
                    _ when m.Groups["score"].Success => new ScoreMarker(
                        Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 0, 100)),
                    _ when m.Groups["value"].Success => new RankMarker(
                        Math.Clamp(int.Parse(m.Groups["value"].Value, System.Globalization.CultureInfo.CurrentCulture), 1, 99)),
                    _ => throw new InvalidOperationException($"Marker regex succeeded, but no valid regex type found. Text: {markerText}")
                };
            }

            return Marker.Empty;
        }
    }

    private static readonly Marker _empty = new NoMarker();
    private static readonly Marker _planMarker = new PlanMarker();

    [GeneratedRegex(@"^(?<marker>(?<vote>[xX✓✔✗✘Х☒☑])|(?<rank>#)?(?<value>[0-9]{1,3})(?<score>%)?|(?<approval>[-+]))$")]
    private static partial Regex MarkerRegex { get; }
}


/// <summary>
/// Extension class containing mapping function for subclasses of <see cref="Marker"/> objects.
/// </summary>
public static class MarkerMapping
{
    extension(Marker marker)
    {
        /// <summary>
        /// Map function that defines how to implement a function that can apply to different
        /// subclasses of <see cref="Marker"/>.
        /// </summary>
        /// <typeparam name="T">The function return type.</typeparam>
        /// <param name="origin">The <see cref="Origin"/> that this extension method applies to.</param>
        /// <param name="voteMap">What to do if the <see cref="Marker"/> is a <see cref="VoteMarker"/></param>
        /// <param name="rankMap">What to do if the <see cref="Marker"/> is a <see cref="RankMarker"/></param>
        /// <param name="scoreMap">What to do if the <see cref="Marker"/> is a <see cref="ScoreMarker"/></param>
        /// <param name="approvalMap">What to do if the <see cref="Marker"/> is a <see cref="ApprovalMarker"/></param>
        /// <param name="planMap">What to do if the <see cref="Marker"/> is a <see cref="PlanMarker"/></param>
        /// <param name="emptyMap">What to do if the <see cref="Marker"/> is a <see cref="NoMarker"/></param>
        /// <returns>The result of whichever function got applied.</returns>
        /// <exception cref="InvalidOperationException">Will trigger if another subclass is
        /// ever created, but this function hasn't been updated.</exception>
        public T Map<T>(
            Func<VoteMarker, T> voteMap,
            Func<RankMarker, T> rankMap,
            Func<ScoreMarker, T> scoreMap,
            Func<ApprovalMarker, T> approvalMap,
            Func<PlanMarker, T> planMap,
            Func<NoMarker, T> emptyMap)
        {
            return marker switch
            {
                VoteMarker voteMarker => voteMap(voteMarker),
                RankMarker rankMaker => rankMap(rankMaker),
                ScoreMarker scoreMarker => scoreMap(scoreMarker),
                ApprovalMarker approvalMarker => approvalMap(approvalMarker),
                PlanMarker planMarker => planMap(planMarker),
                NoMarker noMarker => emptyMap(noMarker),
                _ => throw new InvalidOperationException("Unknown Marker type.")
            };
        }
    }
}

/// <summary>
/// Extension class that provides extensions to <see cref="Marker"/> objects
/// based on the underlying type.
/// </summary>
public static partial class MarkerExtensions
{
    extension(Marker marker)
    {
        /// <summary>
        /// Get a string value to use for display purposes for a marker.
        /// </summary>
        /// <param name="marker">The marker to get a value for.</param>
        /// <returns>Returns a string value based on the marker's derived class and state.</returns>
        public string Display() => marker.Map(
            voteMarker => "X",
            rankMarker => $"#{rankMarker.Rank}",
            scoreMarker => $"{scoreMarker.Score}%",
            approvalMarker => approvalMarker.Approve ? "+" : "-",
            planMarker => Strings.PlanNameMarker,
            noMarker => "");

        /// <summary>
        /// Get a string value to use for display purposes for a marker.
        /// </summary>
        /// <param name="marker">The marker to get a value for.</param>
        /// <returns>Returns a string value based on the marker's derived class and state.</returns>
        public MarkerType Type => marker.Map(
            voteMarker => MarkerType.Vote,
            rankMarker => MarkerType.Rank,
            scoreMarker => MarkerType.Score,
            approvalMarker => MarkerType.Approval,
            planMarker => MarkerType.Plan,
            noMarker => MarkerType.None);

        /// <summary>
        /// Get a numeric value representing a marker.
        /// </summary>
        /// <param name="marker">The marker to get a value for.</param>
        /// <returns>Returns an integer value based on the marker's derived class and state.</returns>
        public int Value => marker.Map(
            voteMarker => 100,
            rankMarker => rankMarker.Rank,
            scoreMarker => scoreMarker.Score,
            approvalMarker => approvalMarker.Approve ? 80 : 20,
            planMarker => 0,
            noMarker => 0);

        /// <summary>
        /// Gets whether the marker's current state can be considered a 'positive' result.
        /// </summary>
        /// <param name="marker">The marker to examine.</param>
        /// <returns>Returns <c>true</c> if the marker is positive, <c>false</c> if
        /// it is not, or <c>null</c> if there is no meaningful way to answer.</returns>
        public bool? IsPositive => marker.Map(
            voteMarker => true,
            rankMarker => (bool?)null,
            scoreMarker => scoreMarker.Score > 50,
            approvalMarker => approvalMarker.Approve,
            planMarker => null,
            noMarker => null);
    }
}

/// <summary>
/// Comparer class for <see cref="Marker"/> objects.
/// </summary>
public class MarkersComparer : IEqualityComparer<Marker>, IComparer<Marker>
{
    public static MarkersComparer Instance { get; } = new();

    public int Compare(Marker? x, Marker? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        // MarkerType.None matches anything.
        if (x is NoMarker || y is NoMarker) return 0;

        // MarkerType.Plan should be ignored.
        if (x is PlanMarker || y is PlanMarker) return 0;

        if (x.Type == y.Type)
            return x.Value.CompareTo(y.Value);

        // Ranks should get sorted before other types.
        if (x is RankMarker) return -1;
        if (y is RankMarker) return 1;

        // Otherwise just compare the values.
        return x.Value.CompareTo(y.Value);
    }

    public bool Equals(Marker? x, Marker? y)
    {
        if (x is null || y is null) return false;
        if (ReferenceEquals(x, y)) return true;

        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] Marker obj)
    {
        return obj.Value.GetHashCode();
    }
}
