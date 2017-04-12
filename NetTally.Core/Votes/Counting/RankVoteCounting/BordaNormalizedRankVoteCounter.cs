using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    /// <summary>
    /// Borda is being removed as a valid option from the list of rank vote options.
    /// Aside from systemic failures of the method itself, it cannot give proper
    /// valuation to unranked options, which intrinsically makes it a bad fit
    /// for handling user-entered quest voting schemes.
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    public class BordaNormalizedRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            Debug.WriteLine(">>Normalized Borda Counting<<");

            //var voterCount = task.SelectMany(t => t.Value).Distinct().Count();

            var groupVotes = GroupRankVotes.GroupByVoteAndRank(task);

            var rankedVotes = from vote in groupVotes
                              select new { Vote = vote.VoteContent, Rank = RankVote(vote.Ranks) };

            var orderedVotes = rankedVotes.OrderBy(a => a.Rank);

            RankResults results = new RankResults();

            results.AddRange(orderedVotes.Select(a =>
                new RankResult(a.Vote, $"BordaNorm: [{a.Rank:f5}]")));

            return results;
        }

        /// <summary>
        /// Ranks the vote.
        /// </summary>
        /// <param name="ranks">Votes with associated ranks, for the voters who ranked the vote with a given value.</param>
        /// <returns>Returns a numeric evaluation of the overall rank of the vote.</returns>
        private static double RankVote(IEnumerable<RankedVoters> ranks)
        {
            double voteValue = 0;

            // Add up the sum of the number of voters times the value of each rank.
            // Average the results, and then scale by the number of voters who ranked this option.
            // Ranking value is Borda+1, so that ranks 1 through 9 are given values 2 through 10.
            // That means first place is 5x as valuable as last place, rather than 9x as valuable.

            foreach (var r in ranks)
            {
                voteValue += (r.Rank + 1.0) * r.Voters.Count();
            }

            int totalRankings = ranks.Sum(a => a.Voters.Count());

            voteValue = voteValue / totalRankings / totalRankings;

            return voteValue;
        }
    }
}
