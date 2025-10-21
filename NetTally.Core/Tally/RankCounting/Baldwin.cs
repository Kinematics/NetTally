using System.Diagnostics;
using NetTally.Models.Votes;
using NetTally.Tally.RankCounting.Reference;
using NetTally.Tally.Storage;

namespace NetTally.Tally.RankCounting;

/// <summary>
/// Implement ranking votes using the Baldwin method.
/// It's an instant runoff that uses Wilson scoring to determine
/// which vote to remove each round.
/// </summary>
public class Baldwin : InstantRunoffBase
{
    protected override bool LeastPreferredChecksFullVotes { get; } = true;

    /// <summary>
    /// Gets the least preferred choice.
    /// With the Baldwin method, this is the vote with the lowest Wilson Score.
    /// </summary>
    /// <param name="localRankings">The vote rankings.</param>
    /// <returns>Returns the vote string for the least preferred vote.</returns>
    protected override VoteBlock GetLeastPreferredChoice(VoteStorage votes)
    {
        var rankedVotes = from vote in votes
                          select new { rating = (Vote: vote, Calc: RankingCalculations.LowerWilsonRankingScore(vote)) };

        var worstVote = rankedVotes.MinBy(a => a.rating.Calc.score);

        if (worstVote == null)
        {
            return VoteBlock.Empty;
        }

        Debug.Write($"({worstVote.rating.Calc.score:f5})");

        return worstVote.rating.Vote.Key;
    }
}

