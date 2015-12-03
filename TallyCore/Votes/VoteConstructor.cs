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
        IVoteCounter VoteCounter { get; }

        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        readonly Regex basePlanRegex = new Regex(@"base\s*plan((:|\s)+)(?<planname>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference.
        readonly Regex anyPlanRegex = new Regex(@"^(base\s*)?plan(:|\s)+◈?(?<planname>.+)\.?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="voteCounter">An IVoteCounter must be provided to the constructor.</param>
        public VoteConstructor(IVoteCounter voteCounter)
        {
            if (voteCounter == null)
                throw new ArgumentNullException(nameof(voteCounter));

            VoteCounter = voteCounter;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// First pass review of posts to extract and store plans.
        /// </summary>
        /// <param name="post">Post to be examined.</param>
        /// <param name="quest">Quest being tallied.</param>
        public void PreprocessPlans(PostComponents post, IQuest quest)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var plans = post.GetAllPlans();

            StorePlans(plans);

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
                    VoteCounter.FutureReferences.Add(post);
                    return false;
                }

                // Process the actual vote.
                ProcessVote(post, quest.PartitionMode);
            }

            // Handle ranking votes, if applicable.
            if (quest.AllowRankedVotes)
            {
                var rankings = GetRankingsFromPost(post);

                if (rankings.Count > 0)
                    ProcessRankings(rankings, post, quest.PartitionMode);
            }

            return true;
        }
        #endregion

        #region Utility functions for processing plans.
        /// <summary>
        /// Store original plan name and contents in reference containers.
        /// </summary>
        /// <param name="plans">A list of valid plans.</param>
        private void StorePlans(IEnumerable<List<string>> plans)
        {
            foreach (var plan in plans)
            {
                string planName = VoteString.GetPlanName(plan.First());

                if (!VoteCounter.ReferencePlanNames.Contains(planName))
                {
                    VoteCounter.ReferencePlanNames.Add(planName);
                    VoteCounter.ReferencePlans[planName] = plan;
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

                if (!VoteCounter.HasPlan(planName))
                {
                    var nPlan = NormalizePlanName(plan);

                    // Get the list of all vote partitions, built according to current preferences.
                    // One of: By line, By block, or By post (ie: entire vote)
                    var votePartitions = GetVotePartitions(nPlan, partitionMode, VoteType.Plan, post.Author);

                    VoteCounter.AddVotes(votePartitions, planName, post.ID, VoteType.Plan);
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

            // Break the remainder of the vote into blocks so that we can compare vs auto-plans.
            var voteBlocks = post.VoteLines.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

            foreach (var block in voteBlocks)
            {
                // Multi-line blocks might be a plan.  Check.
                if (block.Count() > 1)
                {
                    // See if the block key marks a known plan.
                    string planName = VoteString.GetPlanName(block.Key);

                    if (planName != null && VoteCounter.ReferencePlans.ContainsKey(planName) &&
                        VoteCounter.ReferencePlans[planName].Skip(1).SequenceEqual(block.Skip(1), Text.AgnosticStringComparer))
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
        private string SelectSubLines(string line)
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
        private string NonNullSelectSubLines(string line) => line ?? "Key";

        /// <summary>
        /// Determine if there are any references to future (unprocessed) votes
        /// within the current vote.
        /// </summary>
        /// <param name="vote">List of lines for the current vote.</param>
        /// <returns>Returns true if a future reference is found. Otherwise false.</returns>
        private bool HasFutureReference(PostComponents post)
        {
            var voters = VoteCounter.GetVotersCollection(VoteType.Vote);

            foreach (var line in post.WorkingVote)
            {
                // Exclude plan name marker references.
                var refNames = VoteString.GetVoteReferenceNames(line);

                // Any references to plans automatically work.
                if (refNames[ReferenceType.Plan].Any(p => VoteCounter.HasPlan(p)))
                    continue;

                string refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => VoteCounter.ReferenceVoters.Contains(n));

                if (refVoter != null && refVoter != post.Author)
                {
                    // If there's no vote entry, it must necessarily be a future reference.
                    if (!VoteCounter.HasVoter(refVoter, VoteType.Vote))
                        return true;

                    // Regex to check if there's a leading 'plan' notation
                    string contents = VoteString.GetVoteContent(line);
                    Match m = anyPlanRegex.Match(contents);
                    if (!m.Success)
                    {
                        // If it doesn't have a leading 'plan', we need to know whether the
                        // last vote the referenced voter made has been tallied.
                        if (voters[refVoter] != VoteCounter.ReferenceVoterPosts[refVoter])
                            return true;
                    }
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
        private void ProcessVote(PostComponents post, PartitionMode partitionMode)
        {
            // Get the list of all vote partitions, built according to current preferences.
            // One of: By line, By block, or By post (ie: entire vote)
            List<string> votePartitions = GetVotePartitions(post.WorkingVote, partitionMode, VoteType.Vote, post.Author);

            VoteCounter.AddVotes(votePartitions, post.Author, post.ID, VoteType.Vote);
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
        private List<string> GetRankingsFromPost(PostComponents post)
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
        /// <param name="voteStrings">The standard vote partitions.</param>
        /// <returns></returns>
        private string GetPureRankReference(PostComponents post)
        {
            if (post.VoteLines.Count == 1)
            {
                var refNames = VoteString.GetVoteReferenceNames(post.VoteLines.First());

                var refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => n != post.Author && VoteCounter.HasVoter(n, VoteType.Rank));

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
        private void ProcessRankings(List<string> ranksList, PostComponents post, PartitionMode partitionMode)
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
        private List<string> GetVotePartitionsFromRank(IEnumerable<string> lines, PartitionMode partitionMode, string author)
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
                case PartitionMode.ByBlock:
                    // When partitioning by block, plans are kept whole.  No partitioning here.
                    return PartitionByNone(lines, author);
                case PartitionMode.ByPlanBlock:
                    // When partitioning by PlanBlock, the plan is partitioned by block after promotion.
                    // Make sure to preserve the task from the main line on the resulting blocks.
                    string planTask = VoteString.GetVoteTask(lines.First());
                    var blocks = PartitionByBlock(PromoteLines(lines), author);
                    if (planTask != string.Empty)
                        blocks = ApplyTaskToBlocks(blocks, planTask);
                    return blocks;
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
                case PartitionMode.ByBlock:
                    // Partition by block; no special treatment at the vote level
                    return PartitionByBlock(lines, author);
                case PartitionMode.ByPlanBlock:
                    // Plan/Block partitioning means the plan is partitioned by block.
                    // The vote is also partitioned by block.
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

            foreach (string line in lines)
            {
                List<string> referralVotes = VoteCounter.GetVotesFromReference(line, author);

                if (referralVotes.Count > 0)
                {
                    foreach (var referral in referralVotes)
                        sb.Append(referral);
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

            foreach (string line in lines)
            {
                List<string> referralVotes = VoteCounter.GetVotesFromReference(line, author);

                if (referralVotes.Count > 0)
                {
                    partitions.AddRange(referralVotes);
                }
                else
                {
                    partitions.Add(line + "\r\n");
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
            StringBuilder sb = new StringBuilder();

            foreach (string line in lines)
            {
                List<string> referralVotes = VoteCounter.GetVotesFromReference(line, author);

                if (referralVotes.Count > 0)
                {
                    if (sb.Length > 0)
                    {
                        partitions.Add(sb.ToString());
                        sb.Clear();
                    }

                    partitions.AddRange(referralVotes);
                }
                else
                {
                    string prefix = VoteString.GetVotePrefix(line);

                    // If we encountered a new top-level vote line, store any existing stringbuilder contents.
                    if (prefix == string.Empty && sb.Length > 0)
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
        /// <param name="planTask">A task name to apply.</param>
        /// <returns>Returns the vote blocks with the task applied.</returns>
        private List<string> ApplyTaskToBlocks(List<string> blocks, string planTask)
        {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));
            if (string.IsNullOrEmpty(planTask))
                throw new ArgumentNullException(nameof(planTask));

            List<string> results = new List<string>();

            foreach (var block in blocks)
            {
                if (VoteString.GetVoteTask(block) == string.Empty)
                {
                    string rep = VoteString.ModifyVoteLine(block, task: planTask, ByPartition: true);
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
        private IEnumerable<string> PromoteLines(IEnumerable<string> lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            var remainder = lines.Skip(1);

            if (remainder.All(l => VoteString.GetVotePrefix(l) != string.Empty))
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
        private IEnumerable<string> NormalizePlanName(IEnumerable<string> lines)
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
