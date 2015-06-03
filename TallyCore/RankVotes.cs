using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetTally
{
    public static class RankVotes
    {
        /// <summary>
        /// Rank the ranked votes provided by the vote counter.
        /// </summary>
        /// <param name="voteCounter">Vote counter with a tallied collection of votes.</param>
        /// <returns>Returns a dictionary of task grouping to preferred vote for the
        /// top-rated choice.</returns>
        public static Dictionary<string, List<string>> Rank(IVoteCounter voteCounter)
        {
            if (voteCounter == null)
                throw new ArgumentNullException(nameof(voteCounter));

            if (voteCounter.HasRankedVotes == false)
                throw new InvalidOperationException("There are no votes to rank.");

            // Handle each task separately
            var groupByTask = from vote in voteCounter.RankedVotesWithSupporters
                              group vote by VoteLine.GetVoteTask(vote.Key) into g
                              select g;

            Dictionary<string, List<string>> taskPreference = new Dictionary<string, List<string>>();

            foreach (var task in groupByTask)
            {
                taskPreference[task.Key] = RankTask(task);
            }

            return taskPreference;
        }

        /// <summary>
        /// Select the top option out of the votes cast for a given task.
        /// </summary>
        /// <param name="task">Collection of votes designated for a particular task.</param>
        /// <returns>Returns the top voter choice for the task.</returns>
        private static List<string> RankTask(IGrouping<string, KeyValuePair<string, HashSet<string>>> task)
        {
            var voterChoices = ConvertVotesToVoters(task);

            List<string> topChoices = new List<string>(3);

            // 1st, 2nd and 3rd place results
            for (int i = 0; i < 3; i++)
            {
                var voterChoicesCopy = voterChoices.ToDictionary(a => a.Key, a => a.Value.ToList());

                string topChoice = GetTopRank(voterChoicesCopy);

                if (topChoice != string.Empty)
                    topChoices.Add(topChoice);
            
                RemoveChoice(topChoice, voterChoices);
            }

            return topChoices;
        }

        /// <summary>
        /// Select the top option out of the votes cast for a given task.
        /// </summary>
        /// <param name="task">Collection of votes designated for a particular task.</param>
        /// <returns>Returns the top voter choice for the task.</returns>
        private static string GetTopRank(Dictionary<string, List<string>> voterChoices)
        {
            if (voterChoices == null || voterChoices.Count == 0 || voterChoices.All(a => a.Value.Count == 0))
                return string.Empty;

            // Limit to 10 iterations, to ensure there are no infinite loops
            int loop = 0;

            while (true)
            {
                // First see if any options has a majority of #1 votes
                var firstChoices = CountFirstPlaceVotes(voterChoices);

                var topChoice = firstChoices.OrderByDescending(a => a.Value).First();

                if (IsMajority(topChoice.Value, voterChoices.Count) || OnlyOneChoiceLeft(voterChoices) || ++loop > 9)
                {
                    return topChoice.Key;
                }

                // If no option has an absolute majority, find the most-disliked option
                // and remove it from all voters' option lists, in preparation of another
                // round of checks.
                var lastChoices = CountLastPlaceVotes(voterChoices);

                var bottomChoice = lastChoices.OrderByDescending(a => a.Value).First();

                RemoveLastPlaceOption(bottomChoice, voterChoices);
            }
        }


        /// <summary>
        /// Convert the original grouping of voters per vote into a grouping of votes per voter,
        /// ordered by voter preference for each vote.
        /// </summary>
        /// <param name="task">All votes provided for a given task.</param>
        /// <returns>Returns a grouping of votes per user.</returns>
        private static Dictionary<string, List<string>> ConvertVotesToVoters(IGrouping<string, KeyValuePair<string, HashSet<string>>> task)
        {
            var ordered = task.OrderBy(a => VoteLine.GetVoteMarker(a.Key));

            Dictionary<string, List<string>> voters = new Dictionary<string, List<string>>();
            List<string> votes;

            foreach (var vote in ordered)
            {
                foreach (var voter in vote.Value)
                {
                    if (!voters.TryGetValue(voter, out votes))
                    {
                        votes = new List<string>();
                        voters[voter] = votes;
                    }

                    votes.Add(VoteLine.GetVoteContent(vote.Key));
                }
            }

            return voters;
        }

        /// <summary>
        /// Count up the first choice options for all voters, and return a tally.
        /// </summary>
        /// <param name="voterList">The list of all voters and their ranked choices.</param>
        /// <returns>Returns the number of votes for each of the first-ranked vote options.</returns>
        private static Dictionary<string, int> CountFirstPlaceVotes(Dictionary<string, List<string>> voterList)
        {
            Dictionary<string, int> voteCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int count = 0;

            foreach (var vote in voterList)
            {
                string firstVote = vote.Value.First();

                if (!voteCount.TryGetValue(firstVote, out count))
                {
                    count = 0;
                }

                voteCount[firstVote] = ++count;
            }

            return voteCount;
        }

        /// <summary>
        /// Count up the last choice options for all voters, and return a tally.
        /// </summary>
        /// <param name="voterList">The list of all voters and their ranked choices.</param>
        /// <returns>Returns the number of votes for each of the last-ranked vote options.</returns>
        private static Dictionary<string, int> CountLastPlaceVotes(Dictionary<string, List<string>> voterList)
        {
            Dictionary<string, int> voteCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int count = 0;

            foreach (var vote in voterList)
            {
                string lastVote = vote.Value.Last();

                if (!voteCount.TryGetValue(lastVote, out count))
                {
                    count = 0;
                }

                voteCount[lastVote] = ++count;
            }

            return voteCount;
        }

        /// <summary>
        /// Remove the specified vote option from all voters' option lists.
        /// May not remove the last vote option from a voter.
        /// </summary>
        /// <param name="bottomChoice">The last place option that's being removed.</param>
        /// <param name="voterList">The list of all voters.</param>
        private static void RemoveLastPlaceOption(KeyValuePair<string, int> bottomChoice, Dictionary<string, List<string>> voterList)
        {
            foreach (var voter in voterList)
            {
                if (voter.Value.Count > 1)
                {
                    voter.Value.Remove(bottomChoice.Key);
                }
            }
        }

        /// <summary>
        /// Remove an entry unconditionally from the list of voter choices.
        /// </summary>
        /// <param name="choice">The choice to remove.</param>
        /// <param name="voterChoices">The list of all voter ranked choices.</param>
        private static void RemoveChoice(string choice, Dictionary<string, List<string>> voterChoices)
        {
            foreach (var voter in voterChoices)
            {
                voter.Value.Remove(choice);
            }
        }

        /// <summary>
        /// Check to see if all voters only have one voting option left.
        /// </summary>
        /// <param name="voterList">List of all voters and their choices.</param>
        /// <returns>Returns true if all voters only have one choice left.</returns>
        private static bool OnlyOneChoiceLeft(Dictionary<string, List<string>> voterList)
        {
            if (voterList.All(a => a.Value.Count == 1))
                return true;

            return false;
        }

        /// <summary>
        /// Determine if the given number of voters qualifies as a majority out of
        /// all possible voters.
        /// </summary>
        /// <param name="voters">Number of voters being checked.</param>
        /// <param name="totalVoters">Total number of voters.</param>
        /// <returns>Returns true if the number of voters qualifies as a majority.</returns>
        private static bool IsMajority(int voters, int totalVoters)
        {
            return ((double)voters / (double)totalVoters) > 0.5;
        }

    }
}
