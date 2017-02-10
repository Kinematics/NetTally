using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NetTally.Extensions;

namespace NetTally.Votes.Experiment
{
    // X marker can only contain other X
    // +/- marker can only contain other +/-
    // # rank marker can only contain X (entire block is given a single rank)
    // + score marker can contain X (copy parent score) or other + score

    /// <summary>
    /// Class that handles taking a source vote and breaking it into individual partitions.
    /// </summary>
    public static class VotingConstructor
    {
        internal static void ProcessPlan(Plan plan, IQuest quest, CancellationToken token)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            // Check for cancellation once per post processed.
            token.ThrowIfCancellationRequested();

            IEnumerable<VoteLine> lines;
            IEnumerable<VotePartition> parts;

            switch (quest.PartitionMode)
            {
                case PartitionMode.None:
                    parts = new List<VotePartition> { plan.Content };
                    VotingRecords.Instance.AddVoteEntries(parts, plan.Identity);
                    break;
                case PartitionMode.ByLine:
                    lines = plan.Content.VoteLines.Skip(1);
                    parts = lines.Select(a => new VotePartition(a, VoteType.Plan));
                    VotingRecords.Instance.AddVoteEntries(parts, plan.Identity);
                    break;
                case PartitionMode.ByLineTask:
                    lines = UpliftLines(plan.Content.VoteLines);
                    parts = lines.Select(a => new VotePartition(a, VoteType.Plan));
                    VotingRecords.Instance.AddVoteEntries(parts, plan.Identity);
                    break;
                case PartitionMode.ByBlock:
                    parts = new List<VotePartition> { plan.Content };
                    VotingRecords.Instance.AddVoteEntries(parts, plan.Identity);
                    break;
                case PartitionMode.ByBlockAll:
                    parts = UpliftBlocks(plan.Content.VoteLines, VoteType.Plan);
                    VotingRecords.Instance.AddVoteEntries(parts, plan.Identity);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown parition mode: {quest.PartitionMode}.");
            }

            plan.SetContentPartitions(parts);
        }

        /// <summary>
        /// Takes all lines except the first one.
        /// If the first one has a task, propagates that task to any other lines that do not already have a task.
        /// If all lines (other than the first) have a prefix, remove up to 1 prefix character from all lines.
        /// </summary>
        /// <param name="voteLines">The vote lines.</param>
        /// <returns>Returns the list of lines with the above modifications made.</returns>
        private static IEnumerable<VoteLine> UpliftLines(IReadOnlyList<VoteLine> voteLines)
        {
            var first = voteLines.First();
            var rest = voteLines.Skip(1);

            string task = first.Task;
            int minIndent = rest.Min(a => a.Prefix.Length);

            var result = rest.Select(a =>
                a.Modify(
                    task: (!string.IsNullOrEmpty(task) && string.IsNullOrEmpty(a.Task)) ?
                           task : null,
                    prefix: (minIndent > 0 && a.Prefix.Length > minIndent) ?
                           a.Prefix.Substring(minIndent) : null
                    )
                );

            return result;
        }

        private static IEnumerable<VotePartition> UpliftBlocks(IReadOnlyList<VoteLine> voteLines, VoteType voteType)
        {
            var first = voteLines.First();
            var rest = voteLines.Skip(1);

            string task = first.Task;
            int minIndent = rest.Min(a => a.Prefix.Length);

            var result = rest.Select(a =>
                a.Modify(
                    task: (!string.IsNullOrEmpty(task) && a.Prefix.Length == minIndent && string.IsNullOrEmpty(a.Task)) ?
                           task : null,
                    prefix: (minIndent > 0 && a.Prefix.Length > minIndent) ?
                           a.Prefix.Substring(minIndent) : null
                    )
                );

            var groupResult = result.GroupAdjacentByContinuation(a => a.CleanContent, Vote.VoteBlockContinues);

            var partResult = groupResult.Select(a => new VotePartition(a, voteType));

            return partResult;
        }

        /// <summary>
        /// Process a post to get the component votes (partitions) and store them
        /// in the voting records.
        /// </summary>
        /// <param name="post">The post being processed.</param>
        /// <param name="quest">The quest being processed.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns true if the post was successfully processed, or will not be processed.</returns>
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

            // If a newer vote has been registered for this user in the vote records,
            // that means that this post was a post with a future reference that got
            // overridden by another vote later in the thread, and we're reprocessing it now.
            // Only actually process it if this is not the case.
            if (!VotingRecords.Instance.HasNewerVote(post.Identity))
            {
                // Get the list of all vote partitions from the post, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<VotePartition> votePartitions = GetVotePartitions(post, quest.PartitionMode, VoteType.Vote);

                // Optional filtering of vote partitions, based on task.
                List<VotePartition> filteredPartitions = FilterVotesByTask(votePartitions, quest);

                // Add the results to the voting records.
                VotingRecords.Instance.AddVoteEntries(filteredPartitions, post.Identity);
            }

            return true;
        }

        /// <summary>
        /// Take a list of vote partitions, and filter out any that do not match the
        /// current quest's task filter.
        /// </summary>
        /// <param name="votePartitions">The partitions to filter.</param>
        /// <param name="quest">The quest being processed.</param>
        /// <returns>All partitions that matched the current filter, or all partitions if
        /// there is no active filter.</returns>
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
