using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally
{
    /// <summary>
    /// Class that can handle constructing votes (in various manners) from the base text of a post.
    /// </summary>
    public class VoteConstructor
    {
        #region Constructor and vars
        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        private static readonly Regex basePlanRegex = new Regex(@"base\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase);
        #endregion

        #region Public functions
        /// <summary>
        /// First pass review of posts to extract and store plans.
        /// In this pass, only plans that have actual content (ie: indented
        /// sub-lines) are considered.
        /// </summary>
        /// <param name="post">Post to be examined.</param>
        /// <param name="quest">Quest being tallied.</param>
        public void PreprocessPlansPhase1(PostComponents post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var plans = post.GetAllPlansWithContent();

            StorePlanReferences(plans);

            ProcessPlans(plans, post, quest.PartitionMode);
        }

        /// <summary>
        /// Second pass review of posts to extract and store plans.
        /// In this pass, plans that are simply labels for the entire post,
        /// and don't have any content themselves, are considered.
        /// </summary>
        /// <param name="post">Post to be examined.</param>
        /// <param name="quest">Quest being tallied.</param>
        public void PreprocessPlansPhase2(PostComponents post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var plans = post.GetAllFullPostPlans();

            StorePlanReferences(plans);

            ProcessPlans(plans, post, quest.PartitionMode);
        }

        /// <summary>
        /// Second pass processing of a post, to handle actual vote processing.
        /// </summary>
        /// <param name="post">The post to process.</param>
        /// <param name="quest">The quest being tallied.</param>
        public bool ProcessPost(PostComponents post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (!post.IsVote)
                throw new ArgumentException("Post is not a valid vote.");

            // If the vote has content, deal with it
            if (post.WorkingVote != null && post.WorkingVote.Count > 0)
            {
                // If it has a reference to a plan or voter that has not been processed yet,
                // delay processing.
                if (HasFutureReference(post))
                {
                    VoteCounter.Instance.FutureReferences.Add(post);
                    return false;
                }

                // If a newer vote has been registered in the vote counter, that means
                // that this post was a prior future reference that got overridden later.
                // Indicate that it has been processed so that it doesn't try to
                // re-submit it later.
                if (VoteCounter.Instance.HasNewerVote(post))
                {
                    return true;
                }

                // Process the actual vote.
                ProcessVote(post, quest.PartitionMode);
            }

            // Handle ranking votes, if applicable.
            if (AdvancedOptions.Instance.AllowRankedVotes)
            {
                var rankings = GetRankingsFromPost(post);

                if (rankings.Count > 0)
                    ProcessRankings(rankings, post);
            }

            post.Processed = true;
            return true;
        }
        #endregion

        #region Utility functions for processing plans.
        /// <summary>
        /// Store original plan name and contents in reference containers.
        /// </summary>
        /// <param name="plans">A list of valid plans.</param>
        private static void StorePlanReferences(IEnumerable<List<string>> plans)
        {
            foreach (var plan in plans)
            {
                string planName = VoteString.GetPlanName(plan.First());

                if (!VoteCounter.Instance.ReferencePlanNames.Contains(planName, StringUtility.AgnosticStringComparer))
                {
                    VoteCounter.Instance.ReferencePlanNames.Add(planName);
                    VoteCounter.Instance.ReferencePlans[planName] = plan;
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
        private static void ProcessPlans(IEnumerable<List<string>> plans, PostComponents post, PartitionMode partitionMode)
        {
            foreach (var plan in plans)
            {
                string planName = VoteString.GetMarkedPlanName(plan.First());

                if (!VoteCounter.Instance.HasPlan(planName))
                {
                    var nPlan = NormalizePlanName(plan);

                    // Get the list of all vote partitions, built according to current preferences.
                    // One of: By line, By block, or By post (ie: entire vote)
                    var votePartitions = GetVotePartitions(nPlan, partitionMode, VoteType.Plan, post.Author);

                    VoteCounter.Instance.AddVotes(votePartitions, planName, post.ID, VoteType.Plan);
                }
            }
        }
        #endregion

        #region Utility functions for processing votes.
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
                var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Plan);
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
                        if (VoteCounter.Instance.HasPlan(planName) && voters[planName] == post.ID)
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
            if (postPlanName != null && VoteCounter.Instance.ReferencePlans.ContainsKey(postPlanName) &&
                    VoteCounter.Instance.ReferencePlans[postPlanName].Skip(1).SequenceEqual(post.VoteLines.Skip(1), StringUtility.AgnosticStringComparer))
            {
                // Replace known plans with just the plan key.  They'll be expanded later.
                vote.Add(post.VoteLines.First());
            }
            else
            {
                // If the entire post isn't an auto-plan, break it down into blocks.

                // Break the remainder of the vote into blocks so that we can compare vs auto-plans.
                var voteBlocks = post.VoteLines.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

                foreach (var block in voteBlocks)
                {
                    // Multi-line blocks might be a plan.  Check.
                    if (block.Count() > 1)
                    {
                        // See if the block key marks a known plan.
                        string planName = VoteString.GetPlanName(block.Key);

                        if (planName != null && VoteCounter.Instance.ReferencePlans.ContainsKey(planName) &&
                            VoteCounter.Instance.ReferencePlans[planName].Skip(1).SequenceEqual(block.Skip(1), StringUtility.AgnosticStringComparer))
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

        /// <summary>
        /// Utility function to determine whether adjacent lines should
        /// be grouped together.
        /// Creates a grouping key for the provided line.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>Returns the line as the key if it's not a sub-vote line.
        /// Otherwise returns null.</returns>
        private static string SelectSubLines(string line)
        {
            string prefix = VoteString.GetVotePrefix(line);
            if (string.IsNullOrEmpty(prefix))
                return line;
            else
                return null;
        }

        /// <summary>
        /// Supplementary function for line grouping, in the event that the first
        /// line of the vote is indented (and thus would normally generate a null key).
        /// </summary>
        /// <param name="line">The line to generate a key for.</param>
        /// <returns>Returns the line, or "Key", as the key for a line.</returns>
        private static string NonNullSelectSubLines(string line) => line ?? "Key";

        /// <summary>
        /// Determine if there are any references to future (unprocessed) votes
        /// within the current vote.
        /// </summary>
        /// <param name="vote">List of lines for the current vote.</param>
        /// <returns>Returns true if a future reference is found. Otherwise false.</returns>
        private static bool HasFutureReference(PostComponents post)
        {
            // If we decide it has to be forced, ignore all checks in here.
            if (post.ForceProcess)
                return false;

            foreach (var line in post.WorkingVote)
            {
                // Exclude plan name marker references.
                var refNames = VoteString.GetVoteReferenceNames(line);

                // Any references to plans automatically work.
                if (refNames[ReferenceType.Plan].Any(VoteCounter.Instance.HasPlan))
                    continue;

                string refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => VoteCounter.Instance.ReferenceVoters.Contains(n, StringUtility.AgnosticStringComparer))
                    ?.AgnosticMatch(VoteCounter.Instance.ReferenceVoters);

                if (refVoter != null && refVoter != post.Author)
                {
                    var refVoterPosts = VoteCounter.Instance.PostsList.Where(p => p.Author == refVoter).ToList();

                    // If ref voter has no posts (how did we get here?), can't be a future reference.
                    if (!refVoterPosts.Any())
                        return false;

                    // If the referenced voter never made a real vote (eg: only made base plans or rank votes),
                    // then this can't be treated as a future reference.
                    var refWorkingVotes = refVoterPosts.Where(p => p.WorkingVote.Count > 0);

                    if (!refWorkingVotes.Any())
                    {
                        return false;
                    }

                    // If the reference name included 'plan', then we use what's available at the time of this post.
                    // 'plan' indicates it's a pinned reference, and is stored in the Label slot if found.
                    if (refNames[ReferenceType.Label].Any())
                    {
                        // If we've processed a vote for the ref voter, that's what will be used.
                        if (VoteCounter.Instance.HasVoter(refVoter, VoteType.Vote))
                            return false;
                    }

                    // If there's no 'plan' label, then we need to verify that the last vote that the
                    // ref voter made (has a working vote) was processed.
                    // If it's been processed, then we're OK to let this vote through.
                    if (refWorkingVotes.Last().Processed)
                        return false;

                    // If none of the conditions above are met, then consider this a future reference.
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Partition the vote and store the vote and voter.
        /// </summary>
        /// <param name="vote">The vote to process.</param>
        /// <param name="post">The post it was derived from.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        private static void ProcessVote(PostComponents post, PartitionMode partitionMode)
        {
            // Get the list of all vote partitions, built according to current preferences.
            // One of: By line, By block, or By post (ie: entire vote)
            List<string> votePartitions = GetVotePartitions(post.WorkingVote, partitionMode, VoteType.Vote, post.Author);

            VoteCounter.Instance.AddVotes(votePartitions, post.Author, post.ID, VoteType.Vote);
        }

        #endregion

        #region Utility functions for processing ranked votes.
        /// <summary>
        /// Get the ranking lines from a post.
        /// May pull either the direct values, if provided, or copy a referenced
        /// users vote if available.
        /// </summary>
        /// <param name="voteStrings">The vote being checked.</param>
        /// <returns>Returns any ranked vote lines in the vote.</returns>
        private static List<string> GetRankingsFromPost(PostComponents post)
        {
            // If there are any explicit rank vote lines, return those.
            if (post.RankLines.Any())
                return post.RankLines;

            // If there were no explicit rankings, see if there's a reference to
            // another voter as the only line of this vote.
            string refName = GetPureRankReference(post);

            if (refName != null)
            {
                // If so, see if that voter made any rank votes.
                var indirect = VoteCounter.Instance.GetVotesCollection(VoteType.Rank).Where(r => r.Value.Contains(refName)).Select(v => v.Key);

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
        /// <param name="voteStrings">The standard vote partitions.</param>
        /// <returns></returns>
        private static string GetPureRankReference(PostComponents post)
        {
            if (post.VoteLines.Count == 1)
            {
                var refNames = VoteString.GetVoteReferenceNames(post.VoteLines.First());

                var refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => n != post.Author && VoteCounter.Instance.HasVoter(n, VoteType.Rank));

                return refVoter;
            }

            return null;
        }

        /// <summary>
        /// Put any ranking votes found in the grouped vote lines into the standard tracking sets.
        /// </summary>
        /// <param name="ranksList">A list of all rank votes in the post.</param>
        /// <param name="post">The components of the original post.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        private static void ProcessRankings(List<string> ranksList, PostComponents post)
        {
            if (ranksList.Count > 0)
            {
                VoteCounter.Instance.AddVotes(ranksList, post.Author, post.ID, VoteType.Rank);
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
        private static List<string> GetVotePartitions(IEnumerable<string> lines, PartitionMode partitionMode, VoteType voteType, string author)
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
        private static List<string> GetVotePartitionsFromPlan(IEnumerable<string> lines, PartitionMode partitionMode, string author)
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
        private static List<string> GetVotePartitionsFromVote(IEnumerable<string> lines, PartitionMode partitionMode, string author)
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
        private static List<string> PartitionByNone(IEnumerable<string> lines, string author)
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
                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = VoteCounter.Instance.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    foreach (var referral in referralVotes)
                        sb.Append(referral);

                    referralVotes = referralVotes.SelectMany(a => StringUtility.GetStringLines(a)).ToList();
                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
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
        private static List<string> PartitionByLine(IEnumerable<string> lines, string author)
        {
            List<string> partitions = new List<string>();
            List<string> referralVotes = new List<string>();

            foreach (string line in lines)
            {
                // If someone copy/pasted a vote with a referral at the top (eg: self-named plan),
                // skip the copy/pasted section.
                if (referralVotes.Any())
                {
                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = VoteCounter.Instance.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    partitions.AddRange(referralVotes);

                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
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
        private static List<string> PartitionByLineTask(IEnumerable<string> lines, string author)
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
                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = VoteCounter.Instance.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    partitions.AddRange(referralVotes);

                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
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
        private static List<string> PartitionByBlock(IEnumerable<string> lines, string author)
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
                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
                    {
                        referralVotes = referralVotes.Skip(1).ToList();
                        continue;
                    }

                    referralVotes.Clear();
                }

                referralVotes = VoteCounter.Instance.GetVotesFromReference(line, author);

                if (referralVotes.Any())
                {
                    if (sb.Length > 0)
                    {
                        partitions.Add(sb.ToString());
                        sb.Clear();
                    }

                    partitions.AddRange(referralVotes);

                    referralVotes = referralVotes.SelectMany(a => StringUtility.GetStringLines(a)).ToList();
                    if (StringUtility.AgnosticStringComparer.Equals(line, referralVotes.First()))
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
