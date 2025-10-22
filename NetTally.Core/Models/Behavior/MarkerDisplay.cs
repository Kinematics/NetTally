using NetTally.Configure;
using NetTally.Models.Behavior;
using NetTally.Models.Mapping;
using NetTally.Models.Votes;

namespace NetTally.Models.Behavior;

public static class MarkerDisplay
{
    extension(Marker marker)
    {
        /// <summary>
        /// Get a string value to use for display purposes for a marker.
        /// </summary>
        /// <param name="marker">The marker to get a value for.</param>
        /// <returns>A string representation of the marker.</returns>
        public string Display => marker.Map(
            voteMarker => "X",
            rankMarker => $"#{rankMarker.Rank}",
            scoreMarker => $"{scoreMarker.Score}%",
            approvalMarker => approvalMarker.Approve ? "+" : "-",
            planMarker => Strings.PlanNameMarker,
            noMarker => "");

        /// <summary>
        /// Get a string representation of the marker, enclosed in brackets.
        /// </summary>
        /// <param name="marker">The marker to get a value for.</param>
        /// <returns>A string representation of the marker, enclosed in brackets.</returns>
        public string BracketedDisplay => marker.Map(
            voteMarker => "[X]",
            rankMarker => $"[#{rankMarker.Rank}]",
            scoreMarker => $"[{scoreMarker.Score}%]",
            approvalMarker => $"[{(approvalMarker.Approve ? "+" : "-")}]",
            planMarker => $"[{Strings.PlanNameMarker}]",
            noMarker => "[]");
    }
}
