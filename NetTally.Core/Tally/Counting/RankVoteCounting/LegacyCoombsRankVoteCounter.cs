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

    class LegacyCoombsRankVoteCounter : BaseRankVoteCounter
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
                var allChoices = GroupRankVotes.GetAllChoices(voterRankings);

                for (int i = 1; i <= 9; i++)
                {
                    RankResult? winner = GetWinningVote(voterRankings, winningChoices, allChoices);

                    if (winner == null)
                        break;

                    winningChoices.Add(winner);
                    allChoices.Remove(winner.Option);

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
        /// <param name="allChoices">All remaining choices.</param>
        /// <returns>Returns the winning vote.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        private RankResult? GetWinningVote(IEnumerable<VoterRankings> voterRankings, RankResults chosenChoices, List<string> allChoices)
        {
            // Initial conversion from enumerable to list
            List<VoterRankings> localRankings = RemoveChoicesFromVotes(voterRankings, chosenChoices.Select(c => c.Option));

            AddUnselectedRankings(localRankings, allChoices);

            int voterCount = localRankings.Count();
            int winCount = voterCount / 2 + 1;
            string eliminated = "";

            bool eliminateOne = false;

            while (true)
            {
                var preferredVotes = GetPreferredCounts(localRankings);

                if (!preferredVotes.Any())
                    break;

                CountedChoice best = preferredVotes.MaxObject(a => a.Count);

                if (best.Count >= winCount)
                    return new RankResult(best.Choice, $"LCoombs Eliminations: [{eliminated}]");

                // If no more choice removals will bump up lower prefs to higher prefs, return the best of what's left.
                if (!localRankings.Any(r => r.RankedVotes.Count() > 1))
                    return new RankResult(best.Choice, $"LCoombs Eliminations: [{eliminated}]");

                string leastPreferredChoice = GetLeastPreferredChoice(localRankings);

                eliminated += Comma(eliminateOne) + leastPreferredChoice;

                RemoveChoiceFromVotes(localRankings, leastPreferredChoice);
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
        /// Adds ranking entries for any choices that users did not explictly rank.
        /// Modifies the provided list.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <param name="allChoices">All available choices.</param>
        private static void AddUnselectedRankings(List<VoterRankings> localRankings, List<string> allChoices)
        {
            foreach (var ranker in localRankings)
            {
                if (ranker.RankedVotes.Count == allChoices.Count)
                    continue;

                var extras = allChoices.Except(ranker.RankedVotes.Select(v => v.Vote));

                foreach (var extra in extras)
                {
                    ranker.RankedVotes.Add(new RankedVote(vote: extra, rank: 10));
                }
            }
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
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <returns>Returns the vote string for the least preferred vote.</returns>
        private static string GetLeastPreferredChoice(List<VoterRankings> localRankings)
        {
            Dictionary<string, int> rankTotals = new Dictionary<string, int>();

            foreach (var voter in localRankings)
            {
                foreach (var rank in voter.RankedVotes)
                {
                    if (!rankTotals.ContainsKey(rank.Vote))
                        rankTotals[rank.Vote] = 0;

                    rankTotals[rank.Vote] += rank.Rank;
                }
            }

            var maxRank = rankTotals.MaxObject(a => a.Value);

            return maxRank.Key;
        }

        /// <summary>
        /// Gets the count of the number of times a given vote is the most preferred option
        /// among the provided voters.
        /// </summary>
        /// <param name="voterRankings">The list of voters and their rankings of each option.</param>
        /// <returns>Returns a collection of Choice/Count objects.</returns>
        private static IEnumerable<CountedChoice> GetPreferredCounts(IEnumerable<VoterRankings> voterRankings)
        {
            var preferredVotes = from voter in voterRankings
                                 let preferred = voter.RankedVotes.First().Vote
                                 group voter by preferred into preffed
                                 select new CountedChoice(choice: preffed.Key, count: preffed.Count());

            return preferredVotes;
        }
    }
}
