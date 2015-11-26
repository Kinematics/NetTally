using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        readonly Regex basePlanRegex = new Regex(@"base\s*plan(:|\s)+(?<planname>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference.
        readonly Regex planRegex = new Regex(@"^plan(:|\s)+(?<planname>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference.
        readonly Regex anyPlanRegex = new Regex(@"^(base\s*)?plan(:|\s)+(?<planname>.+)\.?$", RegexOptions.IgnoreCase);
        readonly Regex anyPlanReference = new Regex(@"^((base\s*)?plan(:|\s)+)?(?<reference>.+)\.?$", RegexOptions.IgnoreCase);
        // Potential reference to another user's plan.
        readonly Regex planNameRegex = new Regex(@"^(?<label>base\s*plan(:|\s)+)?(?<reference>.+)", RegexOptions.IgnoreCase);

        readonly List<string> formattingTags = new List<string>() { "color", "b", "i", "u" };
        readonly Dictionary<string, Regex> rxStart = new Dictionary<string, Regex>();
        readonly Dictionary<string, Regex> rxEnd = new Dictionary<string, Regex>();

        /// <summary>
        /// Setup some dictionary lists for validating vote formatting.
        /// </summary>
        private void SetupFormattingRegexes()
        {
            foreach (var tag in formattingTags)
            {
                if (tag == "color")
                    rxStart[tag] = new Regex(string.Concat(@"\[", tag, @"=([^]]*)\]"));
                else
                    rxStart[tag] = new Regex(string.Concat(@"\[", tag, @"\]"));

                rxEnd[tag] = new Regex(string.Concat(@"\[/", tag, @"\]"));
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="voteCounter">An IVoteCounter must be provided to the constructor.</param>
        public VoteConstructor(IVoteCounter voteCounter)
        {
            if (voteCounter == null)
                throw new ArgumentNullException(nameof(voteCounter));

            VoteCounter = voteCounter;
            SetupFormattingRegexes();
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

            var plans = GetPlansFromPost(post.VoteStrings);

            // Any plans with only a single line attached to the name are invalid (possibly normal vote references).
            var validPlans = plans.Where(p => p.Count > 1);

            if (validPlans.Any())
            {
                StorePlans(validPlans);

                ProcessPlans(validPlans, post, quest.PartitionMode);
            }
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

            // Get the lines of the post that correspond to the vote.
            var vote = GetVoteFromPost(post.VoteStrings);

            // If it has a reference to a plan or voter that has not been processed yet,
            // delay processing.
            if (HasFutureReference(vote))
                return false;

            // Process the actual vote.
            ProcessVote(vote, post, quest.PartitionMode);

            // Handle ranking votes, if applicable.
            if (quest.AllowRankedVotes)
            {
                var rankings = GetRankingsFromPost(post.VoteStrings);

                if (rankings.Count > 0)
                    ProcessRankings(rankings, post, quest.PartitionMode);
            }

            return true;
        }
        #endregion

        #region Utility functions for processing plans.
        /// <summary>
        /// Given the lines of a vote, extract all base plans and auto-plans from them.
        /// A plan is a block that starts with a line saying, "Plan" or "Base Plan".
        /// There is no necessary ordering for plan blocks vs other vote lines.
        /// </summary>
        /// <param name="postStrings">The lines of the vote.</param>
        /// <returns>Returns a list of any found plans, with each plan being
        /// the list of vote lines that make it up.</returns>
        private List<List<string>> GetPlansFromPost(List<string> postStrings)
        {
            List<List<string>> results = new List<List<string>>();
            List<string> plan = null;

            foreach (var line in postStrings)
            {
                string prefix = VoteString.GetVotePrefix(line);

                // If there's no prefix on this vote line, we're starting a new block.
                // If we had any open plans, close and save them.
                // Then see if we need to start a new plan.
                if (prefix == string.Empty)
                {
                    if (plan != null)
                    {
                        results.Add(plan);
                        plan = null;
                    }

                    string planname = GetPlanName(line);

                    if (planname != null)
                    {
                        // Make sure the plan doesn't already exist in the tracker.
                        // If it does, this counts as a repeat, and should be considered an attempt
                        // to reference the original plan, rather than redefine it.
                        // As soon as this occurs, we should treat all further lines
                        // as regular vote lines, rather than additional potential plans.
                        if (!VoteCounter.HasPlan(planname))
                        {
                            // If it's a new base plan, add the first line to a new base plan list,
                            // and add the newly created list to the results.
                            plan = new List<string>() { line };
                        }
                    }
                }
                else
                {
                    // If we have any open plan, add sub-vote lines to it.
                    plan?.Add(line);
                }
            }

            // If we reached the end of a vote with an active plan, make sure to save it.
            if (plan != null)
            {
                results.Add(plan);
            }

            return results;
        }

        /// <summary>
        /// Store original plan name and contents in reference containers.
        /// </summary>
        /// <param name="plans">A list of valid plans.</param>
        private void StorePlans(IEnumerable<List<string>> plans)
        {
            foreach (var plan in plans)
            {
                string planName = GetPlanName(plan.First());

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
                string planName = GetMarkedPlanName(plan.First());

                if (!VoteCounter.HasPlan(planName))
                {
                    // Add/update the plan's post ID.
                    VoteCounter.AddVoterPostID(planName, post.ID, VoteType.Plan);

                    List<string> planLines = PromotePlanLines(plan.Skip(1));

                    // Get the list of all vote partitions, built according to current preferences.
                    // One of: By line, By block, or By post (ie: entire vote)
                    List<string> votePartitions = GetVotePartitions(planLines, partitionMode, VoteType.Plan);

                    foreach (var votePartition in votePartitions)
                    {
                        VoteCounter.AddVoteSupport(votePartition, planName, VoteType.Plan);
                    }
                }
            }
        }
        #endregion

        #region Utility functions for processing votes.
        /// <summary>
        /// Get the contents of the vote from the lines of the entire post.
        /// Does not include base plans or ranked votes, and condenses
        /// known auto-votes into a simple reference.
        /// </summary>
        /// <param name="voteStrings">The contents of the post.</param>
        /// <returns>Returns just the vote portion of the post.</returns>
        private List<string> GetVoteFromPost(List<string> voteStrings)
        {
            List<string> vote = new List<string>();
            bool checkForBasePlans = true;

            // Remove ranked vote lines beforehand.
            var nonRankedLines = voteStrings.Where(s => !VoteString.IsRankedVote(s));

            // Then group everything leftover into blocks
            var voteBlocks = nonRankedLines.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

            foreach (var block in voteBlocks)
            {
                // Skip past base plan blocks at the start
                if (checkForBasePlans)
                {
                    if (GetPlanName(block.Key, basePlan: true) != null)
                        continue;
                }

                // If we get here, we're done checking for base plans.
                checkForBasePlans = false;

                // Check if the block defines a plan.
                if (GetPlanName(block.Key) != null)
                {
                    // Replace known plans with just the plan key, if we can match with a reference plan.
                    if (VoteCounter.ReferencePlans.Any(p => p.Value.SequenceEqual(block)))
                    {
                        // If it's a known plan, only pass through the reference.
                        vote.Add(block.Key);
                    }
                    else
                    {
                        // If it's not a known reference plan, pass the whole thing through.
                        vote.AddRange(block);
                    }
                }
                else
                {
                    // If it's not a plan, just pass it through.
                    vote.AddRange(block);
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
        private bool HasFutureReference(List<string> vote)
        {
            foreach (var line in vote)
            {
                // Exclude plan name marker references.
                var refNames = VoteString.GetVoteReferenceNames(line);
                var planNames = refNames.Where(r => r.StartsWith(Utility.Text.PlanNameMarker));
                var voteNames = refNames.Where(r => !r.StartsWith(Utility.Text.PlanNameMarker));

                // Any references to plans automatically work.
                if (planNames.Any(p => VoteCounter.HasPlan(p)))
                    continue;

                string refVoter = null;

                foreach (var name in voteNames)
                {
                    if (VoteCounter.ReferenceVoters.Contains(name) &&
                        !VoteCounter.ReferencePlanNames.Contains(name))
                    {
                        refVoter = name;
                        break;
                    }
                }

                if (refVoter != null)
                {
                    string contents = VoteString.GetVoteContent(line);
                    Match m = anyPlanRegex.Match(contents);
                    if (m.Success)
                    {
                        // If it matches the anyPlan regex, it has 'plan' in the line, and thus we
                        // only need to know if that voter has voted at all yet.
                        if (!VoteCounter.HasVoter(refVoter, VoteType.Vote))
                            return true;
                    }
                    else
                    {
                        // If it doesn't have a leading 'plan', we need to know whether the
                        // last vote the referenced voter made has been tallied.
                        if (!VoteCounter.HasVoter(refVoter, VoteType.Vote) ||
                            VoteCounter.VoterMessageId[refVoter] != VoteCounter.ReferenceVoterPosts[refVoter])
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
        private void ProcessVote(List<string> vote, PostComponents post, PartitionMode partitionMode)
        {
            // Remove the post author from any other existing votes.
            VoteCounter.RemoveSupport(post.Author, VoteType.Vote);

            // Add/update the post author's post ID.
            VoteCounter.AddVoterPostID(post.Author, post.ID, VoteType.Vote);

            // Get the list of all vote partitions, built according to current preferences.
            // One of: By line, By block, or By post (ie: entire vote)
            List<string> votePartitions = GetVotePartitions(vote, partitionMode, VoteType.Vote);

            foreach (var votePartition in votePartitions)
            {
                VoteCounter.AddVoteSupport(votePartition, post.Author, VoteType.Vote);
            }
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
        private List<string> GetRankingsFromPost(List<string> voteStrings)
        {
            // Get any explicit ranking votes from the post itself.
            var direct = voteStrings.Where(line => VoteString.IsRankedVote(line));

            if (direct.Any())
                return direct.ToList();

            // If there were no explicit rankings, see if there's a reference to
            // another voter as the only line of this vote.
            string refName = GetPureRankReference(voteStrings);

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
        private string GetPureRankReference(List<string> voteStrings)
        {
            if (voteStrings.Count == 1)
            {
                var partitionLines = Utility.Text.GetStringLines(voteStrings.First());

                if (partitionLines.Count == 1)
                {
                    var refNames = VoteString.GetVoteReferenceNames(partitionLines.First());

                    if (refNames.Count > 0)
                    {
                        var refName = refNames.FirstOrDefault(n => VoteCounter.RankedVoterMessageId.Keys.Contains(n));

                        if (refName != null)
                            return refName;
                    }
                }
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
                // Remove the post author from any other existing votes.
                VoteCounter.RemoveSupport(post.Author, VoteType.Rank);

                // Add/update the post author's post ID.
                VoteCounter.AddVoterPostID(post.Author, post.ID, VoteType.Rank);

                foreach (var line in ranksList)
                {
                    VoteCounter.AddVoteSupport(line, post.Author, VoteType.Rank);
                }
            }
        }

        #endregion

        #region Partitioning handling
        /// <summary>
        /// Given a list of vote lines, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="lines">List of valid vote lines.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> GetVotePartitions(IEnumerable<string> lines, PartitionMode partitionMode, VoteType voteType)
        {
            List<string> partitions = new List<string>();
            StringBuilder sb = new StringBuilder();
            StringBuilder holding_sb = new StringBuilder();
            string currentTask = "";
            string taskHeader = "";
            bool addedTopLevelLine = false;
            List<string> referralVotes;

            // Work through the list of matched lines
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // Handle partitioning based on either vote type or partition mode.

                if (voteType == VoteType.Rank)
                {
                    PartitionRanks(partitions, sb, trimmedLine);
                }
                else if ((referralVotes = VoteCounter.GetVotesFromReference(line)).Count > 0)
                {
                    // If a line refers to another voter or base plan, pull that voter's votes
                    PartitionReferrals(partitions, sb, referralVotes, partitionMode, ref taskHeader, ref currentTask);
                }
                else if (voteType == VoteType.Plan)
                {
                    PartitionPlans(partitions, sb, trimmedLine, partitionMode);
                }
                else if (partitionMode == PartitionMode.None)
                {
                    PartitionByVote(partitions, sb, trimmedLine);
                }
                else if (partitionMode == PartitionMode.ByLine)
                {
                    PartitionByLine(partitions, sb, trimmedLine);
                }
                else if (partitionMode == PartitionMode.ByBlock)
                {
                    PartitionByBlock(partitions, sb, trimmedLine);
                }
                else if (partitionMode == PartitionMode.ByTask)
                {
                    PartitionByTask(partitions, sb, trimmedLine);
                }
                else if (partitionMode == PartitionMode.ByTaskBlock)
                {
                    PartitionByTaskBlock(partitions, sb, holding_sb, trimmedLine, ref addedTopLevelLine, ref taskHeader, ref currentTask);
                }
            }

            if (holding_sb.Length > 0)
            {
                sb.Append(holding_sb.ToString());
            }
            if (sb.Length > 0)
            {
                partitions.Add(sb.ToString());
            }

            // Clean up any BBCode issues (matching tags, remove duplicate tags, etc)
            CleanUpBBCode(partitions);

            return partitions;
        }

        /// <summary>
        /// Add to the partition construction if we're working on a rank vote.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        private void PartitionRanks(List<string> partitions, StringBuilder sb, string line)
        {
            // Ranked vote lines are all treated individually.
            partitions.Add(line + "\r\n");
        }

        /// <summary>
        /// Add to the partition construction if we're working on a vote plan.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        private void PartitionPlans(List<string> partitions, StringBuilder sb, string line, PartitionMode partitionMode)
        {
            // If partitioning a Base Plan (other than By Line), simply collate all lines together.
            // The entire plan is considered a single block.
            if (partitionMode == PartitionMode.ByLine)
            {
                PartitionByLine(partitions, sb, line);
            }
            else if (partitionMode == PartitionMode.ByTask || partitionMode == PartitionMode.ByTaskBlock)
            {
                PartitionByTask(partitions, sb, line);
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        /// <summary>
        /// Add to the partition construction if the line found reference vote referrals.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="referralVotes">The list of all referenced votes.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        /// <param name="taskHeader">The task header (may be extracted and returned).</param>
        /// <param name="currentTask">The current task (may be extracted and returned).</param>
        private void PartitionReferrals(List<string> partitions, StringBuilder sb, List<string> referralVotes, PartitionMode partitionMode,
            ref string taskHeader, ref string currentTask)
        {
            // If we're not using vote partitions, append all lines onto the current vote string.
            // Otherwise, add each of the other voter's votes to our partition list.
            if (partitionMode == PartitionMode.None)
            {
                foreach (var v in referralVotes)
                    sb.Append(v);
            }
            else if (partitionMode == PartitionMode.ByTask)
            {
                foreach (var v in referralVotes)
                {
                    string task = VoteString.GetVoteTask(v);
                    if (task == string.Empty)
                    {
                        // If there is no task associated with the referral element,
                        // treat it like PartitionMode.None.
                        sb.Append(v);
                    }
                    else
                    {
                        // If there is a task, store any existing sb values in the
                        // partitions, and add the referral as a partition.
                        if (sb.Length > 0)
                        {
                            partitions.Add(sb.ToString());
                            sb.Clear();
                        }

                        currentTask = task;

                        string firstLine = Utility.Text.FirstLine(v);
                        string firstLineContent = VoteString.GetVoteContent(firstLine);
                        if (firstLineContent == string.Empty)
                        {
                            taskHeader = firstLine;
                        }
                        else
                        {
                            taskHeader = "";
                        }

                        partitions.Add(v);
                    }
                }
            }
            else
            {
                if (sb.Length > 0)
                {
                    partitions.Add(sb.ToString());
                    sb.Clear();
                }
                partitions.AddRange(referralVotes);
            }
        }

        /// <summary>
        /// Add to the partition construction if the vote is not being partitioned.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        private void PartitionByVote(List<string> partitions, StringBuilder sb, string line)
        {
            // If no partition mode, just stack all lines onto the string builder,
            // to be added together at the end.
            sb.AppendLine(line);
        }

        /// <summary>
        /// Add to the partition construction if the vote is being partitioned by line.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        private void PartitionByLine(List<string> partitions, StringBuilder sb, string line)
        {
            // If partitioning by line, every line gets added to the partitions list.
            // Skip lines without any content.
            if (VoteString.GetVoteContent(line) != string.Empty)
                partitions.Add(line + "\r\n");
        }

        /// <summary>
        /// Add to the partition construction if the vote is being partitioned by block.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        private void PartitionByBlock(List<string> partitions, StringBuilder sb, string line)
        {
            // If partitioning a vote by block, work on collecting chunks together.
            if (sb.Length == 0)
            {
                // Start a new block
                sb.AppendLine(line);
            }
            else if (VoteString.GetVotePrefix(line).StartsWith("-"))
            {
                // Sub-lines get added to an existing block
                sb.AppendLine(line);
            }
            else
            {
                // New top-level lines indicate we should save the current
                // accumulation and start a new block.
                string currentAccumulation = sb.ToString();
                // Skip blocks without any valid content
                if (VoteString.GetVoteContent(currentAccumulation) != string.Empty)
                    partitions.Add(sb.ToString());
                sb.Clear();
                sb.AppendLine(line);
            }
        }

        /// <summary>
        /// Add to the partition construction if the vote is being partitioned by task.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        private void PartitionByTask(List<string> partitions, StringBuilder sb, string line)
        {
            // Create a new block each time we encounter a new task.

            // If string builder is empty, start adding.
            if (sb.Length == 0)
            {
                sb.AppendLine(line);
            }
            else if (VoteString.GetVoteTask(line) != string.Empty)
            {
                // We've reached a new task block
                partitions.Add(sb.ToString());
                sb.Clear();
                sb.AppendLine(line);
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        /// <summary>
        /// Add to the partition construction if the vote is being partitioned by task and block.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        private void PartitionByTaskBlock(List<string> partitions, StringBuilder sb, StringBuilder holding_sb,
            string line, ref bool addedTopLevelLine, ref string taskHeader, ref string currentTask)
        {
            // A blend of task and block breakdowns
            // Top-level elements are retained within the current block if
            // we're inside a task segment.
            // However top-level elements with sub-elements get their own partition even if a
            // new task wasn't on that line.
            // Applies task name to each sub-block encountered.

            string prefix;
            string marker;
            string task;
            string content;

            // Get vote line components, since we'll be using them a bunch
            VoteString.GetVoteComponents(line, out prefix, out marker, out task, out content);

            if (task != string.Empty)
            {
                // We've started a new task block

                // Push all pending accumulations to the completed stacks
                if (holding_sb.Length > 0)
                {
                    sb.Append(holding_sb.ToString());
                    holding_sb.Clear();
                }

                if (sb.Length > 0)
                {
                    partitions.Add(sb.ToString());
                    sb.Clear();
                }

                sb.AppendLine(line);

                // Save details
                addedTopLevelLine = (prefix == string.Empty);
                currentTask = task;

                if (content == string.Empty)
                    taskHeader = line;
                else
                    taskHeader = "";
            }
            else if (sb.Length == 0)
            {
                // If string builder is empty, start adding new stuff.
                sb.AppendLine(line);

                // Save details
                addedTopLevelLine = (prefix == string.Empty);
                currentTask = task;

                // If the line is nothing but a task (no content), save this as a task header
                if (task != string.Empty && content == string.Empty)
                    taskHeader = line;
                else
                    taskHeader = "";
            }
            else if (holding_sb.Length > 0)
            {
                // holding_sb holds the last top-level line, if any.

                // If we get another top-level line, just push through the stack
                if (prefix == string.Empty)
                {
                    sb.Append(holding_sb.ToString());
                    holding_sb.Clear();
                    holding_sb.AppendLine(line);
                }
                else
                {
                    // If it's a sub-line, we started a new block
                    if (sb.Length > 0)
                    {
                        // If the current sb has any actual content in it, add to the partitions
                        if (VoteString.GetVoteContent(sb.ToString()) != string.Empty)
                            partitions.Add(sb.ToString());

                        sb.Clear();
                    }

                    if (taskHeader != string.Empty)
                    {
                        // If we have a defined task header, put it to the sb before we add
                        // the holding string.
                        sb.AppendLine(taskHeader);
                        sb.Append(holding_sb.ToString());
                    }
                    else if (currentTask != string.Empty)
                    {
                        // If we don't have a task header, but do have an active task, apply
                        // that to the holding string before adding it.
                        string tasked_holding_line = VoteString.ReplaceTask(holding_sb.ToString(), currentTask);
                        sb.Append(tasked_holding_line);
                    }
                    else
                    {
                        // Otherwise, just add what we're holding
                        sb.Append(holding_sb.ToString());
                    }

                    // Clear what we added
                    holding_sb.Clear();

                    // Add the incoming line
                    sb.AppendLine(line);
                    addedTopLevelLine = false;
                }
            }
            else
            {
                // Otherwise, we haven't stored any holding lines, but we -have- added
                // some lines to sb.

                // If we're adding a sub-level line, it always just gets added.
                if (prefix != string.Empty)
                {
                    sb.AppendLine(line);
                    addedTopLevelLine = false;
                }

                // If we're adding a new top-level line, it gets added to the holding string
                // if the previous line was also top-level.

                else if (addedTopLevelLine)
                {
                    holding_sb.AppendLine(line);
                }

                // If we're adding a new top-level line, but the previous line was -not-
                // a top-level, that means we're starting a new block
                else
                {
                    // If we're starting a new block, put the task header or current task
                    // in place, if applicable.

                    if (sb.Length > 0)
                    {
                        // Push anything in the current sb to the partitions
                        partitions.Add(sb.ToString());
                        sb.Clear();
                    }

                    // Add a task header or task element to the current line when
                    // starting a new block, if available.
                    if (taskHeader != string.Empty)
                    {
                        sb.AppendLine(taskHeader);
                        sb.AppendLine(line);
                    }
                    else if (currentTask != string.Empty)
                    {
                        sb.AppendLine(VoteString.ReplaceTask(line, currentTask));
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }

                    addedTopLevelLine = true;
                }
            }
        }
        #endregion

        #region Functions dealing with plan names.

        /// <summary>
        /// Get the plan name from a vote line, if the vote line is formatted to define a plan.
        /// All BBCode is removed from the line, including URLs (such as @username markup).
        /// </summary>
        /// <param name="voteLine">The vote line being examined.  Cannot be null.</param>
        /// <returns>Returns the plan name, if found, or null if not.</returns>
        private string GetPlanName(string voteLine, bool basePlan = false)
        {
            if (voteLine == null)
                throw new ArgumentNullException(nameof(voteLine));

            string lineContent = VoteString.GetVoteContent(voteLine);
            string simpleContent = VoteString.DeUrlContent(lineContent);

            Match m;

            if (basePlan)
                m = basePlanRegex.Match(simpleContent);
            else
                m = anyPlanRegex.Match(simpleContent);

            if (m.Success)
            {
                return m.Groups["planname"].Value.Trim();
            }

            return null;
        }

        /// <summary>
        /// Get the plan name from the provided vote line, and mark it with the plan name character
        /// marker if found.
        /// If no valid plan name is found, returns null.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the modified plan name, if found, or null if not.</returns>
        private string GetMarkedPlanName(string voteLine)
        {
            string planname = GetPlanName(voteLine);
            if (planname != null)
                return Utility.Text.PlanNameMarker + planname;

            return null;
        }

        /// <summary>
        /// Given a list of lines that corresponds to a base plan as part of a user's post,
        /// remove the line with the plan's name, and promote all other lines one level.
        /// Promotion = Turn a -[x] line to a [x] line.
        /// </summary>
        /// <param name="planLines">Vote lines that start with a Base Plan name.</param>
        /// <returns>Returns the plan's vote lines as if they were their own vote.</returns>
        private List<string> PromotePlanLines(IEnumerable<string> planLines)
        {
            if (planLines.All(a => a.Trim().StartsWith("-")))
            {
                var promotedLines = planLines.Select(a => a.Trim().Substring(1));

                return promotedLines.ToList();
            }
            else
            {
                return planLines.ToList();
            }
        }
        #endregion

        #region Functions dealing with BBCode
        /// <summary>
        /// Handle various forms of cleanup relating to BBCode in the vote partitions.
        /// </summary>
        /// <param name="partitions">List of vote strings.</param>
        private void CleanUpBBCode(List<string> partitions)
        {
            // Make sure any BBCode formatting tags are matched up in each partition result.
            CloseFormattingTags(partitions);
            // Remove newlines after BBCode tags
            CompactBBCodeNewlines(partitions);
            // Clean duplicate BBCode tags (eg: [b][b]stuff[/b][/b])
            StripRedundantBBCode(partitions);
            // If the entire string in a partition is bolded, remove the bolding.
            UnboldLines(partitions);
        }

        /// <summary>
        /// Make sure each vote string in the provided list closes any opened BBCode formatting it uses,
        /// and that orphan closing tags are removed.
        /// </summary>
        /// <param name="partitions">List of vote strings.</param>
        public void CloseFormattingTags(List<string> partitions)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            bool replace;

            foreach (var partition in partitions)
            {
                string replacement = partition.TrimEnd();
                replace = false;

                foreach (var tag in formattingTags)
                {
                    var start = rxStart[tag];
                    var end = rxEnd[tag];

                    var starts = start.Matches(partition);
                    var ends = end.Matches(partition);

                    if (starts.Count > ends.Count)
                    {
                        for (int i = ends.Count; i < starts.Count; i++)
                        {
                            replacement += "[/" + tag + "]";
                        }
                        replace = true;
                    }
                    else if (ends.Count > starts.Count)
                    {
                        replacement = end.Replace(replacement, "", ends.Count - starts.Count);
                        replace = true;
                    }
                }

                if (replace)
                {
                    replacements[partition] = replacement + "\r\n";
                }
            }

            foreach (var rep in replacements)
            {
                partitions.Remove(rep.Key);
                partitions.Add(rep.Value);
            }
        }

        /// <summary>
        /// Check each partition string, and remove newlines that are immediately after any
        /// BBCode opening tag.
        /// </summary>
        /// <param name="partitions">List of vote strings.</param>
        private void CompactBBCodeNewlines(List<string> partitions)
        {
            Regex openBBCodeNewlines = new Regex(@"(\[[biu]\])[\r\n]+");
            MatchEvaluator me = new MatchEvaluator(MatchEvaluatorGroup1);
            List<string> correctedPartitions = new List<string>();

            foreach (string part in partitions)
            {
                correctedPartitions.Add(openBBCodeNewlines.Replace(part, me));
            }
            
            partitions.Clear();
            partitions.AddRange(correctedPartitions);
        }

        /// <summary>
        /// Check each partition string, and remove duplicate BBCode tags.
        /// </summary>
        /// <param name="partitions">List of vote strings.</param>
        private void StripRedundantBBCode(List<string> partitions)
        {
            MatchEvaluator me = new MatchEvaluator(MatchEvaluatorGroup1);
            List<string> correctedPartitions = new List<string>();

            string[] codes = { "b", "i", "u" };

            foreach (string part in partitions)
            {
                string corrected = part;

                foreach (string code in codes)
                {
                    Regex dupeStart = new Regex($@"(\[{code}\]){{2}}");
                    Regex dupeEnd = new Regex($@"(\[/{code}\]){{2}}");

                    Match mStart = dupeStart.Match(part);
                    Match mEnd = dupeEnd.Match(part);

                    if (mStart.Success && mEnd.Success)
                    {
                        corrected = dupeStart.Replace(corrected, me);
                        corrected = dupeEnd.Replace(corrected, me);
                    }
                }

                correctedPartitions.Add(corrected);
            }

            partitions.Clear();
            partitions.AddRange(correctedPartitions);
        }

        /// <summary>
        /// Remove bold BBCode tags if they encompass the entire partition (vote) line.
        /// </summary>
        /// <param name="partitions">List of vote strings.</param>
        private void UnboldLines(List<string> partitions)
        {
            Regex openBBCodeNewlines = new Regex(@"^\[b\](.+)\[/b\](\r\n)$");
            MatchEvaluator me = new MatchEvaluator(MatchEvaluatorGroup12);
            List<string> correctedPartitions = new List<string>();

            foreach (string part in partitions)
            {
                correctedPartitions.Add(openBBCodeNewlines.Replace(part, me));
            }

            partitions.Clear();
            partitions.AddRange(correctedPartitions);
        }

        /// <summary>
        /// Return group 1 of a regex match.
        /// </summary>
        /// <param name="m">Match from a replacement check.</param>
        /// <returns>Return group 1 of a regex match.</returns>
        private string MatchEvaluatorGroup1(Match m) => m.Groups[1].Value;

        /// <summary>
        /// Return groups 1 and 2 of a regex match.
        /// </summary>
        /// <param name="m">Match from a replacement check.</param>
        /// <returns>Return groups 1 and 2 of a regex match.</returns>
        private string MatchEvaluatorGroup12(Match m) => m.Groups[1].Value + m.Groups[2].Value;
        #endregion
    }
}
