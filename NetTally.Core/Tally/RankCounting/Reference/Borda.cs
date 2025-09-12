using NetTally.Tally.Posts.Comparer;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.RankCounting;
using NetTally.Tally.Storage;
using NetTally.Tally.Vote.Component;

namespace NetTally.Tally.RankCounting.Reference;

/// <summary>
/// Borda is being removed as a valid option from the list of rank vote options.
/// Aside from systemic failures of the method itself, it cannot give proper
/// valuation to unranked options, which intrinsically makes it a bad fit
/// for handling user-entered quest voting schemes.
/// </summary>
public class Borda : IRankVoteCounter
{
    /// <summary>
    /// Calculate the rank order for the votes provided for the current task.
    /// Use the Borda Count method.
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
    /// Rank the vote using Borda math.  Ranks closer to 1 have higher value.
    /// To handle scaling, rank 6 is considered to be worth 0 points, which
    /// means rank 1 is 5 points, and rank 9 is -3 points.
    /// </summary>
    /// <param name="vote">The vote being scored.</param>
    /// <returns>Returns the Borda score based on the voters for the vote.</returns>
    private static double GetBordaScore(VoteStorageEntryF vote)
    {
        double voteValue = 0;
        int count = 0;

        // Add up the sum of the number of voters times the value of each rank.
        // If any voter didn't vote for an option, they effectively add a 0 (rank #6) for that option.
        foreach (var voter in vote.Value)
        {
            if (voter.Key is UserOrigin && voter.Value.Marker is RankMarker)
            {
                voteValue += 6 - voter.Value.Marker.Value;
                count++;
            }
        }

        return voteValue / count;
    }
}
