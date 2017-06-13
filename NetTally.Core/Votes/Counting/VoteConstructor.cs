﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using NetTally.Extensions;
using NetTally.Utility;
using NetTally.VoteCounting;

namespace NetTally.Votes
{
    /// <summary>
    /// Class that can handle constructing votes (in various manners) from the base text of a post.
    /// </summary>
    public class VoteConstructor
    {
        #region Fields
        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        static readonly Regex basePlanRegex = new Regex(@"base\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase);
        #endregion

        #region Constructor
        public VoteConstructor(IVoteCounter voteCounter)
        {
            VoteCounter = voteCounter ?? throw new ArgumentNullException(nameof(voteCounter));
        }

        public IVoteCounter VoteCounter { get; }
        #endregion

        #region Public functions
        /// <summary>
        /// First pass review of posts to extract and store plans.
        /// In this pass, only plans that have actual content (ie: indented
        /// sub-lines) are considered.
        /// </summary>
        /// <param name="post">Post to be examined.</param>
        /// <param name="quest">Quest being tallied.</param>
        public void PreprocessPlansWithContent(PostComponents post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var plans = GetAllPlansWithContent(post);

            var filteredPlans = FilterPlansByTask(plans, quest);

            StorePlanReferences(filteredPlans);

            ProcessPlans(filteredPlans, post, quest.PartitionMode);
        }

        /// <summary>
        /// Second pass review of posts to extract and store plans.
        /// In this pass, plans that are simply labels for the entire post,
        /// and don't have any content themselves, are considered.
        /// The overall vote must have more than one line, however.
        /// </summary>
        /// <param name="post">Post to be examined.</param>
        /// <param name="quest">Quest being tallied.</param>
        public void PreprocessPlanLabelsWithContent(PostComponents post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (AdvancedOptions.Instance.ForbidVoteLabelPlanNames)
                return;

            var plans = GetAllFullPostPlans(post);

            var filteredPlans = FilterPlansByTask(plans, quest);

            StorePlanReferences(filteredPlans);

            ProcessPlans(filteredPlans, post, quest.PartitionMode);
        }

        /// <summary>
        /// Third pass review of posts to extract and store plans.
        /// In this pass, plans that are simply labels for the entire post,
        /// and don't have any content themselves, are considered.
        /// The overall vote may have just one line.
        /// </summary>
        /// <param name="post">Post to be examined.</param>
        /// <param name="quest">Quest being tallied.</param>
        public void PreprocessPlanLabelsWithoutContent(PostComponents post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (AdvancedOptions.Instance.ForbidVoteLabelPlanNames)
                return;

            var plans = GetAllOneLinePlans(post);

            var filteredPlans = FilterPlansByTask(plans, quest);

            StorePlanReferences(filteredPlans);

            ProcessPlans(filteredPlans, post, quest.PartitionMode);
        }

        /// <summary>
        /// Second pass processing of a post, to handle actual vote processing.
        /// </summary>
        /// <param name="post">The post to process.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>True if the post was processed, false if it was not.</returns>
        public bool ProcessPost(PostComponents post, IQuest quest, CancellationToken token)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (!post.IsVote)
                throw new ArgumentException("Post is not a valid vote.");

            token.ThrowIfCancellationRequested();

            // If the vote has content, deal with it
            if (post.WorkingVote != null && post.WorkingVote.Count > 0)
            {
                // If it has a reference to a plan or voter that has not been processed yet,
                // delay processing.
                if (HasFutureReference(post))
                {
                    VoteCounter.FutureReferences.Add(post);
                    return false;
                }

                // If a newer vote has been registered in the vote counter, that means
                // that this post was a prior future reference that got overridden later.
                // Indicate that it has been processed so that it doesn't try to
                // re-submit it later.
                if (VoteCounter.HasNewerVote(post))
                {
                    return true;
                }

                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<string> votePartitions = GetVotePartitions(post.WorkingVote, quest.PartitionMode, VoteType.Vote, post.Author);

                var filteredPartitions = FilterVotesByTask(votePartitions, quest);

                // Add those to the vote counter.
                VoteCounter.AddVotes(filteredPartitions, post.Author, post.ID, VoteType.Vote);

            }

            // Handle ranking votes, if applicable.
            if (AdvancedOptions.Instance.AllowRankedVotes)
            {
                var rankings = GetRankingsFromPost(post, quest);

                if (rankings.Count > 0)
                    ProcessRankings(rankings, post);
            }

            post.Processed = true;
            return true;
        }

        /// <summary>
        /// Allows partitioning a provided vote (unified vote string) using the specified partition mode.
        /// </summary>
        /// <param name="vote">The vote string.</param>
        /// <param name="quest">The quest, for filter parameters.</param>
        /// <param name="partitionMode">The partition mode to use.</param>
        /// <returns>Returns the partitioned vote as a list of strings.</returns>
        public List<string> PartitionVoteString(string vote, IQuest quest, PartitionMode partitionMode)
        {
            if (string.IsNullOrEmpty(vote))
                return new List<string>();

            var voteLines = vote.GetStringLines();
            return PartitionVoteStrings(voteLines, quest, partitionMode);
        }

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
        /// Get the lines of the vote that we will be processing out of the post.
        /// Only take the .VoteLines, and condense any instances of known plans
        /// to just a reference to the plan name.
        /// </summary>
        /// <param name="post">The post we're getting the vote from.</param>
        /// <returns>Returns the vote with plans compressed.</returns>
        public List<string> GetWorkingVote(PostComponents post)
        {
            List<string> vote = new List<string>();

            if (post == null || !post.IsVote)
                return vote;

            // First determine if any base plans are copies of an original definition, or being defined in this post.
            // If they're just copies, then embed them in the working vote.

            if (post.BasePlans.Any())
            {
                var voters = VoteCounter.GetVotersCollection(VoteType.Plan);
                bool checkPlan = true;
                string planName;

                foreach (var bPlan in post.BasePlans)
                {
                    planName = VoteString.GetMarkedPlanName(bPlan.Key);
                    if (planName == null)
                        continue;

                    // As long as we keep finding base plans that are defined in this post, keep skipping.
                    if (checkPlan)
                    {
                        if (VoteCounter.HasPlan(planName) && voters[planName] == post.ID)
                            continue;
                    }

                    checkPlan = false;

                    // If we reach here, any further plans are copy/pastes of defined plans, and should
                    // have the key added to the working vote.
                    vote.Add(bPlan.Key);
                }
            }

            // Then make sure there are actual vote lines to process.
            if (!post.VoteLines.Any())
                return vote;

            // Then check if the *entire post* should be treated as a complete plan.
            string postPlanName = VoteString.GetPlanName(post.VoteLines.First());
            if (postPlanName != null && VoteCounter.ReferencePlans.ContainsKey(postPlanName) &&
                    VoteCounter.ReferencePlans[postPlanName].Skip(1).SequenceEqual(post.VoteLines.Skip(1), Agnostic.StringComparer))
            {
                // Replace known plans with just the plan key.  They'll be expanded later.
                vote.Add(post.VoteLines.First());
            }
            else
            {
                // If the entire post isn't an auto-plan, break it down into blocks.

                // Break the remainder of the vote into blocks so that we can compare vs auto-plans.
                // Group blocks based on parent vote lines (no prefix).
                // Key for each block is the parent vote line.
                var voteBlocks = post.VoteLines.GroupAdjacentToPreviousKey(
                    (s) => string.IsNullOrEmpty(VoteString.GetVotePrefix(s)),
                    (s) => s,
                    (s) => s);


                foreach (var block in voteBlocks)
                {
                    // Multi-line blocks might be a plan.  Check.
                    if (block.Count() > 1)
                    {
                        // See if the block key marks a known plan.
                        string planName = VoteString.GetPlanName(block.Key);

                        if (planName != null && VoteCounter.ReferencePlans.ContainsKey(planName) &&
                            VoteCounter.ReferencePlans[planName].Skip(1).SequenceEqual(block.Skip(1), Agnostic.StringComparer))
                        {
                            // Replace known plans with just the plan key.  They'll be expanded later.
                            vote.Add(block.Key);
                        }
                        else
                        {
                            // If it's not a known plan, pass everything through.
                            vote.AddRange(block);
                        }
                    }
                    else
                    {
                        // Single lines can be added normally
                        vote.AddRange(block);
                        //vote.Add(block.Key);
                    }
                }
            }

            return vote;
        }
        #endregion

        #region Utility functions for processing plans.        
        /// <summary>
        /// Gets a list of all plans within the post that have defined content (child lines).
        /// </summary>
        /// <param name="post">The post to extract plans from.</param>
        /// <returns>Returns a list of plans (which are lists of vote lines).</returns>
        private List<List<string>> GetAllPlansWithContent(PostComponents post)
        {
            List<List<string>> results = new List<List<string>>();

            results.AddRange(post.BasePlans.Select(a => a.ToList()));

            if (post.VoteLines.Any())
            {
                // Group blocks based on parent vote lines (no prefix).
                // Key for each block is the parent vote line.
                var voteBlocks = post.VoteLines.GroupAdjacentToPreviousKey(
                    (s) => string.IsNullOrEmpty(VoteString.GetVotePrefix(s)),
                    (s) => s,
                    (s) => s);

                foreach (var block in voteBlocks)
                {
                    if (block.Count() > 1)
                    {
                        string planname = VoteString.GetPlanName(block.Key);

                        if (planname != null)
                        {
                            // Add a named vote that is named after a user only if it matches the post author's name.
                            if (VoteCounter.ReferenceVoters.Contains(planname, Agnostic.StringComparer))
                            {
                                if (Agnostic.StringComparer.Equals(planname, post.Author))
                                {
                                    results.Add(block.ToList());
                                }
                            }
                            else
                            {
                                // If it's not named after a user, add it normally.
                                results.Add(block.ToList());
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets a list of all full-vote plans (of which there will only be one, if found).
        /// </summary>
        /// <param name="post">The post to extract plans from.</param>
        /// <returns>Returns a list of plans (which are lists of vote lines).</returns>
        private List<List<string>> GetAllFullPostPlans(PostComponents post)
        {
            List<List<string>> results = new List<List<string>>();

            if (post.VoteLines.Any())
            {
                // Group blocks based on parent vote lines (no prefix).
                // Key for each block is the parent vote line.
                var voteBlocks = post.VoteLines.GroupAdjacentToPreviousKey(
                    (s) => string.IsNullOrEmpty(VoteString.GetVotePrefix(s)),
                    (s) => s,
                    (s) => s);

                // If the vote has any plans with content in them, we can't make this a full-post plan.
                if (!voteBlocks.Any(b => b.Count() > 1 && VoteString.GetPlanName(b.Key) != null))
                {
                    // The post must have more than one line to count for a plan label.
                    if (post.VoteLines.Count > 1)
                    {
                        var firstLine = post.VoteLines.First();

                        string planname = VoteString.GetPlanName(firstLine);

                        if (planname != null)
                        {
                            // If it's named after a user, it must be the post author.  Otherwise, anything is fine.
                            if (VoteCounter.ReferenceVoters.Contains(planname, Agnostic.StringComparer))
                            {
                                if (Agnostic.StringComparer.Equals(planname, post.Author))
                                {
                                    results.Add(post.VoteLines);
                                }
                            }
                            else
                            {
                                results.Add(post.VoteLines);
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets a list of all full-vote plans (of which there will only be one, if found).
        /// </summary>
        /// <param name="post">The post to extract plans from.</param>
        /// <returns>Returns a list of plans (which are lists of vote lines).</returns>
        private List<List<string>> GetAllOneLinePlans(PostComponents post)
        {
            List<List<string>> results = new List<List<string>>();

            if (post.VoteLines.Any())
            {
                // Group blocks based on parent vote lines (no prefix).
                // Key for each block is the parent vote line.
                var voteBlocks = post.VoteLines.GroupAdjacentToPreviousKey(
                    (s) => string.IsNullOrEmpty(VoteString.GetVotePrefix(s)),
                    (s) => s,
                    (s) => s);

                foreach (var block in voteBlocks)
                {
                    if (block.Count() == 1)
                    {
                        string planname = VoteString.GetPlanName(block.Key);

                        if (planname != null)
                        {
                            if (VoteCounter.ReferenceVoters.Contains(planname, Agnostic.StringComparer))
                            {
                                if (Agnostic.StringComparer.Equals(planname, post.Author))
                                {
                                    results.Add(block.ToList());
                                }
                            }
                            else
                            {
                                results.Add(block.ToList());
                            }
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Store original plan name and contents in reference containers.
        /// </summary>
        /// <param name="plans">A list of valid plans.</param>
        private void StorePlanReferences(IEnumerable<List<string>> plans)
        {
            foreach (var plan in plans)
            {
                string planName = VoteString.GetPlanName(plan.First());
                string cleanName = VoteString.RemoveBBCode(planName);
                cleanName = VoteString.DeUrlContent(cleanName);


                if (!VoteCounter.ReferencePlanNames.Contains(cleanName, Agnostic.StringComparer))
                {
                    VoteCounter.ReferencePlanNames.Add(cleanName);
                    VoteCounter.ReferencePlans[cleanName] = plan;
                }
            }
        }

        /// <summary>
        /// Put any plans found in the grouped vote lines into the standard tracking sets,
        /// after handling any partitioning needed.
        /// </summary>
        /// <param name="plans">List of plans to be processed.</param>
        /// <param name="post">Post the plans were pulled from.</param>
        /// <param name="partitionMode">Partition mode being used.</param>
        private void ProcessPlans(IEnumerable<List<string>> plans, PostComponents post, PartitionMode partitionMode)
        {
            foreach (var plan in plans)
            {
                string planName = VoteString.GetMarkedPlanName(plan.First());
                string cleanName = VoteString.RemoveBBCode(planName);
                cleanName = VoteString.DeUrlContent(cleanName);

                if (!VoteCounter.HasPlan(cleanName))
                {
                    var nPlan = NormalizePlanName(plan);

                    // Get the list of all vote partitions, built according to current preferences.
                    // One of: By line, By block, or By post (ie: entire vote)
                    var votePartitions = GetVotePartitions(nPlan, partitionMode, VoteType.Plan, post.Author);

                    VoteCounter.AddVotes(votePartitions, cleanName, post.ID, VoteType.Plan);
                }
            }
        }
        #endregion

        #region Utility functions for processing votes.

        /// <summary>
        /// Determine if there are any references to future (unprocessed) votes
        /// within the current vote.
        /// </summary>
        /// <param name="post">Post containing the current vote.</param>
        /// <returns>Returns true if a future reference is found. Otherwise false.</returns>
        private bool HasFutureReference(PostComponents post)
        {
            // If we decide it has to be forced, ignore all checks in here.
            if (post.ForceProcess)
                return false;

            // If proxy votes are disabled, we don't need to look for proxy names, so there can't be future references.
            // Likewise, if we're forcing all proxy votes to be pinned, there can't be any future references.
            if (AdvancedOptions.Instance.DisableProxyVotes || AdvancedOptions.Instance.ForcePinnedProxyVotes)
                return false;

            foreach (var line in post.WorkingVote)
            {
                // Get the possible proxy references this line contains
                var refNames = VoteString.GetVoteReferenceNames(line);

                // Pinned references (^ or pin keywords) are explicitly not future references
                if (refNames[ReferenceType.Label].Any(a => a == "^" || a == "pin"))
                    continue;

                // Any references to plans automatically work, as they are defined in a preprocess phase.
                if (refNames[ReferenceType.Plan].Any(VoteCounter.HasPlan))
                    continue;

                string refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => VoteCounter.ReferenceVoters.Contains(n, Agnostic.StringComparer))
                    ?.AgnosticMatch(VoteCounter.ReferenceVoters);

                if (refVoter != null && refVoter != post.Author)
                {
                    var refVoterPosts = VoteCounter.PostsList.Where(p => p.Author == refVoter).ToList();

                    // If ref voter has no posts (how did we get here?), it can't be a future reference.
                    if (!refVoterPosts.Any())
                        continue;

                    // If the referenced voter never made a real vote (eg: only made base plans or rank votes),
                    // then this can't be treated as a future reference.
                    var refWorkingVotes = refVoterPosts.Where(p => p.WorkingVote.Count > 0);

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
        /// Partition the vote and store the vote and voter.
        /// </summary>
        /// <param name="post">The post it was derived from.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        private void ProcessVote(PostComponents post, PartitionMode partitionMode)
        {
            // Get the list of all vote partitions, built according to current preferences.
            // One of: By line, By block, or By post (ie: entire vote)
            List<string> votePartitions = GetVotePartitions(post.WorkingVote, partitionMode, VoteType.Vote, post.Author);

            VoteCounter.AddVotes(votePartitions, post.Author, post.ID, VoteType.Vote);
        }

        /// <summary>
        /// Filters the plans by task.
        /// </summary>
        /// <param name="plans">The plans.</param>
        /// <param name="taskFilter">The task filter.</param>
        /// <returns>Returns the plans after filtering with the task filter.</returns>
        private static List<List<string>> FilterPlansByTask(List<List<string>> plans, IQuest quest)
        {
            if (!quest.UseCustomTaskFilters)
                return plans;

            // Include lines where the task filter matches
            var filtered = plans.Where(p => quest.TaskFilter.Match(VoteString.GetVoteTask(p.First())));

            return filtered.ToList();
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
                bool check = quest.TaskFilter.Match(task);
                if (check)
                    results.Add(line);
            }

            return results;
        }

        #endregion

        #region Utility functions for processing ranked votes.
        /// <summary>
        /// Get the ranking lines from a post.
        /// May pull either the direct values, if provided, or copy a referenced
        /// users vote if available.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns>
        /// Returns any ranked vote lines in the vote.
        /// </returns>
        private List<string> GetRankingsFromPost(PostComponents post, IQuest quest)
        {
            // If there are any explicit rank vote lines, return those.
            if (post.RankLines.Any())
                return FilterVotesByTask(post.RankLines, quest);

            // If there were no explicit rankings, see if there's a reference to
            // another voter as the only line of this vote.
            string refName = GetPureRankReference(post);

            if (refName != null)
            {
                // If so, see if that voter made any rank votes.
                var indirect = VoteCounter.GetVotesCollection(VoteType.Rank).Where(r => r.Value.Contains(refName)).Select(v => v.Key);

                // If so, return those votes.
                if (indirect.Any())
                    return indirect.ToList();
            }

            // Otherwise, there are no rankings for this vote.
            return new List<string>();
        }

        /// <summary>
        /// Get the name of a voter that is referenced if that is the only
        /// reference in the vote.
        /// </summary>
        /// <param name="post">The post.</param>
        /// <returns></returns>
        private string GetPureRankReference(PostComponents post)
        {
            if (post.VoteLines.Count == 1)
            {
                var refNames = VoteString.GetVoteReferenceNames(post.VoteLines.First());

                var refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => n != post.Author && VoteCounter.HasUserEnteredVoter(n, VoteType.Rank));

                return refVoter;
            }

            return null;
        }

        /// <summary>
        /// Put any ranking votes found in the grouped vote lines into the standard tracking sets.
        /// </summary>
        /// <param name="ranksList">A list of all rank votes in the post.</param>
        /// <param name="post">The components of the original post.</param>
        private void ProcessRankings(List<string> ranksList, PostComponents post)
        {
            if (ranksList.Count > 0)
            {
                VoteCounter.AddVotes(ranksList, post.Author, post.ID, VoteType.Rank);
            }
        }

        #endregion

        #region Partitioning handling
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

                referralVotes = VoteCounter.GetVotesFromReference(line, author);

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

                referralVotes = VoteCounter.GetVotesFromReference(line, author);

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

                referralVotes = VoteCounter.GetVotesFromReference(line, author);

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

                referralVotes = VoteCounter.GetVotesFromReference(line, author);

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
        #endregion

        #region Functions dealing with plan formatting.
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

        /// <summary>
        /// Takes a list of string lines and, if the first line contains a plan
        /// name using "Base Plan", convert it to a version that only uses "Plan".
        /// </summary>
        /// <param name="lines">A list of lines defining a plan.</param>
        /// <returns>Returns the list of lines, with the assurance that
        /// any plan name starts with just "Plan".</returns>
        private static IEnumerable<string> NormalizePlanName(IEnumerable<string> lines)
        {
            string firstLine = lines.First();
            var remainder = lines.Skip(1);

            string nameContent = VoteString.GetVoteContent(firstLine, VoteType.Plan);

            Match m = basePlanRegex.Match(nameContent);
            if (m.Success)
            {
                nameContent = $"Plan{m.Groups[1]}{m.Groups["planname"]}";

                firstLine = VoteString.ModifyVoteLine(firstLine, content: nameContent);

                List<string> results = new List<string>(lines.Count()) { firstLine };
                results.AddRange(remainder);

                return results;
            }

            return lines;
        }
        #endregion
    }
}
