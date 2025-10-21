using NetTally.Models.Defaults;
using NetTally.Models.Votes;

namespace NetTally.Tally.RankCounting.Reference;

/// <summary>
/// Implement ranking votes using the standard instant runoff method.
/// Each round, the least liked of the top-ranked choices is removed.
/// </summary>
public class InstantRunoff : InstantRunoffBase
{
    /// <summary>
    /// Gets the least preferred choice.
    /// In the standard Instant Runoff, this is the vote with the fewest
    /// number of top-ranked votes.
    /// </summary>
    /// <param name="localRankings">The vote rankings.</param>
    /// <returns>Returns the vote string for the least preferred vote.</returns>
    protected override VoteBlock GetLeastPreferredChoice(
        VotesByVoterF voterPreferences)
    {
        return voterPreferences.GroupBy(v => v.Value.First()) // highest rankings
            .MinBy(r => r.Count())    // least preferred
            ?.Key ?? VoteBlock.Empty; // the vote block found, or Empty if none
    }
}
