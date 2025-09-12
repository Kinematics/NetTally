namespace NetTally.Tally.Vote.Component;

public abstract record Marker();

public sealed record VoteMarker() : Marker;

public sealed record ApprovalMarker(bool Approve) : Marker;

public sealed record ScoreMarker(int Score) : Marker;

public sealed record RankMarker(int Rank) : Marker;

public sealed record NoMarker() : Marker;

public sealed record PlanMarker() : Marker;

//public static partial class MarkerPredefined
//{
//    extension(Marker)
//    {
//        public static Marker Empty => _empty;
//        public static Marker PlanMarker => _planMarker;
//    }

//    private static readonly Marker _empty = new NoMarker();
//    private static readonly Marker _planMarker = new PlanMarker();
//}
