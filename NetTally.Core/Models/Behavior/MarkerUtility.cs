using NetTally.Enums;
using NetTally.Models.Mapping;
using NetTally.Models.Votes;

namespace NetTally.Models.Behavior;

/// <summary>
/// Extension class that provides extensions to <see cref="Marker"/> objects
/// based on the underlying type.
/// </summary>
public static partial class MarkerUtility
{
    extension(Marker marker)
    {
        /// <summary>
        /// Get the enum <see cref="MarkerType"/> that corresponds to the <see cref="Marker"/> object.
        /// </summary>
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
        public int Value => marker.Map(
            voteMarker => 100,
            rankMarker => rankMarker.Rank,
            scoreMarker => scoreMarker.Score,
            approvalMarker => approvalMarker.Approve ? 80 : 20,
            planMarker => 0,
            noMarker => 0);

        /// <summary>
        /// Gets whether the marker's current state can be considered a 'positive' result.
        /// Returns <c>null</c> if there is no meaningful way to answer.
        /// </summary>
        public bool? IsPositive => marker.Map<bool?>(
            voteMarker => true,
            rankMarker => null,
            scoreMarker => scoreMarker.Score > 50,
            approvalMarker => approvalMarker.Approve,
            planMarker => null,
            noMarker => null);
    }
}
