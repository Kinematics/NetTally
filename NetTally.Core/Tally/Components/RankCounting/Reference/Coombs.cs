using NetTally.Tally.Components.Votes;
using NetTally.Tally.Vote.Components;

namespace NetTally.Tally.Components.RankCounting.Reference;

/// <summary>
/// Implement ranking votes using the Coombs method.
/// This is an instant runoff that removes the most disliked
/// option each round, instead of the least liked.
/// </summary>
public class Coombs : InstantRunoffBase
{
    /// <summary>
    /// Gets the least preferred choice.
    /// Using Coombs Method, this is the option that has the greatest
    /// number of bottom-ranked votes.
    /// </summary>
    /// <param name="localRankings">The vote rankings.</param>
    /// <returns>Returns the vote string for the least preferred vote.</returns>
    protected override VoteBlockType GetLeastPreferredChoice(VotesByVoterF voterPreferences)
    {
        return voterPreferences.GroupBy(v => v.Value.Last()) // lowest rankings
            .MaxBy(r => r.Count())    // least preferred
            ?.Key ?? VoteBlock.Empty; // the vote block found, or Empty if none
    }
}
