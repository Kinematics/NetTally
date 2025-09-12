namespace NetTally.Tally.Vote.Component;

public abstract record Marker();

public sealed record VoteMarker() : Marker;

public sealed record ApprovalMarker(bool Approve) : Marker;

public sealed record ScoreMarker(int Score) : Marker;

public sealed record RankMarker(int Rank) : Marker;

public sealed record NoMarker() : Marker;

public sealed record PlanMarker() : Marker;

