using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally.Utility;

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

    public class InstantRunoffRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Local class to store a choice/count combo of fields for LINQ.
        /// </summary>
        protected class ChoiceCount
        {
            public string Choice { get; set; }
            public int Count { get; set; }

            public override string ToString() => $"{Choice}: {Count}";
        }

        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            List<string> winningChoices = new List<string>();

            if (task.Any())
            {
                Debug.WriteLine(">>Instant Runoff<<");

                var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);

                for (int i = 1; i <= 9; i++)
                {
                    string winner = GetWinningVote(voterRankings, winningChoices);

                    if (winner == null)
                        break;

                    winningChoices.Add(winner);
                    Debug.WriteLine($"- {winner}");
                }
            }

            return winningChoices;
        }

        /// <summary>
        /// Gets the winning vote, instant runoff style.
        /// </summary>
        /// <param name="voterRankings">The voters' rankings.</param>
        /// <param name="chosenChoices">The already chosen choices that we should exclude.</param>
        /// <returns>Returns the winning vote, if any.  Otherwise, null.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        private string GetWinningVote(IEnumerable<VoterRankings> voterRankings, IEnumerable<string> chosenChoices)
        {
            if (voterRankings == null)
                throw new ArgumentNullException(nameof(voterRankings));
            if (chosenChoices == null)
                throw new ArgumentNullException(nameof(chosenChoices));

            var localRankings = voterRankings;

            foreach (var choice in chosenChoices)
            {
                localRankings = RemoveChoiceFromVotes(localRankings, choice);
            }

            int voterCount = localRankings.Count(v => v.RankedVotes.Any());
            int winCount = (int)Math.Ceiling(voterCount * 0.50011);

            while (true)
            {
                var preferredVotes = GetPreferredCounts(localRankings);

                if (!preferredVotes.Any())
                    break;

                var best = preferredVotes.MaxObject(a => a.Count);

                if (best.Count >= winCount)
                    return best.Choice;

                var worst = preferredVotes.MinObject(a => a.Count);

                localRankings = RemoveChoiceFromVotes(localRankings, worst.Choice); 
            }

            return null;
        }

        /// <summary>
        /// Filter the provided list of voter rankings to remove any instances of the specified choice.
        /// </summary>
        /// <param name="voterRankings">The votes to filter.</param>
        /// <param name="choice">The choice to remove.</param>
        /// <returns>Returns the list without the given choice in the voters' rankings.</returns>
        private IEnumerable<VoterRankings> RemoveChoiceFromVotes(IEnumerable<VoterRankings> voterRankings, string choice)
        {
            var res = from voter in voterRankings
                      select new VoterRankings
                      {
                          Voter = voter.Voter,
                          RankedVotes = voter.RankedVotes.Where(v => v.Vote != choice).ToList()
                      };

            return res;
        }

        /// <summary>
        /// Gets the count of the number of times a given vote is the most preferred option
        /// among the provided voters.
        /// </summary>
        /// <param name="voterRankings">The list of voters and their rankings of each option.</param>
        /// <returns>Returns a collection of Choice/Count objects.</returns>
        private IEnumerable<ChoiceCount> GetPreferredCounts(IEnumerable<VoterRankings> voterRankings)
        {
            var preferredVotes = from voter in voterRankings
                                 let preferred = GetPreferredVote(voter.RankedVotes)
                                 where preferred != null
                                 group voter by preferred into preffed
                                 select new ChoiceCount { Choice = preffed.Key, Count = preffed.Count() };

            return preferredVotes;
        }

        /// <summary>
        /// Gets the preferred vote (ie: highest ranked) from a collection of ranked votes.
        /// </summary>
        /// <param name="voterRankings">A voter's rankings.</param>
        /// <returns>Returns the vote component of the most preferred vote in the list,
        /// or null if none are present.</returns>
        private string GetPreferredVote(IEnumerable<RankedVote> voterRankings)
        {
            var choice = voterRankings.OrderBy(a => a.Rank).FirstOrDefault()?.Vote;
            return choice;
        }
    }
}
