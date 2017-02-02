﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NetTally.Votes.Experiment
{
    // X marker can only contain other X
    // +/- marker can only contain other +/-
    // # rank marker can only contain X (entire block is given a single rank)
    // + score marker can contain X (copy parent score) or other + score


    public static class VotingConstructor
    {
        internal static bool ProcessPost(Post post, IQuest quest, CancellationToken token)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (!post.HasVote)
                throw new ArgumentException("Post is not a valid vote.");

            // Check for cancellation once per post processed.
            token.ThrowIfCancellationRequested();

            // Skip processing votes with future references, unless the post
            // is marked to force processing.
            if (!post.ForceProcess && HasFutureReference(post))
            {
                VotingRecords.Instance.NoteFutureReference(post);
                return false;
            }

            // If a newer vote has been registered in the vote records, that means
            // that this post was a post with a future reference that got overridden
            // by another vote later in the thread, and we're reprocessing it now.
            // Only actually process it if this is not the case.
            if (!VotingRecords.Instance.HasNewerVote(post.Identity))
            {
                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<VotePartition> votePartitions = GetVotePartitions(post, quest.PartitionMode, VoteType.Vote);

                // Optional filtering of vote partitions, based on task.
                List<VotePartition> filteredPartitions = FilterVotesByTask(votePartitions, quest);

                // Add the results to the voting records.
                VotingRecords.Instance.AddVotes(filteredPartitions, post.Identity, VoteType.Vote);
            }

            return true;
        }

        private static List<VotePartition> FilterVotesByTask(List<VotePartition> votePartitions, IQuest quest)
        {
            if (!quest.UseCustomTaskFilters)
                return votePartitions;

            return votePartitions.Where(p => quest.TaskFilter.Match(p.Task)).ToList();
        }

        private static List<VotePartition> GetVotePartitions(Post post, PartitionMode partitionMode, VoteType vote)
        {
            throw new NotImplementedException();
        }

        private static bool HasFutureReference(Post post)
        {
            throw new NotImplementedException();
        }

        internal static List<string> GetWorkingVote(Post post)
        {
            throw new NotImplementedException();
        }

    }
}
