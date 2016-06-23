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

    /// <summary>
    /// Rated Instant Runoff voting scores all vote options, taking the top two,
    /// and does an instant runoff between them.
    /// This uses the Wilson score for scoring votes.
    /// The scoring aspect allows relative preference to be taken into account,
    /// but relative preference is ignored for the runoff phase, which merely
    /// checks for which of A or B is most often preferred over the other.
    /// This avoids the flaws of standard instant runoff voting by incorporating
    /// score ratings into the evaluation.
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    public class RIRVRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            Debug.WriteLine(">>Rated Instant Runoff<<");

            List<string> winningChoices = new List<string>();

            // The groupVotes are used for getting the Wilson score
            var rankedVotes = GroupRankVotes.GroupByVoteAndRank(task);
            // The voterRankings are used for the runoff
            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
            // The full choices list is just to keep track of how many we have left.
            var allChoices = GroupRankVotes.GetAllChoices(voterRankings);

            for (int i = 1; i <= 9; i++)
            {
                string winner = GetWinningVote(voterRankings, rankedVotes);

                if (winner == null)
                    break;

                winningChoices.Add(winner);
                allChoices.Remove(winner);

                Debug.WriteLine($"- {winner}");

                if (!allChoices.Any())
                    break;

                voterRankings = RemoveChoiceFromVotes(voterRankings, winner);
                rankedVotes = RemoveChoiceFromRanks(rankedVotes, winner);
            }

            return winningChoices;
        }

        /// <summary>
        /// Gets the winning vote.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="rankedVotes">The votes, ranked.</param>
        /// <returns></returns>
        private string GetWinningVote(IEnumerable<VoterRankings> voterRankings, IEnumerable<RankGroupedVoters> rankedVotes)
        {
            string option1;
            string option2;

            GetTopTwoRatedOptions(rankedVotes, out option1, out option2);

            string winner = GetOptionWithHigherPrefCount(voterRankings, option1, option2);

            return winner;
        }

        /// <summary>
        /// Gets the top two rated options.
        /// </summary>
        /// <param name="rankedVotes">The group votes.</param>
        /// <param name="option1">The top rated option. Null if there aren't any options available.</param>
        /// <param name="option2">The second rated option.  Null if there is only one option available.</param>
        private void GetTopTwoRatedOptions(IEnumerable<RankGroupedVoters> rankedVotes, out string option1, out string option2)
        {
            var scoredVotes = from vote in rankedVotes
                              select new { Vote = vote.VoteContent, Rank = RankScoring.LowerWilsonScore(vote.Ranks) };

            var orderedVotes = scoredVotes.OrderByDescending(a => a.Rank);

            var topTwo = orderedVotes.Take(2);

            if (!topTwo.Any())
            {
                option1 = null;
                option2 = null;
            }
            else if (topTwo.Count() == 1)
            {
                option1 = topTwo.First().Vote;
                option2 = null;
            }
            else
            {
                option1 = topTwo.First().Vote;
                option2 = topTwo.Last().Vote;
            }

            Debug.Write($"[{option1 ?? ""}, {option2 ?? ""}] ");
        }

        /// <summary>
        /// Gets the option with higher preference count.
        /// This is the runoff portion of the vote evaluation.  Whichever option has more
        /// people that prefer it over the other, wins.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.  This allows seeing which option each voter preferred.</param>
        /// <param name="option1">The first option up for consideration.</param>
        /// <param name="option2">The second option up for consideration.</param>
        /// <returns>Returns the winning option.</returns>
        private string GetOptionWithHigherPrefCount(IEnumerable<VoterRankings> voterRankings, string option1, string option2)
        {
            if (string.IsNullOrEmpty(option2))
                return option1;

            int count1 = 0;
            int count2 = 0;

            foreach (var voter in voterRankings)
            {
                var rank1 = voter.RankedVotes.FirstOrDefault(a => StringUtility.AgnosticStringComparer.Equals(a.Vote, option1));
                var rank2 = voter.RankedVotes.FirstOrDefault(a => StringUtility.AgnosticStringComparer.Equals(a.Vote, option2));

                if (rank1 == null && rank2 == null)
                    continue;

                if (rank1 == null)
                {
                    count2++;
                    continue;
                }

                if (rank2 == null)
                {
                    count1++;
                    continue;
                }

                if (rank1.Rank > rank2.Rank)
                {
                    count2++;
                }
                else if (rank2.Rank > rank1.Rank)
                {
                    count1++;
                }
            }

            // If count1==count2, we use the higher scored option, which
            // will necessarily be option1.  Therefore all ties will be
            // in favor of option1, and the only thing we need to check
            // for is if option2 wins explicitly.
            if (count2 > count1)
            {
                return option2;
            }

            return option1;
        }

        /// <summary>
        /// Removes the specified vote choice from the list of ranked votes.
        /// </summary>
        /// <param name="rankedVotes">The ranked votes.</param>
        /// <param name="choice">The choice being removed.</param>
        /// <returns></returns>
        private IEnumerable<RankGroupedVoters> RemoveChoiceFromRanks(IEnumerable<RankGroupedVoters> rankedVotes, string choice)
        {
            var res = rankedVotes.Where(a => !StringUtility.AgnosticStringComparer.Equals(a.VoteContent, choice));

            return res.ToList();
        }

        /// <summary>
        /// Removes a list of choices from voter rankings.
        /// These are the choices that have already won a rank spot.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="choice">The already chosen choices.</param>
        /// <returns>Returns the results as a list.</returns>
        private IEnumerable<VoterRankings> RemoveChoiceFromVotes(IEnumerable<VoterRankings> voterRankings, string choice)
        {
            var res = from voter in voterRankings
                      select new VoterRankings
                      {
                          Voter = voter.Voter,
                          RankedVotes = voter.RankedVotes
                              .Where(v => !StringUtility.AgnosticStringComparer.Equals(v.Vote, choice))
                              .ToList()
                      };

            return res.ToList();
        }

    }
}
