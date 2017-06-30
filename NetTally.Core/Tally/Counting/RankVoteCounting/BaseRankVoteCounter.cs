﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.Utility;
using NetTally.Votes;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting.RankVoteCounting
{
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
        public RankResultsByTask CountVotes(Dictionary<string, HashSet<string>> votes)
        {
            if (votes == null)
                throw new ArgumentNullException(nameof(votes));

            RankResultsByTask preferencesByTask = new RankResultsByTask();

            if (votes.Any())
            {
                // Handle each task separately
                var groupByTask = votes.GroupBy(vote => VoteString.GetVoteTask(vote.Key), Agnostic.StringComparer);

                foreach (GroupedVotesByTask task in groupByTask)
                {
                    if (task.Any())
                    {
                        preferencesByTask[task.Key] = RankTask(task);

                        Debug.WriteLine($"Ranking task [{task.Key}] via [{this.GetType().Name}]:");
                        foreach (var res in preferencesByTask[task.Key])
                        {
                            Debug.WriteLine($" - Option [{res.Option}] Debug [{res.Debug}]");
                        }
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
