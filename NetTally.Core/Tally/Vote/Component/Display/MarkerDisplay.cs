using NetTally.Configure;

namespace NetTally.Tally.Vote.Component;
public static class MarkerDisplay
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
    }
}
