using System.Collections.Immutable;

namespace NetTally.Models;

/// <summary>
/// A record of a set of vote lines with a particular <see cref="Origin"/>.
/// </summary>
/// <param name="Origin">Where the vote originated from.</param>
/// <param name="VoteLines">The lines contained in the vote.</param>
public record Vote(Origin Origin, ImmutableList<VoteLine> VoteLines);
