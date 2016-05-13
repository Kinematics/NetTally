using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

    public abstract class BaseRankVoteCounter : IRankVoteCounter
    {
        /// <summary>
        /// Counts the provided rank votes.
        /// </summary>
        /// <param name="votes">The rank votes to count.</param>
        /// <returns>Returns an ordered list of ranked votes for each task in the provided votes.</returns>
        /// <exception cref="System.ArgumentNullException">Provided votes cannot be null.</exception>
        public RankResultsByTask CountVotes(SupportedVotes votes)
        {
            if (votes == null)
                throw new ArgumentNullException(nameof(votes));

            RankResultsByTask preferencesByTask = new RankResultsByTask();

            if (votes.Any())
            {
                // Handle each task separately
                var groupByTask = from vote in votes
                                  group vote by VoteString.GetVoteTask(vote.Key) into g
                                  select g;

                foreach (GroupedVotesByTask task in groupByTask)
                {
                    if (task.Any())
                    {
                        Debug.WriteLine($"Rank Task [{task.Key}]");

                        preferencesByTask[task.Key] = RankTask(task);

                        Debug.WriteLine($"End task [{task.Key}]");
                    }
                }
            }

            return preferencesByTask;
        }

        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected abstract RankResults RankTask(GroupedVotesByTask task);

    }
}
