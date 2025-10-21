using NetTally.Enums;
using NetTally.Models.Votes;

namespace NetTally.Models.Utility;

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
