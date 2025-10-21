using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class MarkerDefaults
{
    extension(Marker)
    {
        public static Marker None => noMarker;
        public static Marker PlanMarker => planMarker;
    }

    private static readonly Marker noMarker = new NoMarker();
    private static readonly Marker planMarker = new PlanMarker();
}
