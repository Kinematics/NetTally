using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NetTally.VoteCounting
{
    // List of preference results ordered by winner
    using RankResults = List<string>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    /// <summary>
    /// Wilson vote scoring uses the lower bounds of a Bournoulli analysis of the vote
    /// rankings to get the 95% minimum confidence interval.
    /// This means that a voted item with only a few supporters will have a low score
    /// due to a high error margin, while a score with more supporters will have a
    /// higher relative confidence rating.
    /// This improves on the Borda scoring, which has no means of compensating for
    /// votes that are ranked by less than 100% of the voter base.
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    public class WilsonRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            Debug.WriteLine(">>Wilson Scoring<<");

            // Can calculating the score easily by having all the rankings for
            // each vote grouped together.
            var groupVotes = GroupRankVotes.GroupByVoteAndRank(task);

            var rankedVotes = from vote in groupVotes
                              select new { Vote = vote.VoteContent, Rank = RankScoring.LowerWilsonScore(vote.Ranks) };

            var orderedVotes = rankedVotes.OrderByDescending(a => a.Rank);

            // Display the votes and their ratings for debugging purposes.
            foreach (var orderedVote in orderedVotes)
            {
                Debug.WriteLine($"- {orderedVote.Vote} [{orderedVote.Rank:f5}]");
            }

            // Only return the list of vote options themselves.
            return orderedVotes.Select(a => a.Vote).ToList();
        }
    }
}
