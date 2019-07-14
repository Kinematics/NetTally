using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.Extensions;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.Experiment3
{
    public class BaldwinRankVoteCounter : IRankVoteCounter2
    {
        public List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            int r = 1;

            List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)> resultList
                = new List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>();

            var workingVotes = new VoteStorage(taskVotes);

            while (workingVotes.Count > 0)
            {
                var (vote, score) = GetWinningVote(workingVotes);

                resultList.Add(((r++, score), vote));

                workingVotes.Remove(vote.Key);
            }

            return resultList;
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
        private (KeyValuePair<VoteLineBlock, VoterStorage> vote, double score)
            GetWinningVote(VoteStorage votes)
        {
            var workingVotes = new VoteStorage(votes);

            int voterCount = workingVotes.SelectMany(a => a.Value).Distinct().Count();
            int winCount = voterCount / 2 + 1;

            while (workingVotes.Count > 1)
            {
                var (vote, count) = GetMostPreferredVote(workingVotes);

                if (count >= winCount)
                {
                    var fullVote = workingVotes.First(a => a.Key == vote);
                    return (fullVote, count);
                }

                var leastPreferredChoice = GetLeastPreferredChoice(workingVotes);

                workingVotes.Remove(leastPreferredChoice);
            }

            // If we get to here, the only option left has to win.
            return (workingVotes.First(), 1);
        }

        /// <summary>
        /// Gets the count of the number of times a given vote is the most preferred option
        /// among the provided voters.
        /// </summary>
        /// <param name="voterRankings">The list of voters and their rankings of each option.</param>
        /// <returns>Returns a collection of Choice/Count objects.</returns>
        private (VoteLineBlock vote, int count)
            GetMostPreferredVote(VoteStorage votes)
        {
            // Invert the votes so that we can look at preferences per user.
            var voterPreferences = votes.SelectMany(v => v.Value).GroupBy(u => u.Key).ToDictionary(t => t.Key, s => s.Select(q => q.Value).ToHashSet());

            List<VoteLineBlock> bests = new List<VoteLineBlock>();

            foreach (var voter in voterPreferences)
            {
                var best = voter.Value.MinObject(a => a.MarkerValue);
                bests.Add(best);
            }

            var group = bests.GroupBy(a => a).MaxObject(a => a.Count());

            return (group.Key, group.Count());
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
        private VoteLineBlock GetLeastPreferredChoice(VoteStorage votes)
        {
            var rankedVotes = from vote in votes
                              select new { rating = (vote, RankScoring.LowerWilsonScore(vote)) };

            var worstVote = rankedVotes.MinObject(a => a.rating.Item2);

            Debug.Write($"({worstVote.rating.Item2:f5})");

            return worstVote.rating.vote.Key;
        }
    }
}

