using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NetTally.VoteCounting
{
    // List of preference results ordered by winner
    using RankResults = List<string>;
    // Task (string), Ordered list of ranked votes
    using RankResultsByTask = Dictionary<string, List<string>>;
    // Vote (string), collection of voters
    using SupportedVotes = Dictionary<string, HashSet<string>>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

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
        private double RankVote(IEnumerable<RankedVoters> ranks)
        {
            double voteValue = 0;

            // Add up the sum of the number of voters times the value of each rank.
            // If any voter didn't vote for an option, they effectively add a 0 (rank #10) for that option.
            foreach (var r in ranks)
            {
                voteValue += ValueOfRank(r.Rank) * r.Voters.Count();
            }

            return voteValue;
        }

        /// <summary>
        /// Get the numeric value of a given rank. 
        /// </summary>
        /// <param name="rank">The rank being evaluated.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private double ValueOfRank(string rank)
        {
            if (string.IsNullOrEmpty(rank))
                throw new ArgumentNullException(nameof(rank));

            int rankAsInt = int.Parse(rank);

            if (rankAsInt < 1 || rankAsInt > 9)
                throw new ArgumentOutOfRangeException(nameof(rank));

            // Ranks valued at 1 for #1, then 1/N for each higher N.
            double rankValue = 1.0 / rankAsInt;

            return rankValue;
        }
    }
}
