using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        #region Plan processing        
        /// <summary>
        /// Processes the provided plan.
        /// </summary>
        /// <param name="plan">The plan. Cannot be null.</param>
        /// <param name="quest">The quest being tallied. Cannot be null.</param>
        /// <param name="token">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.InvalidOperationException"></exception>
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
                    break;
                case PartitionMode.ByLine:
                    lines = plan.Content.VoteLines.Skip(1);
                    parts = lines.Select(a => new VotePartition(a, VoteType.Plan));
                    break;
                case PartitionMode.ByLineTask:
                    lines = UpliftLines(plan.Content.VoteLines);
                    parts = lines.Select(a => new VotePartition(a, VoteType.Plan));
                    break;
                case PartitionMode.ByBlock:
                    parts = new List<VotePartition> { plan.Content };
                    break;
                case PartitionMode.ByBlockAll:
                    parts = UpliftBlocks(plan.Content.VoteLines, VoteType.Plan);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown parition mode: {quest.PartitionMode}.");
            }

            plan.SetContentPartitions(parts);
            VotingRecords.Instance.AddVoteEntries(parts, plan.Identity);
        }

        /// <summary>
        /// Takes all lines from a plan except the first one.
        /// If the first one has a task, propagates that task to any other lines that do not already have a task.
        /// If all lines (other than the first) have a prefix, remove min indent number of prefix characters from all lines.
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
                    prefix: (minIndent > 0) ?
                           a.Prefix.Substring(minIndent) : null
                    )
                );

            return result;
        }

        /// <summary>
        /// Uplifts the blocks inside of a plan.
        /// Partitions the plan by block, removing the first line.
        /// If the first/main line of the plan has a task, propagate that to the blocks below it that do not already have a task.
        /// If all lines (other than the first) have a prefix, remove the minimum number of previx characters from all lines.
        /// </summary>
        /// <param name="voteLines">The vote lines.</param>
        /// <param name="voteType">Type of the vote.</param>
        /// <returns></returns>
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
                    prefix: (minIndent > 0) ?
                           a.Prefix.Substring(minIndent) : null
                    )
                );

            var groupResult = result.GroupAdjacentToPreviousSource(a => a.ComparableContent, Vote.VoteBlockContinues);

            var partResult = groupResult.Select(a => new VotePartition(a, voteType));

            return partResult;
        }
        #endregion

        #region Post processing        
        /// <summary>
        /// Processes the posts currently stored in the <see cref="VotingRecords"/>.
        /// </summary>
        /// <param name="quest">The quest being tallied. Cannot be null.</param>
        /// <param name="token">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">quest</exception>
        public static void ProcessPosts(IQuest quest, CancellationToken token)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var unprocessed = VotingRecords.Instance.PostsList;
            List<Post> processed = new List<Post>();

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                processed.Clear();

                foreach (var post in unprocessed)
                {
                    // Check for cancellation once per post processed.
                    token.ThrowIfCancellationRequested();

                    // Get the list of all vote partitions from the post, built according to current preferences.
                    // One of: By line, By block, or By post (ie: entire vote)
                    List<VotePartition> votePartitions = GetVotePartitions(post, quest);

                    if (votePartitions != null)
                    {
                        // Optional filtering of vote partitions, based on task.
                        List<VotePartition> filteredPartitions = FilterVotesByTask(votePartitions, quest);

                        post.Prepare(filteredPartitions);

                        // Add the results to the voting records.
                        VotingRecords.Instance.AddVoteEntries(filteredPartitions, post.Identity);

                        processed.Add(post);
                    }
                }

                // As long as some got processed, remove those from the unprocessed list
                // and let the loop run again.
                if (processed.Any())
                {
                    unprocessed = unprocessed.Except(processed).ToList();
                }
                else
                {
                    // If none got processed (and there must be at least some waiting on processing),
                    // set the ForceProcess flag on them to avoid pending FutureReference waits.
                    foreach (var p in unprocessed)
                    {
                        p.ForceProcess = true;
                    }
                }
            }
        }

        static readonly Regex startsWithPlan = new Regex(@"^(base ?)?(plan[ :]*)(?<planname>.+)");
        static readonly Regex proxyRegex = new Regex(@"^(?<pin>\^\s*)?(?<username>.{1,40}$)");

        /// <summary>
        /// Take the initial value of a post, and substitute plans and
        /// proxy vote lines as appropriate.
        /// </summary>
        /// <param name="post">The original post.</param>
        /// <returns>Returns a list of VoteLines to use for the purpose of processing the vote.</returns>
        public static List<VotePartition> GetVotePartitions(Post post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            List<VotePartition> results = new List<VotePartition>();
            VotePartition partition = null;
            VoteLine parent = null;

            // Move through the vote lines by index so we can modify the
            // index pointer directly when doing comparisons.
            var voteLineArray = post.Vote.VoteLines.ToArray();
            int index = 0;

            while (index < voteLineArray.Length)
            {
                // Current line.
                VoteLine line = voteLineArray[index];

                // First check if the line is a proxy vote for a plan or a user
                Match m = startsWithPlan.Match(line.ComparableContent);

                // "Plan XYZ"
                if (m.Success)
                {
                    string planname = m.Groups["planname"].Value;

                    // Plan has priority, in case the plan is named after the user.
                    if (VotingRecords.Instance.HasPlanName(planname))
                    {
                        // Get the collection of variants of this plan name.
                        var plans = VotingRecords.Instance.GetPlans(planname);
                        Plan plan = null;

                        // Make sure we don't go over the limit of the remaining post vote lines.
                        int remainingLines = voteLineArray.Length - index - 1;

                        if (remainingLines == 0)
                        {
                            // If there are no remaining lines in the current post, we
                            // just take the provided variant 0.
                            plan = plans.First(p => p.Identity.Number == 0);
                        }
                        else
                        {
                            // OK, there are lines that may potentially be copied from the plan.
                            // Limit the plans we're looking at to those that can fit in our remaining line space.
                            var limitedPlans = plans.Where(p => p.Content.VoteLines.Count <= remainingLines).ToList();

                            if (limitedPlans.Any())
                            {
                                foreach (var p in limitedPlans)
                                {
                                    // In each of the plans that can fit in the remaining vote space,
                                    // compare them with an equal-sized segment of the vote.
                                    // If it matches, that's a copy of the plan, and we can skip past it.
                                    var segment = new ArraySegment<VoteLine>(voteLineArray, index, p.Content.VoteLines.Count);

                                    if (segment.SequenceEqual(p.Content.VoteLines))
                                    {
                                        plan = p;
                                        break;
                                    }
                                }

                                // However if no sequence match was found, we only have the naming line to
                                // copy, and we can move on to the next line of the post.
                                if (plan == null)
                                {
                                    plan = limitedPlans.First(p => p.Identity.Number == 0);
                                }
                                else
                                {
                                    // If we did find the full match, update the index so that we increment past that.
                                    index += plan.Content.VoteLines.Count - 1;
                                }
                            }
                            else
                            {
                                // None of the selected plans can fit in the remaining lines of the post.
                                // Thus we don't need to try to match, and just take the named line.
                                plan = plans.First(p => p.Identity.Number == 0);
                            }
                        }

                        // If we found a plan, add its contents to the results and go to move next.
                        // Otherwise, drop out and treat this as a normal line.
                        if (plan != null)
                        {
                            List<VotePartition> planPartitions = VotingRecords.Instance.GetPartitionsForIdentity(plan.Identity);

                            results.AddRange(planPartitions);

                            goto moveNext;
                        }
                    }
                    // Otherwise check if it's a user proxy.  Usernames are unlikely to be allowed to be greater
                    // than 20 characters, but giving a fair buffer just in case.  Don't check for very long strings
                    // as usernames.  Also check global options in case proxy votes are disabled.
                    else if (planname.Length <= 40 && !AdvancedOptions.Instance.DisableProxyVotes)
                    {
                        List<VotePartition> proxyPartitions = GetProxyPartitions(planname, false, post.Identity);

                        // There are no matching identities.  Treat as a normal line.
                        if (proxyPartitions == null)
                            goto normalLine;

                        // If there are no recorded partitions for this identity, it hasn't been
                        // processed yet, and this is a future reference, or a past vote that itself
                        // has future references and hasn't completed processing yet.
                        if (!proxyPartitions.Any())
                        {
                            // If we aren't forcing the processing of this post, just bail and return null.
                            // If we *are* forcing it, we have to treat this as a normal line.
                            if (post.ForceProcess)
                            {
                                goto normalLine;
                            }
                            else
                            {
                                VotingRecords.Instance.NoteFutureReference(post);
                                return null;
                            }
                        }

                        // Add whatever proxy partitions we got to he results and move on.
                        results.AddRange(proxyPartitions);

                        goto moveNext;
                    }
                }
                // If it doesn't start with 'plan', also check if it's a (possibly pinned) username proxy.
                // Usernames are unlikely to be allowed to be greater than 20 characters, but giving
                // a fair buffer just in case.  Don't check for very long strings as usernames.
                // Also check global options in case proxy votes are disabled.
                else
                {
                    // Proxy regex might start with ^ to indicate a pinned name, and have a
                    // username length of no more than 40 characters.
                    m = proxyRegex.Match(line.ComparableContent);

                    if (m.Success && !AdvancedOptions.Instance.DisableProxyVotes)
                    {
                        bool pin = m.Groups["pin"].Success;
                        string username = m.Groups["username"].Value;

                        List<VotePartition> proxyPartitions = GetProxyPartitions(username, pin, post.Identity);

                        // There are no matching identities.  Treat as a normal line.
                        if (proxyPartitions == null)
                            goto normalLine;

                        // If there are no recorded partitions for this identity, it hasn't been
                        // processed yet, and this is a future reference, or a past vote that itself
                        // has future references and hasn't completed processing yet.
                        if (!proxyPartitions.Any())
                        {
                            // If we aren't forcing the processing of this post, just bail and return null.
                            // If we *are* forcing it, we have to treat this as a normal line.
                            if (post.ForceProcess)
                            {
                                goto normalLine;
                            }
                            else
                            {
                                VotingRecords.Instance.NoteFutureReference(post);
                                return null;
                            }
                        }

                        // Add whatever proxy partitions we got to he results and move on.
                        results.AddRange(proxyPartitions);

                        goto moveNext;
                    }
                    // If we have a plan name that doesn't have the 'plan' prefix, redo the logic
                    // of skipping past any copied content.
                    else if (VotingRecords.Instance.HasPlanName(line.ComparableContent))
                    {
                        string planname = line.ComparableContent;

                        // Get the collection of variants of this plan name.
                        var plans = VotingRecords.Instance.GetPlans(planname);
                        Plan plan = null;

                        // Make sure we don't go over the limit of the remaining post vote lines.
                        int remainingLines = voteLineArray.Length - index - 1;

                        if (remainingLines == 0)
                        {
                            // If there are no remaining lines in the current post, we
                            // just take the provided variant 0.
                            plan = plans.First(p => p.Identity.Number == 0);
                        }
                        else
                        {
                            // OK, there are lines that may potentially be copied from the plan.
                            // Limit the plans we're looking at to those that can fit in our remaining line space.
                            var limitedPlans = plans.Where(p => p.Content.VoteLines.Count <= remainingLines).ToList();

                            if (limitedPlans.Any())
                            {
                                foreach (var p in limitedPlans)
                                {
                                    // In each of the plans that can fit in the remaining vote space,
                                    // compare them with an equal-sized segment of the vote.
                                    // If it matches, that's a copy of the plan, and we can skip past it.
                                    var segment = new ArraySegment<VoteLine>(voteLineArray, index, p.Content.VoteLines.Count);

                                    if (segment.SequenceEqual(p.Content.VoteLines))
                                    {
                                        plan = p;
                                        break;
                                    }
                                }

                                // However if no sequence match was found, we only have the naming line to
                                // copy, and we can move on to the next line of the post.
                                if (plan == null)
                                {
                                    plan = limitedPlans.First(p => p.Identity.Number == 0);
                                }
                                else
                                {
                                    // If we did find the full match, update the index so that we increment past that.
                                    index += plan.Content.VoteLines.Count - 1;
                                }
                            }
                            else
                            {
                                // None of the selected plans can fit in the remaining lines of the post.
                                // Thus we don't need to try to match, and just take the named line.
                                plan = plans.First(p => p.Identity.Number == 0);
                            }
                        }

                        // If we found a plan, add its contents to the results and go to move next.
                        // Otherwise, drop out and treat this as a normal line.
                        if (plan != null)
                        {
                            List<VotePartition> planPartitions = VotingRecords.Instance.GetPartitionsForIdentity(plan.Identity);

                            results.AddRange(planPartitions);

                            goto moveNext;
                        }
                    }
                }


                // Not a plan proxy, and not a user proxy. Handle normal partitioning.
                normalLine:

                if (partition == null)
                    partition = new VotePartition();
                if (parent == null)
                    parent = line;

                switch (quest.PartitionMode)
                {
                    case PartitionMode.None:
                        partition.AddLine(line);
                        break;
                    case PartitionMode.ByLine:
                        if (partition.VoteLines.Count > 0)
                            results.Add(partition);
                        partition = new VotePartition(line, VoteType.Vote);
                        break;
                    case PartitionMode.ByLineTask:
                        if (partition.VoteLines.Count > 0)
                            results.Add(partition);

                        if (line.Prefix.Length == 0)
                        {
                            parent = line;
                            partition = new VotePartition(line, VoteType.Vote);
                        }
                        else if (parent.Task != string.Empty && line.Task == string.Empty)
                        {
                            var mLine = line.Modify(task: parent.Task);
                            partition = new VotePartition(mLine, VoteType.Vote);
                        }
                        else
                        {
                            partition = new VotePartition(line, VoteType.Vote);
                        }
                        break;
                    case PartitionMode.ByBlock:
                    case PartitionMode.ByBlockAll:
                        if (line.Prefix.Length == 0)
                        {
                            if (partition.VoteLines.Count > 0)
                                results.Add(partition);
                            partition = new VotePartition(line, VoteType.Vote);
                        }
                        else
                        {
                            partition.AddLine(line);
                        }
                        break;
                    default:
                        break;
                }

                // Exit point for next enumerator line.
                moveNext:

                index++;
            }

            if (partition != null && partition.VoteLines.Count > 0)
                results.Add(partition);

            return results;
        }

        /// <summary>
        /// Gets the partitions voted for by the specified proxy name.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="pin">if set to <c>true</c> [pin].</param>
        /// <param name="postIdentity">The post identity.</param>
        /// <returns>Returns the partitions voted for by the user, or null if none.</returns>
        private static List<VotePartition> GetProxyPartitions(string username, bool pin, Identity postIdentity)
        {
            var identities = VotingRecords.Instance.GetVoterIdentities(username);

            if (identities != null)
            {
                IEnumerable<Identity> searchIdentities;

                if (pin)
                {
                    searchIdentities = identities.Where(i => i.PostIDValue < postIdentity.PostIDValue);
                }
                else
                {
                    searchIdentities = identities;
                }

                // If there are any appropriate identities, we can continue.
                if (searchIdentities.Any())
                {
                    // Get the highest post made by the referenced person.
                    // If pinned, this will be the highest post made by the person prior to the current post.
                    var proxy = searchIdentities.OrderBy(i => i.PostIDValue).Last();

                    var proxyPartitions = VotingRecords.Instance.GetPartitionsForIdentity(proxy);

                    return proxyPartitions;
                }
            }

            return null;
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
        #endregion

    }
}
