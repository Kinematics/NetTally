using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.Utility;

namespace NetTally.VoteCounting
{
    // List of preference results ordered by winner
    using RankResults = List<string>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;


    public class BaldwinRankVoteCounter : BaseRankVoteCounter
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
                Debug.WriteLine(">>Baldwin Runoff<<");

                var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
                var allChoices = GroupRankVotes.GetAllChoices(voterRankings);

                for (int i = 1; i <= 9; i++)
                {
                    string winner = GetWinningVote(voterRankings, winningChoices);

                    if (winner == null)
                        break;

                    winningChoices.Add(winner);
                    allChoices.Remove(winner);

                    Debug.WriteLine($"- {winner}");

                    if (!allChoices.Any())
                        break;
                }
            }

            return winningChoices;
        }

        /// <summary>
        /// Gets the winning vote.
        /// Excludes any already chosen votes from the process.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="chosenChoices">The already chosen choices.</param>
        /// <returns>Returns the winning vote.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        private string GetWinningVote(IEnumerable<VoterRankings> voterRankings, RankResults chosenChoices)
        {
            if (voterRankings == null)
                throw new ArgumentNullException(nameof(voterRankings));
            if (chosenChoices == null)
                throw new ArgumentNullException(nameof(chosenChoices));

            // Initial conversion from enumerable to list
            List<VoterRankings> localRankings = RemoveChoicesFromVotes(voterRankings, chosenChoices);

            int voterCount = localRankings.Count();
            int winCount = voterCount / 2 + 1;
            string eliminated = "";

            try
            {
                bool eliminateOne = false;

                while (true)
                {
                    var preferredVotes = GetPreferredCounts(localRankings);

                    if (!preferredVotes.Any())
                        break;

                    ChoiceCount best = preferredVotes.MaxObject(a => a.Count);

                    if (best.Count >= winCount)
                        return best.Choice;

                    // If no more choice removals will bump up lower prefs to higher prefs, return the best of what's left.
                    if (!localRankings.Any(r => r.RankedVotes.Count() > 1))
                        return best.Choice;

                    eliminated += Comma(eliminateOne);

                    string leastPreferredChoice = GetLeastPreferredChoice(localRankings);

                    RemoveChoiceFromVotes(localRankings, leastPreferredChoice);
                    eliminateOne = true;
                }
            }
            finally
            {
                Debug.WriteLine($"Eliminations: [{eliminated}]");
            }

            return null;
        }

        private static string Comma(bool addComma) => addComma ? ", " : "";


        /// <summary>
        /// Removes a list of choices from voter rankings.
        /// These are the choices that have already won a rank spot.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="chosenChoices">The already chosen choices.</param>
        /// <returns>Returns the results as a list.</returns>
        private static List<VoterRankings> RemoveChoicesFromVotes(IEnumerable<VoterRankings> voterRankings, List<string> chosenChoices)
        {
            var res = from voter in voterRankings
                      select new VoterRankings
                      {
                          Voter = voter.Voter,
                          RankedVotes = voter.RankedVotes
                              .Where(v => chosenChoices.Contains(v.Vote) == false)
                              .OrderBy(v => v.Rank)
                              .Select((a, b) => new RankedVote { Vote = a.Vote, Rank = b + 1 })
                              .ToList()
                      };

            return res.ToList();
        }

        /// <summary>
        /// Filter the provided list of voter rankings to remove any instances of the specified choice.
        /// Modifies the provided list.
        /// </summary>
        /// <param name="voterRankings">The votes to filter.</param>
        /// <param name="choice">The choice to remove.</param>
        private static void RemoveChoiceFromVotes(List<VoterRankings> voterRankings, string choice)
        {
            foreach (var ranker in voterRankings)
            {
                ranker.RankedVotes.RemoveAll(v => v.Vote == choice);
            }
        }

        /// <summary>
        /// Gets the least preferred choice.
        /// This is normally determined by selecting the option with the lowest Borda count.
        /// This is inverted because we don't want to convert ranks to Borda values (it gains us nothing).
        /// It then needs to be averaged across the number of instances of each vote, to 
        /// account for unranked options.  This allows apples-to-apples comparisons against options
        /// that are ranked in all votes.
        /// We then need to scale it relative to the number of instances of that option appearing, to deal
        /// with truncated rankings (where there are more options than rankings allowed).
        /// An option ranked infrequently can be scaled up relative to its rate of occurance for
        /// a high likelihood of elimination.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <returns>Returns the vote string for the least preferred vote.</returns>
        private static string GetLeastPreferredChoice(List<VoterRankings> localRankings)
        {
            var groupVotes = GroupRankVotes.GroupByVoteAndRank(localRankings);

            var rankedVotes = from vote in groupVotes
                              select new { Vote = vote.VoteContent, Rank = RankScoring.LowerWilsonScore(vote.Ranks) };

            var worstVote = rankedVotes.MinObject(a => a.Rank);

            Debug.Write($"({worstVote.Rank:f5}) {worstVote.Vote}");

            return worstVote.Vote;
        }

        /// <summary>
        /// Gets the count of the number of times a given vote is the most preferred option
        /// among the provided voters.
        /// </summary>
        /// <param name="voterRankings">The list of voters and their rankings of each option.</param>
        /// <returns>Returns a collection of Choice/Count objects.</returns>
        private static IEnumerable<ChoiceCount> GetPreferredCounts(IEnumerable<VoterRankings> voterRankings)
        {
            var preferredVotes = from voter in voterRankings
                                 let preferred = voter.RankedVotes.FirstOrDefault()?.Vote
                                 where preferred != null
                                 group voter by preferred into preffed
                                 select new ChoiceCount { Choice = preffed.Key, Count = preffed.Count() };

            return preferredVotes;
        }
    }
}
