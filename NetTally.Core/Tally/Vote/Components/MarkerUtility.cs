using NetTally.Enums;
using NetTally.Utility;

namespace NetTally.Tally.Vote.Components;

/// <summary>
/// Extension class that provides extensions to <see cref="Marker"/> objects
/// based on the underlying type.
/// </summary>
public static partial class MarkerUtility
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
