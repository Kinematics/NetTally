using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Utility.Comparers;

namespace NetTally.Tally.ComponentsF.Counting;

/// <summary>
/// Class that can handle constructing votes from the parsed text of a post.
/// </summary>
public static partial class VoteConstructor
{
    #region Regexes
    // A regex to extract potential references from a vote line.
    [GeneratedRegex(@"^(?<label>(?:\^|↑)(?=\s*\w)|(?:(?:(?:base|proposed)\s*)?plan\b)(?=\s*:?\s*\S))?\s*:?\s*@?(?<reference>.+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ReferenceRegex();

    static readonly Regex referenceNameRegex = ReferenceRegex();
    #endregion Regexes

    #region General public processing functions
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
    public static Dictionary<string, VoteBlockType> PreprocessPostGetPlans(
        Quest quest,
        AuthorType author,
        Func<VoteBlockType, PlanDescriptor> isPlanFunction,
        List<VoteBlockType> blocks)
    {
        Dictionary<string, VoteBlockType> plans = new(StringComparer.OrdinalIgnoreCase);

        foreach (var block in blocks)
        {
            var (isPlan, isImplicit, planName) = isPlanFunction(block);

            if (isPlan &&
                !(isImplicit && quest.ForbidVoteLabelPlanNames) &&
                IsValidPlanName(planName, author.Name, quest) &&
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
    /// <param name="votes">Returns any votes from the post if the post was processed.</param>
    /// <returns><c>True</c> if the post was processed, or <c>false</c> if it was not.</returns>
    public static bool TryProcessPostGetVotes(PostToProcess post, Quest quest,
        out List<VoteBlockType> votes)
    {
        votes = [];

        if (!post.Processed)
        {
            ConfigureWorkingVote(post, quest);

            // If the working vote configuration is complete, process the post.
            if (post.WorkingVoteComplete)
            {
                // If a newer vote has been registered in the vote counter, that means
                // that this post was a prior future reference that got overridden later.
                // If so, don't process it now, but mark the post as processed so that
                // it doesn't get re-submitted later.
                if (quest.VoteCounterF.HasNewerVote(post))
                {
                    post.Processed = true;
                }
                else
                {
                    // Get the results of partitioning the post.
                    var results = PartitionPost(post, quest.PartitionMode);

                    // Add partitions that pass task filtering.
                    votes.AddRange(results.Where(p => DoesTaskFilterPass(p, quest)));

                    post.Processed = true;
                }
            }
        }

        return post.Processed;
    }
    #endregion General public processing functions

    #region Utility functions for processing votes.
    /// <summary>
    /// Make sure the provided plan name is valid.
    /// A named vote that is named after a user is only valid if it matches the post author's name.
    /// </summary>
    /// <param name="planName">The name of the plan.</param>
    /// <param name="postAuthor">The post's author.</param>
    /// <returns>Returns true if the plan name is deemed valid.</returns>
    private static bool IsValidPlanName(string planName, string postAuthor, Quest quest)
    {
        // A named vote that is named after a user is only valid if it matches the post author's name.
        if (quest.VoteCounterF.HasVoter(planName))
        {
            if (!Agnostic.CaseInsensitiveComparer.Equals(planName, postAuthor))
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
    private static bool DoesTaskFilterPass(VoteBlockType block, Quest quest)
    {
        // Always allow if no filters are active.
        if (!quest.UseCustomTaskFilters)
            return true;

        return quest.TaskFilter.Match(block.Task.Name);
    }

    /// <summary>
    /// Work through the original post lines, remove any base plans, and expand
    /// any vote or plan references.  Store the information in the WorkingVote.
    /// </summary>
    /// <param name="post">The post with the working vote to configure.</param>
    /// <param name="quest">The quest being tallied.</param>
    private static void ConfigureWorkingVote(PostToProcess post, Quest quest)
    {
        if (post.WorkingVoteComplete)
            return;

        List<VoteBlockType> workingVote = [];

        // Proposed plans are skipped entirely, if this is the original post that proposed the plan.
        // Keep everything else, flattening the blocks back into a simple list of vote lines.
        var validVoteLines = VoteBlocks
            .GetBlocks(post.VoteLines)
            .Where(b => !IsProposedPlan(b))
            .SelectMany(a => a).ToList();

        for (int i = 0; i < validVoteLines.Count; i++)
        {
            var currentLine = validVoteLines[i];

            var (isReference, isPlan, isPinnedUser, refName) = GetReference(currentLine, quest);

            if (isReference)
            {
                if (isPlan)
                {
                    // We can rely on GetReference returning a valid plan name.
                    var refPlan = quest.VoteCounterF.GetReferencePlan(refName);

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
                    else if ((i + 1) < validVoteLines.Count && validVoteLines[i + 1].Prefix.Depth > 0)
                    {
                        // If a block references a plan, but does not match the original plan in full (eg: missing lines),
                        // don't treat the initial line as a plan reference and then leave junk lines, but just add the
                        // line as normal text.

                        JustAddDirectly(currentLine);
                        continue;
                    }

                    // Meanwhile, we need to pull copies of all vote blocks and store them in our working set.

                    var voteBlocks = quest.VoteCounterF.GetVotesBy(refName);

                    foreach (var voteBlock in voteBlocks)
                    {
                        var replacementBlock = voteBlock with { Marker = currentLine.Marker };
                        workingVote.Add(replacementBlock);
                    }
                }
                // Users
                else
                {
                    PostIdType postSearchLimit = isPinnedUser ? post.Origin.PostId : PostId.Zero;

                    PostToProcess? refUserPost = quest.VoteCounterF.GetLastPostByAuthor(refName, postSearchLimit);

                    // If we can't find the reference post, just treat this as a normal line.
                    if (refUserPost == null)
                    {
                        var block = VoteBlock.Create(currentLine);
                        workingVote.Add(block);
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
                        var voteBlocks = quest.VoteCounterF.GetVotesBy(refName);

                        if (voteBlocks.Any())
                        {
                            foreach (var voteBlock in voteBlocks)
                            {
                                var replacementBlock = voteBlock with { Marker = currentLine.Marker };
                                workingVote.Add(replacementBlock);
                            }
                        }
                        else
                        {
                            // If the user being referenced doesn't actually have any vote,
                            // just add the line directly.  This is most likely due to the
                            // referenced user just proposing a plan, but not making a vote.
                            var block = VoteBlock.Create(currentLine);
                            workingVote.Add(block);
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
        bool IsProposedPlan(VoteBlockType block)
        {
            var (isProposedPlan, isImplicit, proposedPlanName) = VoteBlocks.IsBlockAProposedPlan(block);

            if (isProposedPlan)
            {
                OriginType? planOrigin = quest.VoteCounterF.GetPlanOriginByName(proposedPlanName);

                if (planOrigin == null)
                    return false;

                return PostIdComparer.Instance.Equals(planOrigin.PostId, post.Origin.PostId);
            }

            return false;
        }

        // Just add the vote line directly to the working vote if it's
        // not a reference, or we can't find the reference.
        void JustAddDirectly(VoteLineType currentLine)
        {
            // Handle trimming extended text.
            if (quest.TrimExtendedText)
            {
                var trimmedContent = VoteContent.Trim(currentLine.Content);
                var replacementLine = currentLine with { Content = trimmedContent };
                var block = VoteBlock.Create(replacementLine);
                workingVote.Add(block);
            }
            else
            {
                var block = VoteBlock.Create(currentLine);
                workingVote.Add(block);
            }
        }
    }

    /// <summary>
    /// Attempt to determine if the content of the provided vote line is a reference to a user or plan.
    /// If so, determine what type, and extract the reference name.
    /// </summary>
    /// <param name="voteLine">The vote line to examine.</param>
    /// <param name="quest">The quest being tallied.  Has configuration options that may apply.</param>
    /// <returns>Returns a tuple with the discovered information.</returns>
    private static (bool isReference, bool isPlan, bool isPinnedUser, OriginType refName)
        GetReference(VoteLineType voteLine, Quest quest)
    {
        // Ignore lines over 100 characters long. They can't be user names, and are too long for useful plan names.
        if (voteLine.Content.CleanContent.Length > 100)
            goto noReference;

        Match m = referenceNameRegex.Match(voteLine.Content.CleanContent);
        if (m.Success)
        {
            string label = m.Groups["label"].Value;
            string refName = m.Groups["reference"].Value;

            if (label == "^" || label == "↑")
            {
                OriginType? refUser = quest.VoteCounterF.GetVoterOriginByName(refName);

                // Check to make sure the quest hasn't disabled user proxy votes.
                if (refUser != null && quest.DisableProxyVotes == false)
                    return (isReference: true, isPlan: false, isPinnedUser: true, refName: refUser);
            }
            else if (label.StartsWith("base", StringComparison.OrdinalIgnoreCase)
                  || label.StartsWith("proposed", StringComparison.OrdinalIgnoreCase))
            {
                OriginType? refPlan = quest.VoteCounterF.GetPlanOriginByName(refName);

                if (refPlan != null)
                    return (isReference: true, isPlan: true, isPinnedUser: false, refName: refPlan);
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(label, "plan"))
            {
                OriginType? refPlan = quest.VoteCounterF.GetPlanOriginByName(refName);

                if (refPlan != null)
                    return (isReference: true, isPlan: true, isPinnedUser: false, refName: refPlan);

                // Check user names second
                OriginType? refUser = quest.VoteCounterF.GetVoterOriginByName(refName);

                // Check to make sure the quest hasn't disabled user proxy votes.
                // Force pinning if requested.
                if (refUser != null && quest.DisableProxyVotes == false)
                    return (isReference: true, isPlan: false, isPinnedUser: quest.ForcePinnedProxyVotes, refName: refUser);
            }
            else // Any unlabeled lines
            {
                // Check user names first
                OriginType? refUser = quest.VoteCounterF.GetVoterOriginByName(refName);

                // Check to make sure the quest hasn't disabled user proxy votes.
                if (refUser != null && quest.DisableProxyVotes == false)
                    return (isReference: true, isPlan: false, isPinnedUser: quest.ForcePinnedProxyVotes, refName: refUser);

                OriginType? refPlan = quest.VoteCounterF.GetPlanOriginByName(refName);

                // Check to make sure the quest doesn't forbid non-labeled plan references.
                if (refPlan != null && quest.ForcePlanReferencesToBeLabeled == false)
                    return (isReference: true, isPlan: true, isPinnedUser: false, refName: refPlan);
            }
        }

    noReference:
        return (isReference: false, isPlan: false, isPinnedUser: false, refName: Origin.None);
    }
    #endregion


    #region Partitioning utility functions for partitioning posts

    #region Paritioning Blocks
    /// <summary>
    /// Partition a plan after initial preprocessing.
    /// </summary>
    /// <param name="block">The block defining the plan.</param>
    /// <param name="partitionMode">The current partitioning mode.</param>
    /// <returns>Returns a collection of VoteLineBlocks, extracted from the plan.</returns>
    public static List<VoteBlockType> PartitionPlan(VoteBlockType block, PartitionMode partitionMode)
    {
        return PartitionBlock(block, partitionMode, asPlan: true);
    }

    public static List<VoteBlockType> PartitionChildren(VoteBlockType vote)
    {
        // Break vote block into child blocks and return them.
        return PartitionBlock(vote, PartitionMode.ByBlockAll);
    }

    /// <summary>
    /// Run partitioning on a vote block, without consideration for proxy votes.
    /// Will not cascade tasks.
    /// </summary>
    /// <param name="block">The block to partition.</param>
    /// <param name="partitionMode">The partitioning mode.</param>
    /// <returns>A list of vote blocks.</returns>
    private static List<VoteBlockType> PartitionBlock(VoteBlockType block, PartitionMode partitionMode, bool asPlan = false)
    {
        // If we're not partitioning, we have no work to do.
        if (partitionMode == PartitionMode.None)
            return [block];

        // Single line blocks don't need extra handling.
        if (block.Lines.Count == 1)
            return [block];

        // A content block is the same as an explicit plan.
        if (VoteBlocks.IsThisAContentBlock(block))
        {
            return PartitionBlockForContentBlock(block, partitionMode);
        }
        // A non-content block is anything else, like an implicit plan.
        else
        {
            return PartitionBlockForNonContentBlock(block, partitionMode, asPlan);
        }
    }

    private static List<VoteBlockType> PartitionBlockForContentBlock(
        VoteBlockType block,
        PartitionMode partitionMode)
    {
        // By Line only needs to skip the first line, and take the rest after promoting.
        if (partitionMode == PartitionMode.ByLine || partitionMode == PartitionMode.ByLineTask)
        {
            int minDepth = block.Lines.Skip(1).Min(a => a.Prefix.Depth);

            var promotedLines = block.Lines.Skip(1)
                .Select(v => VoteLine.Promote(v, minDepth))
                .Select(VoteBlock.Create)
                .Where(v => v != null)
                .Select(v => v!);

            return promotedLines.ToList();
        }
        else if (partitionMode == PartitionMode.ByBlock)
        {
            // A content block is already partitioned by block
            return [block];
        }
        else if (partitionMode == PartitionMode.ByBlockAll)
        {
            int minDepth = block.Lines.Skip(1).Min(a => a.Prefix.Depth);

            var promotedLines = block.Lines.Skip(1)
                .Select(v => VoteLine.Promote(v, minDepth));

            var promotedBlocks = VoteBlocks.GetBlocks(promotedLines);

            return promotedBlocks.ToList();
        }

        // Failed partition mode checks.
        throw new ArgumentOutOfRangeException(nameof(partitionMode), $"Unknown partition mode: {partitionMode}");
    }
        
    private static List<VoteBlockType> PartitionBlockForNonContentBlock(
        VoteBlockType block,
        PartitionMode partitionMode,
        bool asPlan = false)
    {
        List<VoteBlockType> partitions = [];
        int skipLines = asPlan ? 1 : 0;

        // By Line is simple.
        if (partitionMode == PartitionMode.ByLine || partitionMode == PartitionMode.ByLineTask)
        {
            var partitionedLines = block.Lines.Skip(skipLines)
                .Select(VoteBlock.Create)
                .Where(v => v != null)
                .Select(v => v!);

            return partitionedLines.ToList();
        }
        else if (partitionMode == PartitionMode.ByBlock)
        {
            // Normal By Block does not partition implicit plans
            if (asPlan && VoteBlocks.IsBlockAnImplicitPlan(block).IsImplicit)
            {
                return [block];
            }

            return VoteBlocks.GetBlocks(block.Skip(skipLines)).ToList();
        }
        else if (partitionMode == PartitionMode.ByBlockAll)
        {
            // By block (all) partitions even implicit plans.
            return VoteBlocks.GetBlocks(block.Skip(skipLines)).ToList();
        }

        // Failed partition mode checks.
        throw new ArgumentOutOfRangeException(nameof(partitionMode), $"Unknown partition mode: {partitionMode}");
    }
    #endregion Paritioning Blocks

    #region Paritioning Posts
    /// <summary>
    /// Partition a post based on the requested partition mode.
    /// </summary>
    /// <param name="post">The post whose vote is being partitioned.</param>
    /// <param name="partitionMode">The partition mode to use.</param>
    /// <returns>Returns the partitions that are to be counted.</returns>
    private static List<VoteBlockType> PartitionPost(PostToProcess post, PartitionMode partitionMode)
    {
        return partitionMode switch
        {
            PartitionMode.None => PartitionPostByNone(post),
            PartitionMode.ByLine => PartitionPostByLine(post),
            PartitionMode.ByLineTask => PartitionPostByLineTask3(post),
            PartitionMode.ByBlock => PartitionPostByBlock(post),
            PartitionMode.ByBlockAll => PartitionPostByBlock(post),
            _ => throw new InvalidOperationException($"Unknown partition mode: {partitionMode}")
        };
    }

    /// <summary>
    /// Generate the vote partitions for a post.
    /// There is no partitioning, so all this does is pull in proxy references.
    /// </summary>
    /// <param name="post">The post whose vote is being partitioned.</param>
    /// <returns>Returns the partitions that are to be counted.</returns>
    private static List<VoteBlockType> PartitionPostByNone(PostToProcess post)
    {
        var collated = post.WorkingVote.SelectMany(v => v);
        var block = VoteBlock.Create(collated);

        if (block == null)
            return [];

        return [block];
    }

    /// <summary>
    /// Generate the vote partitions for a post, using line-level partitioning.
    /// Incorporates any proxy references.
    /// </summary>
    /// <param name="post">The post with the vote to be partitioned.</param>
    /// <returns>Returns a list of vote blocks.</returns>
    private static List<VoteBlockType> PartitionPostByLine(PostToProcess post)
    {
        var partitionedLines = post.WorkingVote
            .SelectMany(v => v)
            .Select(VoteBlock.Create)
            .Where(v => v != null)
            .Select(v => v!);

        return partitionedLines.ToList();
    }

    /// <summary>
    /// Generate the vote partitions for a post, using block-level partitioning.
    /// </summary>
    /// <param name="post">The post with the vote to be partitioned.</param>
    /// <returns>Returns a list of vote blocks.</returns>
    private static List<VoteBlockType> PartitionPostByBlock(PostToProcess post)
    {
        var collated = post.WorkingVote.SelectMany(v => v);
        var blocks = VoteBlocks.GetBlocks(collated);
        return blocks.ToList();
    }

    /// <summary>
    /// Assigns parent-most task to all child lines.
    /// </summary>
    /// <param name="post"></param>
    /// <returns></returns>
    private static List<VoteBlockType> PartitionPostByLineTask2(PostToProcess post)
    {
        var collated = post.WorkingVote.SelectMany(v => v);

        var blocks = VoteBlocks.GetBlocks(collated);

        var retaskedBlocks = blocks.Select(Retask)
                .SelectMany(v => v)
                .Select(VoteBlock.Create)
                .Where(v => v != null)
                .Select(v => v!)
                .ToList();

        return retaskedBlocks;

        static IEnumerable<VoteLineType> Retask(VoteBlockType block, int arg2)
        {
            if (block.Lines.Count == 0)
                return [];

            var task = block.Lines[0].Task;

            return block.Select(v => v with { Task = task });
        }
    }

    /// <summary>
    /// Recursively assigns parent tasks to child lines, taking the closest parent task value.
    /// </summary>
    /// <param name="post"></param>
    /// <returns></returns>
    private static List<VoteBlockType> PartitionPostByLineTask3(PostToProcess post)
    {
        var voteLines = post.WorkingVote
            .SelectMany(v => v);
        
        var r = VoteBlocks.GetBlocks(voteLines);

        var retaskedLines = r.SelectMany(v => RecursePartitionByLineTask(v, VoteTask.Empty));

        return retaskedLines
            .Select(VoteBlock.Create)
            .Where(v => v != null)
            .Select(v => v!)
            .ToList();

        static IEnumerable<VoteLineType> RecursePartitionByLineTask(VoteBlockType block, VoteTaskType task)
        {
            // Hopefully depth 0, but could be spurious prefix indents
            if (block.All(a => a.Depth == block.Lines[0].Depth))
            {
                // If there's no replacement task, we don't have to change anything
                if (task ==  VoteTask.Empty)
                {
                    return [.. block];
                }

                // If there is a replacement task, replace lines that don't have their own task.
                return block.Select(v => v.Task == VoteTask.Empty ? v with { Task = task } : v)
                    .Select(VoteLine.FullPromote);
            }

            // A content block creates a new task scope.
            if (VoteBlocks.IsThisAContentBlock(block))
            {
                var first = block.Lines[0];
                VoteTaskType passTask = block.Task != VoteTask.Empty ? block.Task : task;

                return [first,
                    .. PartitionBlockForContentBlock(block, PartitionMode.ByBlockAll)
                        .Select(a => RecursePartitionByLineTask(a, passTask))
                        .SelectMany(a => a)];
            }

            // Anything else needs to be broken down into either same-depth groups or content blocks.
            return VoteBlocks.GetBlocks(block)
                .Select(a => RecursePartitionByLineTask(a, task))
                .SelectMany(a => a);
        }
    }
    #endregion Paritioning Posts

    #endregion
}
