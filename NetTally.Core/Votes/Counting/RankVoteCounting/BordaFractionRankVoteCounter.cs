using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // List of preference results ordered by winner
    using RankResults = List<string>;
    // Task (string), Ordered list of ranked votes
    using RankResultsByTask = Dictionary<string, List<string>>;
    // Vote (string), collection of voters
    using SupportedVotes = Dictionary<string, HashSet<string>>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    /// <summary>
    /// Borda is being removed as a valid option from the list of rank vote options.
    /// Aside from systemic failures of the method itself, it cannot give proper
    /// valuation to unranked options, which intrinsically makes it a bad fit
    /// for handling user-entered quest voting schemes.
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    public class BordaFractionRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            Debug.WriteLine(">>Borda Fractions<<");

            var groupVotes = GroupRankVotes.GroupByVoteAndRank(task);

            var rankedVotes = from vote in groupVotes
                              select new { Vote = vote.VoteContent, Rank = RankVote(vote.Ranks) };

            var orderedVotes = rankedVotes.OrderByDescending(a => a.Rank);

            foreach (var orderedVote in orderedVotes)
            {
                Debug.WriteLine($"- {orderedVote.Vote} [{orderedVote.Rank}]");
            }

            return orderedVotes.Select(a => a.Vote).ToList();
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
            // If any voter didn't vote for an option, they effectively add a 0 (rank #10) for that option.
            foreach (var r in ranks)
            {
                voteValue += (1.0 / r.Rank) * r.Voters.Count();
            }

            return voteValue;
        }
    }
}
