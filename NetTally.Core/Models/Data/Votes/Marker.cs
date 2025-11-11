namespace NetTally.Models;

/// <summary>
/// A marker on a vote line that's used to indicate how a person voted.
/// </summary>
public abstract record Marker();

/// <summary>
/// A standard vote marker, to indicate a person is voting for this option.
/// </summary>
public sealed record VoteMarker() : Marker;

/// <summary>
/// A vote marker indicating either approval or disapproval for the vote option.
/// </summary>
/// <param name="Approve">Whether the voter approves of the vote line or doesn't.</param>
public sealed record ApprovalMarker(bool Approve) : Marker;

/// <summary>
/// A vote marker indicating the score a voter gave to the vote option.
/// </summary>
/// <param name="Score">A score between 0 (worst) and 100 (best), indicating percent approval.</param>
public sealed record ScoreMarker(int Score) : Marker;

/// <summary>
/// A vote marker indicating the rank a voter gave to this vote option.
/// </summary>
/// <param name="Rank">A rank between 1 (best) and 99 (worst) for the voter's preference for this option.</param>
public sealed record RankMarker(int Rank) : Marker;

/// <summary>
/// A type representing the lack of a vote marker.
/// </summary>
public sealed record NoMarker() : Marker;

/// <summary>
/// A type indicating that this vote line is part of a defined plan, rather than a user vote.
/// </summary>
public sealed record PlanMarker() : Marker;
