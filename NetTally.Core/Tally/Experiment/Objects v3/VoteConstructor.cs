using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.Options;
using NetTally.Utility;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    /// <summary>
    /// Class that can handle constructing votes from the base text of a post.
    /// </summary>
    public class VoteConstructor
    {
        readonly IGeneralInputOptions inputOptions;
        readonly IVoteCounter voteCounter;

        public VoteConstructor(IVoteCounter voteCounter, IGeneralInputOptions options)
        {
            this.voteCounter = voteCounter;
            inputOptions = options;
        }

        #region Public functions
        /// <summary>
        /// Get plans from the provided post during the preprocessing phase.
        /// It takes a parameter for the function that will be used to analyze each
        /// block of the post (or the entire post, if asBlocks is false).
        /// </summary>
        /// <param name="post">The post to examine.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="asBlocks">Whether to break up the post's vote lines into blocks.</param>
        /// <param name="isPlanFunction">The function to run on the vote blocks.</param>
        /// <returns>Returns all blocks of vote lines that are considered to be part of a plan. Includes the plan name.</returns>
        public Dictionary<string, VoteLineBlock> PreprocessPostGetPlans(Post post, IQuest quest,
            bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, string planName)> isPlanFunction)
        {
            Dictionary<string, VoteLineBlock> plans = new Dictionary<string, VoteLineBlock>();

            // Either split the vote into blocks, or encapsulate the vote into an enumerable
            // so that it can be treated the same way.
            var blocks = asBlocks ? VoteBlocks.GetBlocks(post.VoteLines) : new List<VoteLineBlock>() { new VoteLineBlock(post.VoteLines) };

            foreach (var block in blocks)
            {
                var (isPlan, planName) = isPlanFunction(block);

                if (isPlan && IsValidPlanName(planName, post.Author) && IsTaskAllowed(block, quest))
                {
                    plans[planName] = block;
                }
            }

            return plans;
        }

        /// <summary>
        /// Get votes from the provided post during the processing phase.
        /// </summary>
        /// <param name="post">The post being processed.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns a list of all vote partitions from this post.
        /// May return null if nothing was processed.</returns>
        public List<VoteLineBlock>? ProcessPostGetVotes(Post post, IQuest quest)
        {
            // If the vote has content, process it. Base plans have already been
            // extracted from the Working Vote lines.
            if (post.WorkingVoteLines.Count > 0)
            {
                // If it has a reference to a voter that has not been processed yet,
                // delay processing.
                if (HasFutureReference(post, quest))
                {
                    voteCounter.AddFutureReference(post);
                    return null;
                }

                // If a newer vote has been registered in the vote counter, that means
                // that this post was a prior future reference that got overridden later.
                // If so, don't process it now, but allow the post to be marked as
                // processed so that it doesn't try to re-submit it later.
                if (!voteCounter.HasNewerVote(post))
                {
                    // Get the results of partitioning the post.
                    var results = PartitionPost(post, quest.PartitionMode);

                    // Apply task filtering.
                    var filteredResults = results.Where(p => IsTaskAllowed(p, quest)).ToList();

                    post.Processed = true;
                    return filteredResults;
                }
            }

            post.Processed = true;
            return null;
        }
        #endregion

        #region Utility functions for processing votes.
        /// <summary>
        /// Make sure the provided plan name is valid.
        /// A named vote that is named after a user is only valid if it matches the post author's name.
        /// </summary>
        /// <param name="planName">The name of the plan.</param>
        /// <param name="postAuthor">The post's author.</param>
        /// <returns>Returns true if the plan name is deemed valid.</returns>
        private bool IsValidPlanName(string planName, string postAuthor)
        {
            // A named vote that is named after a user is only valid if it matches the post author's name.
            if (voteCounter.HasReferenceVoter(planName))
            {
                if (!Agnostic.StringComparer.Equals(planName, postAuthor))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determine whether the task of the provided vote line block falls within the
        /// filter range of allowed/desired tasks.
        /// If task filters are active, the block must have a task that matches what's allowed.
        /// </summary>
        /// <param name="block">The block of vote lines to check. The first line determines the task.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns true if the block of vote lines is allowed to be tallied.</returns>
        private bool IsTaskAllowed(VoteLineBlock block, IQuest quest)
        {
            // Always allow if no filters are active.
            if (!quest.UseCustomTaskFilters)
                return true;

            if (string.IsNullOrEmpty(block.Task))
                return false;

            return quest.TaskFilter?.Match(block.Task) ?? false;
        }

        /// <summary>
        /// Determine if there are any references to future (unprocessed) user votes
        /// within the current vote.
        /// </summary>
        /// <param name="post">Post containing the current vote.</param>
        /// <returns>Returns true if a future reference is found. Otherwise false.</returns>
        private bool HasFutureReference(Post post, IQuest quest)
        {
            // If we decide it has to be forced, ignore all checks in here.
            if (post.ForceProcess)
                return false;

            // If proxy votes are disabled, we don't need to look for proxy names, so there can't be future references.
            // Likewise, if we're forcing all proxy votes to be pinned, there can't be any future references.
            if (quest.DisableProxyVotes || quest.ForcePinnedProxyVotes)
                return false;

            foreach (var line in post.WorkingVoteLines)
            {
                // Get the possible proxy references this line contains
                var refNames = VoteString.GetVoteReferenceNames(line.Content);

                // Pinned references (^ or ↑ symbols) are explicitly not future references
                if (refNames[ReferenceType.Label].Any(a => a == "^" || a == "↑"))
                    continue;

                // Any references to plans automatically work, as they are defined in a preprocess phase.
                if (refNames[ReferenceType.Plan].Any(voteCounter.HasPlan))
                    continue;

                string? refVoter = voteCounter.GetReferenceVoterName(refNames[ReferenceType.Voter].FirstOrDefault());

                if (refVoter != null && refVoter != post.Author)
                {
                    var refVoterPosts = voteCounter.PostsList.Where(p => p.Author == refVoter);

                    // If the referenced voter never made a real vote (eg: only made base plans or rank votes),
                    // then this can't be treated as a future reference.
                    var refWorkingVotes = refVoterPosts.Where(p => p.WorkingVoteLines.Count > 0);

                    if (!refWorkingVotes.Any())
                        continue;
                    
                    // If there's no 'plan' label, then we need to verify that the last vote that the
                    // ref voter made (has a working vote) was processed.
                    // If it's been processed, then we're OK to let this vote through.
                    if (refWorkingVotes.Last().Processed)
                        continue;

                    // If none of the conditions above are met, then consider this a future reference.
                    return true;
                }
            }

            // No future references were found.
            return false;
        }

        /// <summary>
        /// Given a plan block, if the plan is a base plan, rename it as just a "Plan".
        /// </summary>
        /// <param name="plan">The plan to examine.</param>
        /// <returns>Returns the original plan, or the modified plan if it used "Base Plan".</returns>
        public KeyValuePair<string, VoteLineBlock> NormalizePlan(KeyValuePair<string, VoteLineBlock> plan)
        {
            VoteLine firstLine = plan.Value.First();

            var (planType, planName) = VoteBlocks.CheckIfPlan(firstLine);

            if (planType == VoteBlocks.LineStatus.BasePlan)
            {
                string content = $"Plan: {planName}";
                VoteLine revisedFirstLine = new VoteLine(firstLine.Prefix, firstLine.Marker, firstLine.Task, content, firstLine.MarkerType, firstLine.MarkerValue);

                List<VoteLine> voteLines = new List<VoteLine>() { revisedFirstLine };
                voteLines.AddRange(plan.Value.Skip(1));

                return new KeyValuePair<string, VoteLineBlock>(plan.Key, new VoteLineBlock(voteLines));
            }

            return plan;
        }
        #endregion

        #region Partitioning
        /// <summary>
        /// Partition a plan after initial preprocessing.
        /// </summary>
        /// <param name="block">The block defining the plan.</param>
        /// <param name="partitionMode">The current partitioning mode.</param>
        /// <returns>Returns a collection of VoteLineBlocks, extracted from the plan.</returns>
        public List<VoteLineBlock> PartitionPlan(VoteLineBlock block, PartitionMode partitionMode)
        {
            List<VoteLineBlock> partitions = new List<VoteLineBlock>();

            // If we're not partitioning, we have no work to do.
            if (partitionMode == PartitionMode.None)
            {
                partitions.Add(block);
                return partitions;
            }

            // Single line plans don't need extra handling.
            if (block.Lines.Count == 1)
            {
                partitions.Add(block);
                return partitions;
            }

            // Implicit plans carry some assumptions that don't require additional work.
            if (VoteBlocks.IsBlockAnImplicitPlan(block).isPlan)
            {
                // ByLine only needs to skip the first line, and take the rest as-is.
                if (partitionMode == PartitionMode.ByLine || partitionMode == PartitionMode.ByLineTask)
                {
                    foreach (var line in block.Skip(1))
                    {
                        partitions.Add(new VoteLineBlock(line));
                    }

                    return partitions;
                }
                else if (partitionMode == PartitionMode.ByBlock || partitionMode == PartitionMode.ByBlockAll)
                {
                    return VoteBlocks.GetBlocks(block.Skip(1)).ToList();
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Unknown partition mode: {partitionMode}", nameof(partitionMode));
                }
            }
            else
            {
                // Everything else is an Explicit plan.

                // ByLine only needs to skip the first line, and take the rest after promoting one indent level.
                if (partitionMode == PartitionMode.ByLine || partitionMode == PartitionMode.ByLineTask)
                {
                    foreach (var line in block.Skip(1))
                    {
                        var pLine = line.GetPromotedLine();
                        partitions.Add(new VoteLineBlock(pLine));
                    }

                    return partitions;
                }
                else if (partitionMode == PartitionMode.ByBlock)
                {
                    // Explicit plans are themselves blocks, so don't need modification.
                    partitions.Add(block);
                    return partitions;
                }
                else if (partitionMode == PartitionMode.ByBlockAll)
                {
                    // Visual Studio crashes whenever I try to use the Min() LINQ function.

                    int minDepth = int.MaxValue;
                    foreach (var line in block.Skip(1))
                    {
                        if (line.Depth < minDepth)
                            minDepth = line.Depth;
                    }

                    // Maybe: Apply main line task to sub blocks.

                    return VoteBlocks.GetBlocks(block.Skip(1).Select(a => a.GetPromotedLine(minDepth))).ToList();
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Unknown partition mode: {partitionMode}", nameof(partitionMode));
                }
            }
        }

        /// <summary>
        /// Partition a post based on the requested partition mode.
        /// </summary>
        /// <param name="post">The post whose vote is being partitioned.</param>
        /// <param name="partitionMode">The partition mode to use.</param>
        /// <returns>Returns the partitions that are to be counted.</returns>
        private List<VoteLineBlock> PartitionPost(Post post, PartitionMode partitionMode)
        {
            List<VoteLineBlock> results = partitionMode switch
            {
                PartitionMode.None => PartitionPostByNone(post),
                PartitionMode.ByLine => PartitionPostByLine(post),
                PartitionMode.ByLineTask => PartitionPostByLineTask(post),
                PartitionMode.ByBlock => PartitionPostByBlock(post),
                PartitionMode.ByBlockAll => PartitionPostByBlock(post),
                _ => throw new InvalidOperationException($"Unknown partition mode: {partitionMode}")
            };

            return results;
        }

        /// <summary>
        /// Generate the vote partitions for a post.
        /// There is no partitioning, so all this does is pull in proxy references.
        /// </summary>
        /// <param name="post">The post whose vote is being partitioned.</param>
        /// <returns>Returns the partitions that are to be counted.</returns>
        private List<VoteLineBlock> PartitionPostByNone(Post post)
        {
            List<VoteLine> working = new List<VoteLine>();

            foreach (var line in post.WorkingVoteLines)
            {
                if (line.Depth == 0 && IsProxyReference(line, post.Author, out string proxyName, out VoteType voteType, out bool pinned))
                {
                    var blocks = GetProxyBlocks(proxyName, voteType, pinned, post.IDValue);

                    foreach (var block in blocks)
                    {
                        working.AddRange(block);
                    }
                }
                else
                {
                    working.Add(line);
                }
            }

            VoteLineBlock workingBlock = new VoteLineBlock(working);

            return new List<VoteLineBlock>() { workingBlock };
        }

        /// <summary>
        /// Generate the vote partitions for a post, using line-level partitioning.
        /// Incorporates any proxy references.
        /// </summary>
        /// <param name="post">The post with the vote to be partitioned.</param>
        /// <returns>Returns a list of vote blocks.</returns>
        private List<VoteLineBlock> PartitionPostByLine(Post post)
        {
            List<VoteLineBlock> working = new List<VoteLineBlock>();

            foreach (var line in post.WorkingVoteLines)
            {
                if (line.Depth == 0 && IsProxyReference(line, post.Author, out string proxyName, out VoteType voteType, out bool pinned))
                {
                    working.AddRange(GetProxyBlocks(proxyName, voteType, pinned, post.IDValue));
                }
                else
                {
                    working.Add(new VoteLineBlock(line));
                }
            }

            return working;
        }

        /// <summary>
        /// Generate the vote partitions for a post, using line-level partitioning.
        /// Incorporates any proxy references.
        /// This cascades tasks from higher level lines to lower level ones when partitioning.
        /// </summary>
        /// <param name="post">The post with the vote to be partitioned.</param>
        /// <returns>Returns a list of vote blocks.</returns>
        private List<VoteLineBlock> PartitionPostByLineTask(Post post)
        {
            List<VoteLineBlock> working = new List<VoteLineBlock>();

            (int depth, string task) currentTask = (0, "");
            Stack<(int depth, string task)> taskStack = new Stack<(int depth, string task)>();

            foreach (var line in post.WorkingVoteLines)
            {
                if (line.Depth == 0 && IsProxyReference(line, post.Author, out string proxyName, out VoteType voteType, out bool pinned))
                {
                    working.AddRange(GetProxyBlocks(proxyName, voteType, pinned, post.IDValue));
                }
                else
                {
                    // If we have no task, and the line has no task, do nothing.
                    if (line.Task.Length == 0 && currentTask.task.Length == 0)
                    {
                        working.Add(new VoteLineBlock(line));
                        continue;
                    }

                    // If we've moved up a depth level, then make sure to pop off the stack until
                    // our current task is appropriate to the line level.
                    // Once we've done that, we'll be back to checking if the line is equal or greater
                    // depth than the current task.
                    if (line.Depth < currentTask.depth)
                    {
                        while (currentTask.depth > line.Depth && taskStack.Count > 0)
                        {
                            currentTask = taskStack.Pop();
                        }
                    }

                    // If we move to a new line that's of the same depth as our current task, update the task.
                    if (line.Depth == currentTask.depth)
                    {
                        currentTask.task = line.Task;
                        working.Add(new VoteLineBlock(line));
                        continue;
                    }

                    // If we move to a greater depth...
                    if (line.Depth > currentTask.depth)
                    {
                        // If the new line has no task, just propogate the current task and move on.
                        if (line.Task.Length == 0)
                        {
                            working.Add(new VoteLineBlock(line.WithTask(currentTask.task)));
                            continue;
                        }
                        // Otherwise save the current task on the stack and update to the new task.
                        else
                        {
                            taskStack.Push(currentTask);
                            currentTask = (line.Depth, line.Task);
                            working.Add(new VoteLineBlock(line));
                            continue;
                        }
                    }

                    // We should never get here, but if we do, just save the line.
                    working.Add(new VoteLineBlock(line));
                }
            }

            return working;
        }

        /// <summary>
        /// Generate the vote partitions for a post, using block-level partitioning.
        /// Incorporates any proxy references.
        /// </summary>
        /// <param name="post">The post with the vote to be partitioned.</param>
        /// <returns>Returns a list of vote blocks.</returns>
        private List<VoteLineBlock> PartitionPostByBlock(Post post)
        {
            List<VoteLineBlock> working = new List<VoteLineBlock>();

            var blocks = VoteBlocks.GetBlocks(post.WorkingVoteLines);

            foreach (var block in blocks)
            {
                VoteLine top = block.Lines.First();

                if (top.Depth == 0 && IsProxyReference(top, post.Author, out string proxyName, out VoteType voteType, out bool pinned))
                {
                    working.AddRange(GetProxyBlocks(proxyName, voteType, pinned, post.IDValue));
                }
                else
                {
                    working.Add(block);
                }
            }

            return working;
        }

        /// <summary>
        /// Examines a vote line to determine whether it's a proxy reference.
        /// If so, it returns information about the proxy, including 
        /// </summary>
        /// <param name="voteLine">The vote line to check for proxy references.</param>
        /// <param name="postAuthor">The author of the post.</param>
        /// <param name="proxyName">The determined name of the proxy reference.</param>
        /// <param name="voteType">The determined type of proxy reference.</param>
        /// <param name="pinned">Whether the proxy reference was pinned.</param>
        /// <returns>Returns true if it found a valid proxy reference.</returns>
        private bool IsProxyReference(VoteLine voteLine, string postAuthor, out string proxyName, out VoteType voteType, out bool pinned)
        {
            // Could be a plan, with "Plan X" in the vote line
            // Could be a reference to a user, with or without a "Plan".
            // Could be a pinned user name, like ^Kinematics or ↑Kinematics.

            var refNames = VoteString.GetVoteReferenceNamesFromContent(voteLine.CleanContent);

            if (refNames[ReferenceType.Label].Any(a => a.Contains("plan")))
            {
                // If it's labeled as a 'plan', check plans first, then users.

                string? planName = refNames[ReferenceType.Plan].FirstOrDefault();

                if (planName != null)
                {
                    string? properPlanName = voteCounter.GetPlanName(planName);

                    if (properPlanName != null)
                    {
                        proxyName = properPlanName;
                        voteType = VoteType.Plan;
                        pinned = false;
                        return true;
                    }
                    else if (!voteCounter.Quest!.DisableProxyVotes)
                    {
                        string? voterName = refNames[ReferenceType.Voter].FirstOrDefault();

                        if (voterName != null)
                        {
                            string? properVoterName = voteCounter.GetReferenceVoterName(voterName);

                            // Make sure we have a proper voter name, but don't allow self-references.
                            // Some user names conflict with normal vote lines.
                            if (properVoterName != null && properVoterName != postAuthor)
                            {
                                proxyName = properVoterName;
                                voteType = VoteType.Vote;
                                pinned = false;
                                return true;
                            }
                        }
                    }
                }

            }
            else
            {
                // If it's not labeled as a 'plan', then it's either a pin or an unlabeled reference.
                // Either way, check for users first.

                string? voterName = refNames[ReferenceType.Voter].FirstOrDefault();

                // See if any voter names match the line (as long as we're allowing proxy votes).  If so, use that.
                if (!voteCounter.Quest!.DisableProxyVotes && voteCounter.HasReferenceVoter(voterName))
                {
                    string? properVoterName = voteCounter.GetReferenceVoterName(voterName);

                    // Make sure we have a proper voter name, but don't allow self-references.
                    // Some user names conflict with normal vote lines.
                    if (properVoterName != null && properVoterName != postAuthor)
                    {
                        proxyName = properVoterName;
                        voteType = VoteType.Vote;

                        if (refNames[ReferenceType.Label].Contains("^") || refNames[ReferenceType.Label].Contains("↑"))
                            pinned = true;
                        else
                            pinned = false;

                        return true;
                    }
                }
                // If a user isn't found, only check for regular plans if there's not a non-plan label being used,
                // but not if the quest requires plan vote lines to be labeled.
                else if (!voteCounter.Quest!.ForcePlanReferencesToBeLabeled && !refNames[ReferenceType.Label].Any())
                {
                    string? planName = refNames[ReferenceType.Plan].FirstOrDefault();

                    if (planName != null)
                    {
                        string? properPlanName = voteCounter.GetPlanName(planName);

                        if (properPlanName != null)
                        {
                            proxyName = properPlanName;
                            voteType = VoteType.Plan;
                            pinned = false;
                            return true;
                        }

                    }
                }
            }

            // Nothing was found.
            proxyName = "";
            voteType = VoteType.Vote;
            pinned = false;
            return false;
        }

        /// <summary>
        /// Gets the vote blocks associated with a provided proxy name.
        /// </summary>
        /// <param name="proxyName">The name of the proxy.</param>
        /// <param name="voteType">The type of vote being requested.</param>
        /// <param name="pinned">Whether the proxy vote was pinned.</param>
        /// <param name="postId">The post ID, to provide a reference for a pinned proxy vote.</param>
        /// <returns>Returns a list of vote blocks associated with the proxy.</returns>
        private List<VoteLineBlock> GetProxyBlocks(string proxyName, VoteType voteType, bool pinned, int postId)
        {
            var currentVoteBlocks = voteCounter.GetVotesBy(proxyName);
            return currentVoteBlocks;
        }
        #endregion



        #region Partitioning handling - Obsolete
        /// <summary>
        /// Allows partitioning a provided vote (already broken into a list of lines) using the
        /// specified partition mode.
        /// </summary>
        /// <param name="voteLines">The vote lines.</param>
        /// <param name="quest">The quest, for filter parameters.</param>
        /// <param name="partitionMode">The partition mode to use.</param>
        /// <returns>Returns the partitioned vote as a list of strings.</returns>
        public List<string> PartitionVoteStrings(List<string> voteLines, IQuest quest, PartitionMode partitionMode)
        {
            if (voteLines == null)
                throw new ArgumentNullException(nameof(voteLines));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (voteLines.Count == 0)
                return new List<string>();

            // Get the list of all vote partitions, built according to current preferences.
            // One of: By line, By block, or By post (ie: entire vote)
            List<string> votePartitions = GetVotePartitions(voteLines, partitionMode, VoteType.Vote, "This is a fake voter name~~~~~~~");

            var filteredPartitions = FilterVotesByTask(votePartitions, quest);

            return filteredPartitions;
        }

        /// <summary>
        /// Filters the votes by task.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="taskFilter">The task filter.</param>
        /// <returns>Returns the votes after filtering with the task filter.</returns>
        private static List<string> FilterVotesByTask(List<string> lines, IQuest quest)
        {
            if (!quest.UseCustomTaskFilters)
                return lines;

            List<string> results = new List<string>();

            foreach (var line in lines)
            {
                string firstLine = line.GetFirstLine();
                string task = VoteString.GetVoteTask(firstLine);
                bool check = quest.TaskFilter?.Match(task) ?? false;
                if (check)
                    results.Add(line);
            }

            return results;
        }


        /// <summary>
        /// Gets the partitions of a vote based on partition mode and vote type.
        /// </summary>
        /// <param name="lines">The lines of a vote.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        /// <param name="voteType">The vote type being partitioned.</param>
        /// <param name="author">The author of the post.</param>
        /// <returns>Returns a list of partitions, representing the pieces of the vote.</returns>
        private List<string> GetVotePartitions(IEnumerable<string> lines, PartitionMode partitionMode, VoteType voteType, string author)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (string.IsNullOrEmpty(author))
                throw new ArgumentNullException(nameof(author));
            if (!lines.Any())
                return new List<string>();

            switch (voteType)
            {
                case VoteType.Rank:
                    return GetVotePartitionsFromRank(lines, partitionMode, author);
                case VoteType.Plan:
                    return GetVotePartitionsFromPlan(lines, partitionMode, author);
                case VoteType.Vote:
                    return GetVotePartitionsFromVote(lines, partitionMode, author);
                default:
                    throw new ArgumentException($"Unknown vote type: {voteType}");
            }
        }

        /// <summary>
        /// Get the partitions of a ranked vote.
        /// </summary>
        /// <param name="lines">The lines of a ranked vote.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        /// <param name="author">The author of the post.</param>
        /// <returns>Returns the vote broken into rank partitions.</returns>
        private static List<string> GetVotePartitionsFromRank(IEnumerable<string> lines, PartitionMode partitionMode, string author)
        {
            // Ranked votes only ever have one line of content.
            // Add CRLF to the end, and return that as a list.
            var partitions = lines.Select(a => a + "\r\n");

            return new List<string>(partitions);
        }

        /// <summary>
        /// Gets the vote partitions of a plan.
        /// </summary>
        /// <param name="lines">The lines of a vote plan.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        /// <param name="author">The author of the post.</param>
        /// <returns>Returns the vote partitioned appropriately.</returns>
        private List<string> GetVotePartitionsFromPlan(IEnumerable<string> lines, PartitionMode partitionMode, string author)
        {
            switch (partitionMode)
            {
                case PartitionMode.None:
                    // No partitioning; no special treatment
                    return PartitionByNone(lines, author);
                case PartitionMode.ByLine:
                    // When partitioning by line, promote the plan first.
                    // The label line can be discarded, and the others treated as less indented.
                    return PartitionByLine(PromoteLines(lines), author);
                case PartitionMode.ByLineTask:
                    // When partitioning by line, promote the plan first.
                    // The label line can be discarded, and the others treated as less indented.
                    return PartitionByLineTask(lines, author);
                case PartitionMode.ByBlock:
                    // Normal block partitioning means we don't partition plans.
                    // They will end up as a single block for the regular vote to consume.
                    return PartitionByNone(lines, author);
                case PartitionMode.ByBlockAll:
                    // When partitioning by BlockAll, any plans are themselves partitioned by block (after promotion).
                    // Make sure to preserve the task from the main line on the resulting blocks.
                    string planTask = VoteString.GetVoteTask(lines.First());
                    var blocks = PartitionByBlock(PromoteLines(lines), author);
                    return ApplyTaskToBlocks(blocks, planTask);
                default:
                    throw new ArgumentException($"Unknown partition mode: {partitionMode}");
            }
        }

        /// <summary>
        /// Gets the partitions of a vote.
        /// </summary>
        /// <param name="lines">The lines of a vote.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        /// <param name="author">The author of the post.</param>
        /// <returns>Returns the vote, partitioned according to the requested mode.</returns>
        private List<string> GetVotePartitionsFromVote(IEnumerable<string> lines, PartitionMode partitionMode, string author)
        {
            switch (partitionMode)
            {
                case PartitionMode.None:
                    // No partitioning
                    return PartitionByNone(lines, author);
                case PartitionMode.ByLine:
                    // Partition by line
                    return PartitionByLine(lines, author);
                case PartitionMode.ByLineTask:
                    // Partition by line; keep parent tasks
                    return PartitionByLineTask(lines, author);
                case PartitionMode.ByBlock:
                    // Partition by block.  Plans are considered single blocks.
                    return PartitionByBlock(lines, author);
                case PartitionMode.ByBlockAll:
                    // BlockAll partitioning means any plans are partitioned by block as well.
                    return PartitionByBlock(lines, author);
                default:
                    throw new ArgumentException($"Unknown partition mode: {partitionMode}");
            }
        }

        /// <summary>
        /// Convert the provided lines into a non-partitioned form.
        /// All individual strings are converted into CRLF-terminated portions of a string.
        /// Referral votes are inlined.
        /// </summary>
        /// <param name="lines">The lines of a vote.</param>
        /// <param name="author">The author of the post.</param>
        /// <returns>Returns a non-partitioned version of the vote.</returns>
        private List<string> PartitionByNone(IEnumerable<string> lines, string author)
        {
            List<string> partitions = new List<string>();
            StringBuilder sb = new StringBuilder();
            List<string> referralVotes = new List<string>();

            foreach (string line in lines)
            {
                // If someone copy/pasted a vote with a referral at the top (eg: self-named plan),
                // skip the copy/pasted section.
                if (referralVotes.Any())
                {
                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = voteCounter.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    foreach (var referral in referralVotes)
                        sb.Append(referral);

                    referralVotes = referralVotes.SelectMany(a => a.GetStringLines()).ToList();
                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            if (sb.Length > 0)
                partitions.Add(sb.ToString());

            return partitions;
        }

        /// <summary>
        /// Partition the provided vote into individual partitions, by line.
        /// Referral votes are added as their own partitioned form.
        /// </summary>
        /// <param name="lines">The lines of a vote.</param>
        /// <param name="author">The author of the post.</param>
        /// <returns>Returns a the vote partitioned by line.</returns>
        private List<string> PartitionByLine(IEnumerable<string> lines, string author)
        {
            List<string> partitions = new List<string>();
            List<string> referralVotes = new List<string>();

            foreach (string line in lines)
            {
                // If someone copy/pasted a vote with a referral at the top (eg: self-named plan),
                // skip the copy/pasted section.
                if (referralVotes.Any())
                {
                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = voteCounter.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    partitions.AddRange(referralVotes);

                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }
                }
                else
                {
                    partitions.Add(line + "\r\n");
                }
            }

            return partitions;
        }

        /// <summary>
        /// Partition a vote by line, but carry any task on parent lines down
        /// to child lines.
        /// </summary>
        /// <param name="lines">The lines of the vote.</param>
        /// <param name="author">The author of the vote, for use in determining
        /// valid referrals.</param>
        /// <returns>Returns a list of partitioned vote lines.</returns>
        private List<string> PartitionByLineTask(IEnumerable<string> lines, string author)
        {
            List<string> partitions = new List<string>();
            List<string> referralVotes = new List<string>();
            string parentTask = string.Empty;

            foreach (string line in lines)
            {
                // If someone copy/pasted a vote with a referral at the top (eg: self-named plan),
                // skip the copy/pasted section.
                if (referralVotes.Any())
                {
                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = voteCounter.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    partitions.AddRange(referralVotes);

                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }
                }
                else
                {
                    string taskedLine = line;

                    if (string.IsNullOrEmpty(VoteString.GetVotePrefix(line)))
                    {
                        parentTask = VoteString.GetVoteTask(line);
                    }
                    else if (string.IsNullOrEmpty(VoteString.GetVoteTask(line)))
                    {
                        taskedLine = VoteString.ModifyVoteLine(line, task: parentTask);
                    }

                    partitions.Add(taskedLine + "\r\n");
                }
            }

            return partitions;
        }

        /// <summary>
        /// Partition the provided vote into individual partitions, by block.
        /// Referral votes are added as their own partitioned form.
        /// </summary>
        /// <param name="lines">The lines of a vote.</param>
        /// <param name="author">The author of the post.</param>
        /// <returns>Returns a the vote partitioned by block.</returns>
        private List<string> PartitionByBlock(IEnumerable<string> lines, string author)
        {
            List<string> partitions = new List<string>();
            List<string> referralVotes = new List<string>();
            StringBuilder sb = new StringBuilder();

            foreach (string line in lines)
            {
                // If someone copy/pasted a vote with a referral at the top (eg: self-named plan),
                // skip the copy/pasted section.
                if (referralVotes.Any())
                {
                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = voteCounter.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    if (sb.Length > 0)
                    {
                        partitions.Add(sb.ToString());
                        sb.Clear();
                    }

                    partitions.AddRange(referralVotes);

                    referralVotes = referralVotes.SelectMany(a => a.GetStringLines()).ToList();
                    if (Agnostic.StringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }
                }
                else
                {
                    string prefix = VoteString.GetVotePrefix(line);

                    // If we encountered a new top-level vote line, store any existing stringbuilder contents.
                    if (string.IsNullOrEmpty(prefix) && sb.Length > 0)
                    {
                        partitions.Add(sb.ToString());
                        sb.Clear();
                    }

                    sb.AppendLine(line);
                }
            }

            if (sb.Length > 0)
                partitions.Add(sb.ToString());

            return partitions;
        }

        /// <summary>
        /// Add the specified task to all the provided blocks, if they don't
        /// already have a task.
        /// </summary>
        /// <param name="blocks">A list of vote blocks.</param>
        /// <param name="planTask">A task name to apply.  If no name is provided, no changes are made.</param>
        /// <returns>Returns the vote blocks with the task applied.</returns>
        private static List<string> ApplyTaskToBlocks(List<string> blocks, string planTask)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));
            if (string.IsNullOrEmpty(planTask))
                return blocks;

            List<string> results = new List<string>();

            foreach (var block in blocks)
            {
                if (VoteString.GetVoteTask(block).Length == 0)
                {
                    string rep = VoteString.ModifyVoteLine(block, task: planTask, byPartition: true);
                    results.Add(rep);
                }
                else
                {
                    results.Add(block);
                }
            }

            return results;
        }

        /// <summary>
        /// If all sub-lines of a provided group of lines are indented (have a prefix),
        /// then 'promote' them up a tier (remove one level of the prefix) while discarding
        /// the initial line.
        /// </summary>
        /// <param name="lines">A list of strings to examine/promote.</param>
        /// <returns>Returns the strings without the initial line, and with the
        /// remaining lines reduced by one indent level.</returns>
        private static IEnumerable<string> PromoteLines(IEnumerable<string> lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            var remainder = lines.Skip(1);

            if (remainder.All(l => VoteString.GetVotePrefix(l).Length > 0))
            {
                return remainder.Select(l => l.Substring(1).Trim());
            }

            return remainder;
        }
        #endregion
    }
}
