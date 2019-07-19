using System.Collections.Generic;
using System.Linq;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    /// <summary>
    /// Implement ranking votes using the standard instant runoff method.
    /// Each round, the least liked of the top-ranked choices is removed.
    /// </summary>
    public class InstantRunoff : IRankVoteCounter2
    {
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

                // If not, eliminate the least preferred option and try again.
                var leastPreferredChoice = GetLeastPreferredChoice(voterPreferences);

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
        /// In the standard Instant Runoff, this is the vote with the fewest
        /// number of top-ranked votes.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <returns>Returns the vote string for the least preferred vote.</returns>
        private VoteLineBlock GetLeastPreferredChoice(Dictionary<Origin, List<VoteLineBlock>> voterPreferences)
        {
            var highestRankings = voterPreferences.GroupBy(v => v.Value.First());

            var leastPreferred = highestRankings.MinObject(r => r.Count()).Key;

            return leastPreferred;

        }
    }
}
