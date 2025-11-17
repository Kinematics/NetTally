using NetTally.Models;
using NetTally.Tally.Storage;

namespace NetTally.Tally.RankCounting.Reference;

/// <summary>
/// Borda is being removed as a valid option from the list of rank vote options.
/// Aside from systemic failures of the method itself, it cannot give proper
/// valuation to unranked options, which intrinsically makes it a bad fit
/// for handling user-entered quest voting schemes.
/// </summary>
public class BordaNormalized : IRankVoteCounter
{
    /// <summary>
    /// Calculate the rank order for the votes provided for the current task.
    /// Use the normalized Borda Count method.
    /// </summary>
    /// <param name="taskVotes">The votes in the current task.</param>
    /// <returns>Returns a list of rankings per vote.</returns>
    public List<((int rank, double rankScore) ranking, VoteStorageEntryF vote)>
        CountVotesForTask(VoteStorage taskVotes)
    {
        List<((int rank, double rankScore) ranking, VoteStorageEntryF vote)> resultList = [];

        var processedVotes = taskVotes
            .Select(v => new { score = GetBordaScore(v), vote = v })
            .OrderByDescending(a => a.score)
            .ThenBy(a => a.vote.Value.First().Key.PostId, PostIdComparer.Instance)
            .ToList();

        for (int i = 0; i < processedVotes.Count; i++)
        {
            resultList.Add(((i + 1, processedVotes[i].score), processedVotes[i].vote));
        }

        return resultList;
    }

    /// <summary>
    /// Add up the sum of the number of voters times the value of each rank.
    /// Average the results, and then scale by the number of voters who ranked this option.
    /// Ranking value is Borda+1, so that ranks 1 through 9 are given values 2 through 10.
    /// That means first place is 5x as valuable as last place, rather than 9x as valuable.
    /// </summary>
    /// <param name="vote">The vote being scored.</param>
    /// <returns>Returns the Borda score based on the voters for the vote.</returns>
    private static double GetBordaScore(VoteStorageEntryF vote)
    {
        double voteValue = 0;
        int count = 0;

        // Add up the sum of the number of voters times the value of each rank.
        // Average the results, and then scale by the number of voters who ranked this option.
        // Ranking value is Borda+1, so that ranks 1 through 9 are given values 2 through 10.
        // That means first place is 5x as valuable as last place, rather than 9x as valuable.
        foreach (var voter in vote.Value)
        {
            if (voter.Key is UserOrigin && voter.Value.Marker is RankMarker)
            {
                voteValue += 1.0 + voter.Value.Marker.Value;
                count++;
            }
        }

        voteValue = voteValue / count / count;

        return voteValue;
    }
}
