using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetTally
{
    /// <summary>
    /// Class that can handle constructing votes (in various manners) from the base text of a post.
    /// </summary>
    public class VoteConstructor
    {
        #region Constructor and vars
        IVoteCounter VoteCounter { get; }
        readonly bool allowAutomaticPlans = false;

        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        readonly Regex basePlanRegex = new Regex(@"base\s*plan(:|\s)+(?<baseplan>.+)", RegexOptions.IgnoreCase);
        // Check for a plan reference.
        readonly Regex planRegex = new Regex(@"plan(:|\s)+(?<planname>.+)", RegexOptions.IgnoreCase);
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
        /// Handle processing the vote portions of a post.
        /// </summary>
        /// <param name="post">The post to process.</param>
        /// <param name="quest">The quest being tallied.</param>
        public void ProcessPost(PostComponents post, IQuest quest, bool storeFloatingReferences)
        {
            if (!post.IsVote)
                throw new ArgumentException("Post is not a valid vote.", nameof(post));

            // Separate the lines of the vote into their different types.
            var groupedVoteLines = SeparateVoteTypes(post.VoteStrings);

            // Process each type separately
            ProcessPlans(groupedVoteLines[VoteType.Plan], post, quest.PartitionMode);
            ProcessVotes(groupedVoteLines[VoteType.Vote], post, quest.PartitionMode, storeFloatingReferences);
            if (quest.AllowRankedVotes)
            {
                ProcessRanks(groupedVoteLines[VoteType.Rank], post, quest.PartitionMode);
            }
        }
        #endregion
        
        #region Direct utility functions for processing a post.
        /// <summary>
        /// Given a list of vote lines from a post, break it down into groups of lines,
        /// based on vote type.
        /// Type Plan: Base Plans come first, and must start with a [x] Base Plan: ~plan name~ line.
        /// Type Vote: Normal votes come after that, and collect together all non-rank vote lines.
        /// Type Rank: Rank votes come after that, and are treated independently, with each line as its own entry.
        /// Any base plan lines after normal votes start are ignored.
        /// Any normal vote lines after rank lines start are ignored.
        /// </summary>
        /// <param name="postLines">All the vote lines of the post.</param>
        /// <returns>Returns a dict with lists of strings, each labeled according to
        /// the section of the post vote they correspond to (either plan or vote).</returns>
        private Dictionary<VoteType, List<List<string>>> SeparateVoteTypes(List<string> postLines)
        {
            // The list of all votes from the provided list of lines
            List<Vote> votes = new List<Vote>();
            // Save all lines that will eventually add up to the normal vote
            List<PostLine> normalVote = new List<PostLine>();
            // Var for accumulating base plans
            List<PostLine> basePlan = null;

            List<string> basePlan = null;
            bool checkForBasePlans = true;
            foreach (var line in postLines)
            {
                if (checkForBasePlans)
                {
                    // If no base plan currently defined, or we're starting a new non-nested line,
                    // check to see if it's the start of a new base plan.
                    if ((basePlan == null) || !line.Trim().StartsWith("-"))
                    {
                        Match m = basePlanRegex.Match(line);
                        if (m.Success)
                        {
                            // Make sure the plan doesn't already exist in the tracker.
                            // If it does, this counts as a repeat, and should be considered an attempt to 
                            // reference the original plan, rather than redefine it.
                            // As soon as this occurs, we should treat all further lines
                            // as regular vote lines, rather than additional potential plans.
                            if (!VoteCounter.HasPlan(m.Groups["baseplan"].Value))
                            {
                                // If it's a new base plan, add the first line to a new base plan list and continue processing the post lines
                                basePlan = new List<string>();
                                basePlan.Add(line);
                                results[VoteType.Plan].Add(basePlan);
                                continue;
                        {
                            // If so, add the first line to a new base plan list and continue processing the post lines
                            basePlan = new List<PostLine>();
                            basePlan.Add(line);
                            continue;
                        }
                    }
                    else
                    {
                        // If we've started a base plan, continue adding any lines that start with "-"
                        if (line.Clean.StartsWith("-"))
                        {
                            basePlan.Add(line);
                            continue;
                        }
                        else
                        {
                            // If we reached a new top-level vote line, save any existing base plan as long
                            // as it has more than one line.
                            if (basePlan.Count > 1)
                                votes.Add(new Vote(basePlan, VoteType.Plan));
                            // Check to see if we've reached the start of a new base plan
                            Match m = basePlanRegex.Match(line.Clean);
                            if (m.Success)
                            {
                                // If so, continue the process
                                basePlan = new List<PostLine>();
                                basePlan.Add(line);
                                continue;
                            }
                            else
                            {
                                // Otherwise, null the base plan var and continue with normal processing.
                                basePlan = null;
                            }
                        }
                    }
                }

                // If we get to here, we've finished checking for base plans.
                checkForBasePlans = false;

                // Rank vote lines are added individually to the Rank results.
                if (VoteString.IsRankedVote(line.Clean))
                    votes.Add(new Vote(line, VoteType.Rank));
                }
                else
                {
                    // Anything else is stored in our normal vote list, to be added when we're done.
                    normalVote.Add(line);
                }
                else
                {
                    // And if we get a regular vote line, create a new list to hold
                    // all of the vote's lines and start adding them.
                    if (results[VoteType.Vote].Count == 0)
                    {
                        results[VoteType.Vote].Add(new List<string>());
                    }

                    results[VoteType.Vote].First().Add(line);
            }

            // If there's any base plan that wasn't completed in the above loop, store it here.
            if (basePlan != null && basePlan.Count > 1)
            {
                votes.Add(new Vote(basePlan, VoteType.Plan));
            }

            // If any normal vote was accumulated, add it here.
            if (normalVote.Count > 0)
            {
                votes.Add(new Vote(normalVote, VoteType.Vote));
            }

            return votes;
        }


        /// <summary>
        /// Put any plans found in the grouped vote lines into the standard tracking sets.
        /// </summary>
        /// <param name="voteLinesGrouped"></param>
        /// <param name="postID"></param>
        /// <param name="quest"></param>
        private void ProcessPlans(List<List<string>> plansList, PostComponents post, PartitionMode partitionMode)
        {
            foreach (var plan in plansList)
            {
                string planName = GetBasePlanName(plan);

                // Remove the plan name from any other existing votes.
                VoteCounter.RemoveSupport(planName, VoteType.Plan);

                // Add/update the plan's post ID.
                VoteCounter.AddVoterPostID(planName, post.ID, VoteType.Plan);

                // Promote the plan one tier (excluding the line of the plan name itself) <<<< Do we want to remove this?
                Vote promotedPlan = PromotePlanLines(plan);

                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<string> votePartitions = GetVotePartitions(planLines, partitionMode, VoteType.Plan);

                foreach (var votePartition in votePartitions)
                {
                    VoteCounter.AddVoteSupport(votePartition, planName, VoteType.Plan);
                }
            }
        }

        /// <summary>
        /// Put any votes found in the grouped vote lines into the standard tracking sets.
        /// </summary>
        /// <param name="votesList">List of votes (collections of strings)</param>
        /// <param name="post">The components of the original post.</param>
        /// <param name="partitionMode">The partition mode being used.</param>
        /// <param name="storeFloatingReferences">Whether to store floating references.</param>
        private void ProcessVotes(List<List<string>> votesList, PostComponents post, PartitionMode partitionMode, bool storeFloatingReferences)
        {
            var vote = votesList.FirstOrDefault();

            if (vote != null)
            {
                if (storeFloatingReferences)
                {
                    if (IsFloatingReference(vote.VoteLines))
                    {
                        VoteCounter.FloatingReferences.Add(post);
                        return;
                    }

                    PostComponents existingReference = GetFloatingReference(post.Author);

                    if (existingReference != null)
                    {
                        VoteCounter.FloatingReferences.Remove(existingReference);
                    }
                }


                // Remove the post author from any other existing votes.
                VoteCounter.RemoveSupport(post.Author, VoteType.Vote);

                // Add/update the post author's post ID.
                VoteCounter.AddVoterPostID(post.Author, post.ID, VoteType.Vote);

                // Automatically get any plan names, if named as such at the start of the vote.
                string automaticPlanName = null;
                // Currently disabled.
                if (allowAutomaticPlans)
                    automaticPlanName = AutomaticPlan(vote);

                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<string> votePartitions = GetVotePartitions(vote, partitionMode, VoteType.Vote);

                foreach (var votePartition in votePartitions)
                {
                    if (automaticPlanName != null && automaticPlanName != string.Empty)
                    {
                        // Add the plan's post ID.
                        VoteCounter.AddVoterPostID(automaticPlanName, post.ID, VoteType.Plan);
                        // Add the plan partition.
                        VoteCounter.AddVoteSupport(votePartition, automaticPlanName, VoteType.Plan);
                    }

                    VoteCounter.AddVoteSupport(votePartition, post.Author, VoteType.Vote);
                }


                // Solo references to other voters may copy over their rank votes as well.
                string refName = GetPureRankReference(votePartitions);

                if (refName != null)
                {
                    var refRanks = VoteCounter.GetVotesCollection(VoteType.Rank).Where(r => r.Value.Contains(refName));

                    VoteCounter.AddVoterPostID(post.Author, post.ID, VoteType.Rank);

                    foreach (var refRank in refRanks)
                    {
                        VoteCounter.AddVoteSupport(refRank.Key, post.Author, VoteType.Rank);
                    }
                }
            }
        }

        /// <summary>
        /// Get the name of a voter that is referenced if that is the only
        /// reference in the vote.
        /// </summary>
        /// <param name="votePartitions">The standard vote partitions.</param>
        /// <returns></returns>
        private string GetPureRankReference(List<string> votePartitions)
        {
            if (votePartitions.Count == 1)
            {
                var partitionLines = Utility.Text.GetStringLines(votePartitions.First());

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
        private void ProcessRanks(List<List<string>> ranksList, PostComponents post, PartitionMode partitionMode)
        {
            if (ranksList.Count > 0)
            {
                // Remove the post author from any other existing votes.
                VoteCounter.RemoveSupport(post.Author, VoteType.Rank);

                // Add/update the post author's post ID.
                VoteCounter.AddVoterPostID(post.Author, post.ID, VoteType.Rank);

                foreach (var vote in ranksList)
                {
                    VoteCounter.AddVoteSupport(line.First(), post.Author, VoteType.Rank);
                }
            }
        }

        /// <summary>
        /// Given a list of vote lines, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="lines">List of valid vote lines.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> GetVotePartitions(IEnumerable<string> lines, PartitionMode partitionMode, VoteType voteType)
        {
            List<Vote> partitions = new List<Vote>();
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
        /// Get a plan name automatically from a normal vote, if said plan name doesn't already exist.
        /// </summary>
        /// <param name="vote">The vote to check.</param>
        /// <returns>Returns the plan name, if found, or null if not.</returns>
        private string AutomaticPlan(List<string> vote)
        {
            if (vote == null)
                return null;
            // An empty string list is ignored.
            // A single line cannot be a plan.
            if (vote.Count < 2)
                return null;

            string firstLineContent = VoteString.GetVoteContent(vote.First());

            if (firstLineContent == string.Empty)
                return null;

            Match m = planRegex.Match(firstLineContent);
            if (m.Success)
            {
                string autoPlanName = Utility.Text.PlanNameMarker + m.Groups["planname"].Value;

                if (VoteCounter.GetVotersCollection(VoteType.Plan).ContainsKey(autoPlanName))
                    return null;

                if (VoteCounter.HasPlan(autoPlanName))
                    return null;

                return autoPlanName;
            }

            return null;
        }

        #endregion

        #region Vote construction based on partitioning modes.
        /// <summary>
        /// Add to the partition construction if we're working on a rank vote.
        /// </summary>
        /// <param name="partitions">The table of collected partitions.</param>
        /// <param name="sb">The ongoing constructed string.</param>
        /// <param name="line">The vote line currently being examined.</param>
        private void PartitionRanks(List<Vote> partitions, StringBuilder sb, PostLine line)
        {
            // Ranked vote lines are all treated individually.
            partitions.Add(new Vote(line, VoteType.Rank));
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

        #region Utility
        /// <summary>
        /// Determine if the list of vote lines provided can be considered a
        /// floating reference vote.
        /// </summary>
        /// <param name="voteLines">A list of lines for a vote.</param>
        /// <returns>Returns true if there is a single line that references
        /// a known username (per the VoteCounter).</returns>
        private bool IsFloatingReference(List<string> vote)
        {
            // A vote with multiple vote lines cannot be floating references.
            if (voteLines.Count != 1)
                return false;

            string voteEntry = vote.First();

            var voteLines = Utility.Text.GetStringLines(voteEntry);

            // If the content spans multiple lines, it can't be a floating reference.
            if (voteLines.Count > 1)
                return false;

            // Get the content of the first line of the vote.
            string content = VoteString.GetVoteContent(voteLines.First());

            // Anything starting with "plan" or "base plan" is a fixed reference.
            // Though if the entire match fails, bail out as well.
            Match m = planNameRegex.Match(content);
            if (!m.Success || m.Groups["label"].Success)
                return false;

            // If the content contains a name that exists in the voter list, it can be a floating reference.
            string refName = m.Groups["reference"].Value;

            return VoteCounter.VotePosts.Any(v => string.Compare(refName, v.Author, true) == 0);
        }

        /// <summary>
        /// Return the PostComponents of a post made by the requested author.
        /// </summary>
        /// <param name="author">Author of a post.</param>
        /// <returns>Returns PostComponents if a floating reference post was found, or null if not found.</returns>
        private PostComponents GetFloatingReference(string author)
        {
            var existingReference = VoteCounter.FloatingReferences.FirstOrDefault(r => r.Author == author);
            return existingReference;
        }

        /// <summary>
        /// Given a list of lines that corresponds to a base plan as part of a user's post,
        /// extract the name of the plan.
        /// </summary>
        /// <param name="planLines">Vote lines that start with a Base Plan name.</param>
        /// <returns>Returns the name of the base plan.</returns>
        private string GetBasePlanName(List<string> planLines)
        {
            PostLine firstLine = vote.VoteLines.First();
            string lineContent = VoteString.GetVoteContent(firstLine.Clean);

            Match m = basePlanRegex.Match(lineContent);
            if (m.Success)
            {
                string planName = m.Groups["baseplan"].Value.Trim();

                return Utility.Text.PlanNameMarker + planName;
            }

            throw new InvalidOperationException("These are not the lines for a base plan.");
        }

        /// <summary>
        /// Given a list of lines that corresponds to a base plan as part of a user's post,
        /// remove the line with the plan's name, and promote all other lines one level.
        /// Promotion = Turn a -[x] line to a [x] line.
        /// </summary>
        /// <param name="plan">Vote lines that start with a Base Plan name.</param>
        /// <returns>Returns the plan's vote lines as if they were their own vote.</returns>
        private List<string> PromotePlanLines(List<string> planLines)
        {
            var promotedLines = from p in plan.VoteLines.Skip(1)
                                select new PostLine(p.Original.Substring(1));

            return new Vote(promotedLines, VoteType.Plan);
        }

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
