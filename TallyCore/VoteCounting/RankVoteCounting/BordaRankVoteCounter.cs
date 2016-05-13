using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class BordaRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            var groupVotes = GroupRankVotes.GroupVotesByVoteAndRank(task);

            var rankedVotes = from vote in groupVotes
                              select new { Vote = vote.VoteContent, Rank = RankVote(vote.Ranks) };

            var orderedVotes = rankedVotes.OrderByDescending(a => a.Rank);

            foreach (var orderedVote in orderedVotes)
            {
                Debug.WriteLine($"- {orderedVote.Vote} [{orderedVote.Rank}]");
            }

            return orderedVotes.Select(a => a.Vote).ToList();
        }

        private int RankVote(IEnumerable<RankedVoters> ranks)
        {
            var rankVals = from r in ranks
                           select ValueOfRank(r.Rank);

            int voteValue = 0;

            foreach (var r in ranks)
            {
                int rankValue = ValueOfRank(r.Rank);

                voteValue += rankValue * r.Voters.Count();
            }

            return voteValue;
        }

        private int ValueOfRank(string rank)
        {
            if (string.IsNullOrEmpty(rank))
                throw new ArgumentNullException(nameof(rank));

            int rankAsInt = int.Parse(rank);

            if (rankAsInt < 1 || rankAsInt > 9)
                throw new ArgumentOutOfRangeException(nameof(rank));

            // Ranks valued at 5 for #1, then -1 per rank below that, to a minimum of -3.
            int rankValue = (6 - rankAsInt);

            return rankValue;
        }

    }


}
