using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    /// <summary>
    /// Implement ranking votes using any instant runoff method.
    /// Each round, the least preferred choice is removed, until
    /// one option has the majority of top-ranked votes.
    /// </summary>
    public class InstantRunoffBase : IRankVoteCounter2
    {
        protected bool invertVotesToCheckPreferred = false;


        public List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            int r = 1;

            List<((int rank, double rankScore) ranking, VoteStorageEntry vote)> resultList
                = new List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>();

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
        private (VoteStorageEntry vote, double score) GetWinningVote(VoteStorage votes)
        {
            var workingVotes = new VoteStorage(votes);

            int voterCount = workingVotes.SelectMany(a => a.Value).Distinct().Count();
            int winCount = voterCount / 2 + 1;

            while (workingVotes.Count > 1)
            {
                // Invert the votes so that we can look at preferences per user.
                var voterPreferences = workingVotes
                    .SelectMany(v => v.Value)
                    .GroupBy(u => u.Key)
                    .ToDictionary(t => t.Key, s => s.Select(q => q.Value).OrderBy(r => r.MarkerValue).ToList());

                // Check to see if we have a winner.
                var (vote, count) = GetMostPreferredVote(voterPreferences);

                if (count >= winCount)
                {
                    var fullVote = workingVotes.First(a => a.Key == vote);
                    return (fullVote, count);
                }

                VoteLineBlock leastPreferredChoice;

                // If not, eliminate the least preferred option and try again.
                if (invertVotesToCheckPreferred)
                {
                    leastPreferredChoice = GetLeastPreferredChoice(voterPreferences);
                }
                else
                {
                    leastPreferredChoice = GetLeastPreferredChoice(workingVotes);
                }

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
        private (VoteLineBlock vote, int count) GetMostPreferredVote(Dictionary<Origin, List<VoteLineBlock>> voterPreferences)
        {
            var highestRankings = voterPreferences.GroupBy(v => v.Value.First());

            var mostPreferred = highestRankings.MaxObject(r => r.Count());

            return (mostPreferred.Key, mostPreferred.Count());
        }

        /// <summary>
        /// Get the least preferred choice to determine which vote to eliminate.
        /// Implemented by derived classes.
        /// </summary>
        /// <param name="voterPreferences">This version takes the voter preferences collection.</param>
        /// <returns>Returns the vote that is least preferred.</returns>
        protected virtual VoteLineBlock GetLeastPreferredChoice(Dictionary<Origin, List<VoteLineBlock>> voterPreferences)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the least preferred choice to determine which vote to eliminate.
        /// Implemented by derived classes.
        /// </summary>
        /// <param name="voterPreferences">This version takes the vote storage collection.</param>
        /// <returns>Returns the vote that is least preferred.</returns>
        protected virtual VoteLineBlock GetLeastPreferredChoice(VoteStorage votes)
        {
            throw new NotImplementedException();
        }
    }
}
