using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.Extensions;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    class InstantRunoffRankVoteCounter : BaseRankVoteCounter
    {
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

            RankResults winningChoices = new RankResults();

            if (task.Any())
            {
                var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);

                for (int i = 1; i <= 9; i++)
                {
                    RankResult? winner = GetWinningVote(voterRankings, winningChoices);

                    if (winner == null)
                        break;

                    winningChoices.Add(winner);
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
        private RankResult? GetWinningVote(IEnumerable<VoterRankings> voterRankings, RankResults chosenChoices)
        {
            List<VoterRankings> localRankings = RemoveChoicesFromVotes(voterRankings, chosenChoices.Select(c => c.Option));

            int voterCount = localRankings.Count(v => v.RankedVotes.Any());
            int winCount = voterCount / 2 + 1;
            string eliminated = "";

            bool eliminateOne = false;

            while (true)
            {
                var preferredVotes = GetPreferredCounts(localRankings);

                if (!preferredVotes.Any())
                    break;

                var best = preferredVotes.MaxObject(a => a.Count);

                if (best.Count >= winCount)
                    return new RankResult(best.Choice, $"IRV Eliminations: [{eliminated}]");

                var worst = preferredVotes.MinObject(a => a.Count);

                eliminated += Comma(eliminateOne) + worst.Choice;

                RemoveChoiceFromVotes(localRankings, worst.Choice);
                eliminateOne = true;
            }

            return null;
        }

        private static string Comma(bool addComma)
        {
            if (addComma)
                return ", ";
            else
                return "";
        }

        /// <summary>
        /// Removes a list of choices from voter rankings.
        /// These are the choices that have already won a rank spot.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="chosenChoices">The already chosen choices.</param>
        /// <returns>Returns the results as a list.</returns>
        private static List<VoterRankings> RemoveChoicesFromVotes(IEnumerable<VoterRankings> voterRankings, IEnumerable<string> chosenChoices)
        {
            var res = from voter in voterRankings
                      select new VoterRankings
                      (
                          voter: voter.Voter,
                          rankedVotes: voter.RankedVotes.Where(v => chosenChoices.Contains(v.Vote) == false).OrderBy(v => v.Rank).ToList()
                      );

            return res.ToList();
        }

        /// <summary>
        /// Filter the provided list of voter rankings to remove any instances of the specified choice.
        /// </summary>
        /// <param name="voterRankings">The votes to filter.</param>
        /// <param name="choice">The choice to remove.</param>
        /// <returns>Returns the list without the given choice in the voters' rankings.</returns>
        private static void RemoveChoiceFromVotes(IEnumerable<VoterRankings> voterRankings, string choice)
        {
            foreach (var ranker in voterRankings)
            {
                ranker.RankedVotes.RemoveAll(v => v.Vote == choice);
            }
        }

        /// <summary>
        /// Gets the count of the number of times a given vote is the most preferred option
        /// among the provided voters.
        /// </summary>
        /// <param name="voterRankings">The list of voters and their rankings of each option.</param>
        /// <returns>Returns a collection of Choice/Count objects.</returns>
        private IEnumerable<CountedChoice> GetPreferredCounts(IEnumerable<VoterRankings> voterRankings)
        {
            var preferredVotes = from voter in voterRankings
                                 let preferred = GetPreferredVote(voter.RankedVotes)
                                 where preferred != null
                                 group voter by preferred into preffed
                                 select new CountedChoice(choice: preffed.Key, count: preffed.Count());

            return preferredVotes;
        }

        /// <summary>
        /// Gets the preferred vote (ie: highest ranked) from a collection of ranked votes.
        /// </summary>
        /// <param name="voterRankings">A voter's rankings.</param>
        /// <returns>Returns the vote component of the most preferred vote in the list,
        /// or null if none are present.</returns>
        private static string GetPreferredVote(IEnumerable<RankedVote> voterRankings)
        {
            var choice = voterRankings.OrderBy(a => a.Rank).FirstOrDefault()?.Vote;
            return choice;
        }
    }
}
