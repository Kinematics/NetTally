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


    public class RIRVRankVoteCounter : BaseRankVoteCounter
    {
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            Debug.WriteLine(">>Rated Instant Runoff<<");

            List<string> winningChoices = new List<string>();

            var groupVotes = GroupRankVotes.GroupByVoteAndRank(task);
            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
            var allChoices = GetAllChoices(voterRankings);


            for (int i = 1; i <= 9; i++)
            {
                string winner = GetWinningVote(voterRankings, groupVotes);

                if (winner == null)
                    break;

                winningChoices.Add(winner);
                allChoices.Remove(winner);

                Debug.WriteLine($"- {winner}");

                if (!allChoices.Any())
                    break;

                voterRankings = RemoveChoiceFromVotes(voterRankings, winner);
                groupVotes = RemoveChoiceFromRanks(groupVotes, winner);
            }

            return winningChoices;
        }

        private string GetWinningVote(IEnumerable<VoterRankings> voterRankings, IEnumerable<RankGroupedVoters> groupVotes)
        {
            var rankedVotes = from vote in groupVotes
                              select new { Vote = vote.VoteContent, Rank = RankScoring.LowerWilsonScore(vote.Ranks) };

            var orderedVotes = rankedVotes.OrderByDescending(a => a.Rank);

            var topTwo = orderedVotes.Take(2);

            if (!topTwo.Any())
                return null;

            if (topTwo.Count() == 1)
                return topTwo.First().Vote;

            string option1 = topTwo.First().Vote;
            string option2 = topTwo.Last().Vote;
            bool option1ScoreHigher = topTwo.First().Rank > topTwo.Last().Rank;


            Debug.Write($"[{option1}, {option2}] ");

            string preferredOption = GetOptionWithHigherPrefCount(voterRankings, option1, option2, option1ScoreHigher);

            return preferredOption;
        }


        private string GetOptionWithHigherPrefCount(IEnumerable<VoterRankings> voterRankings, string option1, string option2, bool option1ScoreHigher)
        {
            int count1 = 0;
            int count2 = 0;

            foreach (var voter in voterRankings)
            {
                var rank1 = voter.RankedVotes.FirstOrDefault(a => a.Vote == option1);
                var rank2 = voter.RankedVotes.FirstOrDefault(a => a.Vote == option2);

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

            if (count1 > count2)
            {
                return option1;
            }
            if (count2 > count1)
            {
                return option2;
            }
            if (option1ScoreHigher)
            {
                return option1;
            }
            return option2;
        }


        /// <summary>
        /// Gets all choices from all user votes.
        /// </summary>
        /// <param name="rankings">The collection of user votes.</param>
        /// <returns>Returns a list of all the choices in the task.</returns>
        private List<string> GetAllChoices(IEnumerable<VoterRankings> rankings)
        {
            var res = rankings.SelectMany(r => r.RankedVotes).Select(r => r.Vote).Distinct();

            return res.ToList();
        }

        /// <summary>
        /// Removes a list of choices from voter rankings.
        /// These are the choices that have already won a rank spot.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="choice">The already chosen choices.</param>
        /// <returns>Returns the results as a list.</returns>
        private List<VoterRankings> RemoveChoiceFromVotes(IEnumerable<VoterRankings> voterRankings, string choice)
        {
            var res = from voter in voterRankings
                      select new VoterRankings
                      {
                          Voter = voter.Voter,
                          RankedVotes = voter.RankedVotes
                              .Where(v => choice != v.Vote)
                              .OrderBy(v => v.Rank)
                              .Select((a, b) => new RankedVote { Vote = a.Vote, Rank = b + 1 })
                              .ToList()
                      };

            return res.ToList();
        }

        private List<RankGroupedVoters> RemoveChoiceFromRanks(IEnumerable<RankGroupedVoters> groupVotes, string winner)
        {
            var res = groupVotes.Where(a => a.VoteContent != winner);

            return res.ToList();
        }


        /// <summary>
        /// Removes a list of choices from voter rankings.
        /// These are the choices that have already won a rank spot.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="chosenChoices">The already chosen choices.</param>
        /// <returns>Returns the results as a list.</returns>
        private List<VoterRankings> RemoveChoicesFromVotes(IEnumerable<VoterRankings> voterRankings, List<string> chosenChoices)
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

    }
}
