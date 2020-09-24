using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Forums;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.VoteCounting;

namespace NetTally.Votes
{
    /// <summary>
    /// Class that can handle constructing votes from the parsed text of a post.
    /// </summary>
    public class VoteConstructor
    {
        readonly IVoteCounter voteCounter;

        /// <summary>
        /// <see cref="VoteConstructor"/> class has a dependency on <see cref="IVoteCounter"/>.
        /// </summary>
        /// <param name="voteCounter">The vote counter to store locally and use within this class.</param>
        public VoteConstructor(IVoteCounter voteCounter)
        {
            this.voteCounter = voteCounter;
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
            bool asBlocks, Func<IEnumerable<VoteLine>, (bool isPlan, bool isImplicit, string planName)> isPlanFunction)
        {
            Dictionary<string, VoteLineBlock> plans = new Dictionary<string, VoteLineBlock>(StringComparer.Ordinal);

            // Either split the vote into blocks, or encapsulate the vote into an enumerable
            // so that it can be treated the same way.
            var blocks = asBlocks ? VoteBlocks.GetBlocks(post.VoteLines) : new List<VoteLineBlock>() { new VoteLineBlock(post.VoteLines) };

            foreach (var block in blocks)
            {
                var (isPlan, isImplicit, planName) = isPlanFunction(block);

                if (isPlan &&
                    !(isImplicit && quest.ForbidVoteLabelPlanNames) &&
                    IsValidPlanName(planName, post.Origin.Author) &&
                    DoesTaskFilterPass(block, quest))
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
        public List<VoteLineBlock> ProcessPostGetVotes(Post post, IQuest quest)
        {
            if (post.Processed)
                return null;

            if (!post.WorkingVoteComplete)
                ConfigureWorkingVote(post, quest);

            // If the working vote configuration is complete, process the post.
            if (post.WorkingVoteComplete)
            {
                // If a newer vote has been registered in the vote counter, that means
                // that this post was a prior future reference that got overridden later.
                // If so, don't process it now, but allow the post to be marked as
                // processed so that it doesn't try to re-submit it later.
                if (voteCounter.HasNewerVote(post))
                {
                    post.Processed = true;
                }
                else
                {
                    // Get the results of partitioning the post.
                    var results = PartitionPost(post, quest.PartitionMode);

                    // Apply task filtering.
                    var filteredResults = results.Where(p => DoesTaskFilterPass(p, quest)).ToList();

                    post.Processed = true;
                    return filteredResults;
                }

            }

            return null;
        }

        /// <summary>
        /// Given a plan block, if the plan is a base/proposed plan, rename it as just a "Plan".
        /// Convert the marker for all lines to None.
        /// </summary>
        /// <param name="plan">The plan to examine.</param>
        /// <returns>Returns the original plan, or the modified plan if it used "Base Plan".</returns>
        public (string name, VoteLineBlock contents) NormalizePlan(string keyName, VoteLineBlock keyContents)
        {
            VoteLine firstLine = keyContents.First();

            var (planType, planName) = VoteBlocks.CheckIfPlan(firstLine);

            // Proposed needs to be converted to an unadorned plan name.
            if (planType == VoteBlocks.LineStatus.Proposed)
            {
                string content = $"Plan: {planName}";
                firstLine = firstLine.WithContent(content);
            }

            // All vote lines in a plan should have MarkerType of None.
            // This allows them to be part of any comparison, and easily mesh with various output.
            if (planType != VoteBlocks.LineStatus.None)
            {
                firstLine = firstLine.WithMarker("", MarkerType.None, 0);

                List<VoteLine> voteLines = new List<VoteLine>() { firstLine };
                voteLines.AddRange(keyContents.Skip(1));

                var returnPlan = new VoteLineBlock(voteLines).WithMarker(Strings.PlanNameMarker, MarkerType.Plan, 0);

                return (planName, returnPlan);
            }

            // If it's not a plan, how did we get here?
            return (keyName, keyContents);
        }

        /// <summary>
        /// Partition a plan after initial preprocessing.
        /// </summary>
        /// <param name="block">The block defining the plan.</param>
        /// <param name="partitionMode">The current partitioning mode.</param>
        /// <returns>Returns a collection of VoteLineBlocks, extracted from the plan.</returns>
        public List<VoteLineBlock> PartitionPlan(VoteLineBlock block, PartitionMode partitionMode)
        {
            return Partition(block, partitionMode, asPlan: true);
        }

        /// <summary>
        /// Given a vote block, partition it by block and save the resulting
        /// split votes in the vote counter.
        /// </summary>
        /// <param name="vote">The vote to partition.</param>
        /// <returns>Returns true if successfully completed.</returns>
        public List<VoteLineBlock> PartitionChildren(VoteLineBlock vote)
        {
            // Break vote block into child blocks and return them.
            return Partition(vote, PartitionMode.ByBlockAll);
        }
        #endregion

        #region Working Vote Configuration
        /// <summary>
        /// Work through the original post line, remove any base plans, and expand
        /// any vote or plan references.  Store the information in the WorkingVote.
        /// </summary>
        /// <param name="post">The post with the working vote to configure.</param>
        /// <param name="quest">The quest being tallied.</param>
        public void ConfigureWorkingVote(Post post, IQuest quest)
        {
            if (post.WorkingVoteComplete)
                return;

            List<(VoteLine line, VoteLineBlock block)> workingVote = new List<(VoteLine line, VoteLineBlock block)>();

            // Proposed plans are skipped entirely, if this is the original post that proposed the plan.
            // Keep everything else, flattening the blocks back into a simple list of vote lines.
            var validVoteLines = VoteBlocks.GetBlocks(post.VoteLines).Where(b => !IsProposedPlan(b)).SelectMany(a => a).ToList();

            for (int i = 0; i < validVoteLines.Count; i++)
            {
                var currentLine = validVoteLines[i];

                var (isReference, isPlan, isPinnedUser, refName) = GetReference(currentLine, quest);

                if (isReference)
                {
                    if (isPlan)
                    {
                        // We can rely on GetReference returning a valid plan name.
                        var refPlan = voteCounter.GetReferencePlan(refName);

                        // If there is no available reference plan, just add the line and continue.
                        if (refPlan == null)
                        {
                            JustAddDirectly(currentLine);
                            continue;
                        }

                        // Is the plan reference a single line, or is the entire plan embedded in the vote?
                        var partial = validVoteLines.Skip(i).Take(refPlan.Lines.Count);

                        // If it's a full match, we need to skip past these lines in the next index increment.
                        if (refPlan.Equals(partial))
                        {
                            i += refPlan.Lines.Count - 1; // compensate for the i++ increment
                        }
                        else if ((i + 1) < validVoteLines.Count && validVoteLines[i + 1].Depth > 0)
                        {
                            // If a block references a plan, but does not match the original plan in full (eg: missing lines),
                            // don't treat the initial line as a plan reference and then leave junk lines, but just add the
                            // line as normal text.

                            JustAddDirectly(currentLine);
                            continue;
                        }

                        // Meanwhile, we need to pull copies of all vote blocks and store them in our working set.

                        var voteBlocks = voteCounter.GetVotesBy(refName);

                        foreach (var voteBlock in voteBlocks)
                        {
                            workingVote.Add((null, voteBlock.WithMarker(currentLine.Marker, currentLine.MarkerType, currentLine.MarkerValue)));
                        }
                    }
                    // Users
                    else
                    {
                        PostId postSearchLimit = isPinnedUser ? post.Origin.ID : PostId.Zero;

                        Post refUserPost = voteCounter.GetLastPostByAuthor(refName, postSearchLimit);

                        // If we can't find the reference post, just treat this as a normal line.
                        if (refUserPost == null)
                        {
                            workingVote.Add((currentLine, null));
                        }
                        // If the reference post hasn't been processed yet, bail out entirely,
                        // because we're in a future reference position.
                        else if (!refUserPost.Processed && !post.ForceProcess)
                        {
                            return;
                        }
                        // Otherwise save the reference vote.
                        else
                        {
                            var voteBlocks = voteCounter.GetVotesBy(refName);

                            if (voteBlocks.Count > 0)
                            {
                                foreach (var voteBlock in voteBlocks)
                                {
                                    workingVote.Add((null, voteBlock.WithMarker(currentLine.Marker, currentLine.MarkerType, currentLine.MarkerValue)));
                                }
                            }
                            else
                            {
                                // If the user being referenced doesn't actually have any vote,
                                // just add the line directly.  This is most likely due to the
                                // referenced user just proposing a plan, but not making a vote.
                                workingVote.Add((currentLine, null));
                            }
                        }
                    }
                }
                // Non-references just get added directly.
                else
                {
                    JustAddDirectly(currentLine);
                }
            }

            post.WorkingVote.AddRange(workingVote);
            post.WorkingVoteComplete = true;

            return;

            //////////////////////////////////////////

            // Local function to handle determining if the block is part of a Proposed Plan or not.
            bool IsProposedPlan(VoteLineBlock block)
            {
                var (isProposedPlan, isImplicit, proposedPlanName) = VoteBlocks.IsBlockAProposedPlan(block);

                if (isProposedPlan)
                {
                    Origin planOrigin = voteCounter.GetPlanOriginByName(proposedPlanName);

                    if (planOrigin == null)
                        return false;

                    return planOrigin.ID == post.Origin.ID;
                }

                return false;
            }

            // Just add the vote line directly to the working vote if it's
            // not a reference, or we can't find the reference.
            void JustAddDirectly(VoteLine currentLine)
            {
                // Handle trimming extended text.
                if (quest.TrimExtendedText)
                {
                    workingVote.Add((currentLine.WithTrimmedContent(), null));
                }
                else
                {
                    workingVote.Add((currentLine, null));
                }
            }
        }




        // A regex to extract potential references from a vote line.
        static readonly Regex referenceNameRegex =
            new Regex(@"^(?<label>(?:\^|↑)(?=\s*\w)|(?:(?:(?:base|proposed)\s*)?plan\b)(?=\s*:?\s*\S))?\s*:?\s*(?<reference>.+)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Attempt to determine if the content of the provided vote line is a reference to a user or plan.
        /// If so, determine what type, and extract the reference name.
        /// </summary>
        /// <param name="voteLine">The vote line to examine.</param>
        /// <param name="quest">The quest being tallied.  Has configuration options that may apply.</param>
        /// <returns>Returns a tuple with the discovered information.</returns>
        private (bool isReference, bool isPlan, bool isPinnedUser, Origin refName) GetReference(VoteLine voteLine, IQuest quest)
        {
            // Ignore lines over 100 characters long. They can't be user names, and are too long for useful plan names.
            if (voteLine.CleanContent.Length > 100)
                goto noReference;

            Match m = referenceNameRegex.Match(voteLine.CleanContent);
            if (m.Success)
            {
                string label = m.Groups["label"].Value;
                string refName = m.Groups["reference"].Value;

                if (string.Equals(label, "^") || string.Equals(label, "↑"))
                {
                    Origin refUser = voteCounter.GetVoterOriginByName(refName);

                    // Check to make sure the quest hasn't disabled user proxy votes.
                    if (refUser != null && quest.DisableProxyVotes == false)
                        return (isReference: true, isPlan: false, isPinnedUser: true, refName: refUser);
                }
                else if (label.StartsWith("base") || label.StartsWith("proposed"))
                {
                    Origin refPlan = voteCounter.GetPlanOriginByName(refName);

                    if (refPlan != null)
                        return (isReference: true, isPlan: true, isPinnedUser: false, refName: refPlan);
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(label, "plan"))
                {
                    Origin refPlan = voteCounter.GetPlanOriginByName(refName);

                    if (refPlan != null)
                        return (isReference: true, isPlan: true, isPinnedUser: false, refName: refPlan);

                    // Check user names second
                    Origin refUser = voteCounter.GetVoterOriginByName(refName);

                    // Check to make sure the quest hasn't disabled user proxy votes.
                    // Force pinning if requested.
                    if (refUser != null && quest.DisableProxyVotes == false)
                        return (isReference: true, isPlan: false, isPinnedUser: quest.ForcePinnedProxyVotes, refName: refUser);
                }
                else // Any unlabeled lines
                {
                    // Check user names first
                    Origin refUser = voteCounter.GetVoterOriginByName(refName);

                    // Check to make sure the quest hasn't disabled user proxy votes.
                    if (refUser != null && quest.DisableProxyVotes == false)
                        return (isReference: true, isPlan: false, isPinnedUser: quest.ForcePinnedProxyVotes, refName: refUser);

                    Origin refPlan = voteCounter.GetPlanOriginByName(refName);

                    // Check to make sure the quest doesn't forbid non-labeled plan references.
                    if (refPlan != null && quest.ForcePlanReferencesToBeLabeled == false)
                        return (isReference: true, isPlan: true, isPinnedUser: false, refName: refPlan);
                }
            }

        noReference:
            return (isReference: false, isPlan: false, isPinnedUser: false, refName: Origin.Empty);
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
            if (voteCounter.HasVoter(planName))
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
        private bool DoesTaskFilterPass(VoteLineBlock block, IQuest quest)
        {
            // Always allow if no filters are active.
            if (!quest.UseCustomTaskFilters)
                return true;

            if (string.IsNullOrEmpty(block.Task))
                return false;

            return quest.TaskFilter?.Match(block.Task) ?? false;
        }
        #endregion

        #region Partitioning utility functions for partitioning posts
        /// <summary>
        /// Run partitioning on a vote block, without consideration for proxy votes.
        /// Will not cascade tasks.
        /// </summary>
        /// <param name="block">The block to partition.</param>
        /// <param name="partitionMode">The partitioning mode.</param>
        /// <returns></returns>
        private List<VoteLineBlock> Partition(VoteLineBlock block, PartitionMode partitionMode, bool asPlan = false)
        {
            List<VoteLineBlock> partitions = new List<VoteLineBlock>();

            // If we're not partitioning, we have no work to do.
            if (partitionMode == PartitionMode.None)
            {
                partitions.Add(block);
                return partitions;
            }

            // Single line blocks don't need extra handling.
            if (block.Lines.Count == 1)
            {
                partitions.Add(block);
                return partitions;
            }

            // A content block is the same as an explicit plan.
            if (VoteBlocks.IsThisAContentBlock(block))
            {
                // ByLine only needs to skip the first line, and take the rest after promoting one indent level.
                if (partitionMode == PartitionMode.ByLine || partitionMode == PartitionMode.ByLineTask)
                {
                    int minDepth = int.MaxValue;
                    foreach (var line in block.Skip(1))
                    {
                        if (line.Depth < minDepth)
                            minDepth = line.Depth;
                    }

                    foreach (var line in block.Skip(1))
                    {
                        var pLine = line.GetPromotedLine(minDepth);
                        partitions.Add(new VoteLineBlock(pLine));
                    }

                    return partitions;
                }
                else if (partitionMode == PartitionMode.ByBlock)
                {
                    // A content block is already partitioned by block
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

                    // Maybe: Apply main task to sub blocks.

                    return VoteBlocks.GetBlocks(block.Skip(1).Select(a => a.GetPromotedLine(minDepth))).ToList();
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"Unknown partition mode: {partitionMode}", nameof(partitionMode));
                }
            }
            // A non-content block is anything else, like an implicit plan.
            else
            {
                // ByLine is simple.
                if (partitionMode == PartitionMode.ByLine || partitionMode == PartitionMode.ByLineTask)
                {
                    foreach (var line in block.Skip(asPlan ? 1 : 0))
                    {
                        partitions.Add(new VoteLineBlock(line));
                    }

                    return partitions;
                }
                // Normal By Block does not partition implicit plans
                else if (partitionMode == PartitionMode.ByBlock)
                {
                    if (asPlan && !VoteBlocks.IsBlockAnImplicitPlan(block).isImplicit)
                    {
                        return VoteBlocks.GetBlocks(block.Skip(asPlan ? 1 : 0)).ToList();
                    }
                    else
                    {
                        partitions.Add(block);
                        return partitions;
                    }
                }
                // By block (all) partitions even implicit plans.
                else if (partitionMode == PartitionMode.ByBlockAll)
                {
                    return VoteBlocks.GetBlocks(block.Skip(asPlan ? 1 : 0)).ToList();
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
            switch (partitionMode)
            {
                case PartitionMode.None:
                    return PartitionPostByNone(post);
                case PartitionMode.ByLine:
                    return PartitionPostByLine(post);
                case PartitionMode.ByLineTask:
                    return PartitionPostByLineTask(post);
                case PartitionMode.ByBlock:
                    return PartitionPostByBlock(post);
                case PartitionMode.ByBlockAll:
                    return PartitionPostByBlock(post);
                default:
                    throw new InvalidOperationException($"Unknown partition mode: {partitionMode}");
            }
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

            foreach (var (line, block) in post.WorkingVote)
            {
                if (line != null)
                {
                    working.Add(line);
                }
                else if (block != null)
                {
                    working.AddRange(block);
                }
            }

            List<VoteLineBlock> results = new List<VoteLineBlock>();

            if (working.Count > 0)
            {
                VoteLineBlock workingBlock = new VoteLineBlock(working);

                results.Add(workingBlock);
            }

            return results;
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

            foreach (var (line, block) in post.WorkingVote)
            {
                if (line != null)
                {
                    working.Add(new VoteLineBlock(line));
                }
                else if (block != null)
                {
                    working.AddRange(block.Select(a => new VoteLineBlock(a)));
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

            foreach (var (line, block) in post.WorkingVote)
            {
                if (line != null)
                {
                    working.Add(CascadeLineTask(line, ref currentTask, ref taskStack));
                }
                else if (block != null)
                {
                    // If we hit an embedded block, reset the current task, break the block up, and do the task cascade.
                    taskStack.Clear();
                    currentTask = (0, "");

                    foreach (var blockLine in block)
                    {
                        working.Add(CascadeLineTask(blockLine, ref currentTask, ref taskStack));
                    }
                }
            }

            return working;
        }

        /// <summary>
        /// Generate the vote partitions for a post, using block-level partitioning.
        /// </summary>
        /// <param name="post">The post with the vote to be partitioned.</param>
        /// <returns>Returns a list of vote blocks.</returns>
        private List<VoteLineBlock> PartitionPostByBlock(Post post)
        {
            List<VoteLineBlock> working = new List<VoteLineBlock>();
            List<VoteLine> tempList = new List<VoteLine>();

            foreach (var (line, block) in post.WorkingVote)
            {
                if (line != null)
                {
                    // Start a new block if we reach a new 0-depth line.
                    if (line.Depth == 0 && tempList.Count > 0)
                    {
                        working.Add(new VoteLineBlock(tempList));
                        tempList.Clear();
                    }

                    tempList.Add(line);
                }
                else if (block != null)
                {
                    // Save the accumulated lines if we run into a block element.
                    if (tempList.Count > 0)
                    {
                        working.Add(new VoteLineBlock(tempList));
                        tempList.Clear();
                    }

                    // If partition mode is BlockAll, the plan has already been
                    // partitioned, so we don't need to re-do the work.
                    working.Add(block);
                }
            }

            // Save any remainder
            if (tempList.Count > 0)
            {
                working.Add(new VoteLineBlock(tempList));
                tempList.Clear();
            }

            return working;
        }

        private VoteLineBlock CascadeLineTask(VoteLine line,
            ref (int depth, string task) currentTask,
            ref Stack<(int depth, string task)> taskStack)
        {
            // If we have no task, and the line has no task, do nothing.
            if (line.Task.Length == 0 && currentTask.task.Length == 0)
            {
                return new VoteLineBlock(line);
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
                return new VoteLineBlock(line);
            }

            // If we move to a greater depth...
            if (line.Depth > currentTask.depth)
            {
                // If the new line has no task, just propogate the current task and move on.
                if (line.Task.Length == 0)
                {
                    return new VoteLineBlock(line.WithTask(currentTask.task));
                }
                // Otherwise save the current task on the stack and update to the new task.
                else
                {
                    taskStack.Push(currentTask);
                    currentTask = (line.Depth, line.Task);
                    return new VoteLineBlock(line);
                }
            }

            // We should never get here, but if we do, just return the line.
            return new VoteLineBlock(line);
        }
        #endregion
    }
}
