namespace NetTally.Tally.Vote.Component;

public abstract record Marker();

public sealed record VoteMarker() : Marker;

public sealed record ApprovalMarker(bool Approve) : Marker;

public sealed record ScoreMarker(int Score) : Marker;

public sealed record RankMarker(int Rank) : Marker;

public sealed record NoMarker() : Marker;

public sealed record PlanMarker() : Marker;

public static class MarkerPredefined
{
    extension(Marker)
    {
        public static Marker Empty => noMarker;
        public static Marker PlanMarker => planMarker;
    }

    private static readonly Marker noMarker = new NoMarker();
    private static readonly Marker planMarker = new PlanMarker();
}
