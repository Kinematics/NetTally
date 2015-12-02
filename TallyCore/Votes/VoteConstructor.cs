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

            // A 0 count vote means the post only contained base plans.  Done.
            if (vote.Count == 0)
                return true;

            // If it has a reference to a plan or voter that has not been processed yet,
            // delay processing.
            if (HasFutureReference(vote, post.Author))
            {
                VoteCounter.FutureReferences.Add(post);
                return false;
            }

            // Process the actual vote.
            ProcessVote(vote, post, quest.PartitionMode);

            // Handle ranking votes, if applicable.
            if (quest.AllowRankedVotes)
            {
                var rankings = GetRankingsFromPost(post.VoteStrings, post.Author);

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

            var voteBlocks = postStrings.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

            foreach (var block in voteBlocks)
            {
                if (block.Count() > 1)
                {
                    string planname = GetPlanName(block.Key);

                    if (planname != null && !VoteCounter.HasPlan(planname))
                        results.Add(block.ToList());
                }
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
                    var planLines = PromotePlanName(plan);

                    // Get the list of all vote partitions, built according to current preferences.
                    // One of: By line, By block, or By post (ie: entire vote)
                    List<string> votePartitions = GetVotePartitions(planLines, partitionMode, VoteType.Plan, post.Author);

                    VoteCounter.AddVote(votePartitions, planName, post.ID, VoteType.Plan);
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
                    if (block.Count() > 1 && GetPlanName(block.Key, basePlan: true) != null)
                        continue;
                }

                // If we get here, we're done checking for base plans.
                checkForBasePlans = false;

                // Check if the block defines a plan.
                if (block.Count() > 1)
                {
                    // Replace known plans with just the plan key, if we can match with a reference plan.
                    string planName = GetPlanName(block.Key);

                    if (planName != null && VoteCounter.ReferencePlans.ContainsKey(planName) &&
                        VoteCounter.ReferencePlans[planName].Skip(1).SequenceEqual(block.Skip(1), Text.AgnosticStringComparer))
                    {
                        // If it's a known plan, only pass through the reference.
                        vote.Add(block.Key);
                    }
                    else
                    {
                        // If it's not a plan, just pass it through.
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
        private bool HasFutureReference(List<string> vote, string author)
        {
            var voters = VoteCounter.GetVotersCollection(VoteType.Vote);

            foreach (var line in vote)
            {
                // Exclude plan name marker references.
                var refNames = VoteString.GetVoteReferenceNames(line);

                // Any references to plans automatically work.
                if (refNames[ReferenceType.Plan].Any(p => VoteCounter.HasPlan(p)))
                    continue;

                string refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => VoteCounter.ReferenceVoters.Contains(n));

                if (refVoter != null && refVoter != author)
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
        private void ProcessVote(List<string> vote, PostComponents post, PartitionMode partitionMode)
        {
            // Get the list of all vote partitions, built according to current preferences.
            // One of: By line, By block, or By post (ie: entire vote)
            List<string> votePartitions = GetVotePartitions(vote, partitionMode, VoteType.Vote, post.Author);

            VoteCounter.AddVote(votePartitions, post.Author, post.ID, VoteType.Vote);
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
        private List<string> GetRankingsFromPost(List<string> voteStrings, string author)
        {
            // Get any explicit ranking votes from the post itself.
            var direct = voteStrings.Where(line => VoteString.IsRankedVote(line));

            if (direct.Any())
                return direct.ToList();

            // If there were no explicit rankings, see if there's a reference to
            // another voter as the only line of this vote.
            string refName = GetPureRankReference(voteStrings, author);

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
        private string GetPureRankReference(List<string> voteStrings, string author)
        {
            if (voteStrings.Count == 1)
            {
                var partitionLines = Text.GetStringLines(voteStrings.First());

                if (partitionLines.Count == 1)
                {
                    var refNames = VoteString.GetVoteReferenceNames(partitionLines.First());

                    var refVoter = refNames[ReferenceType.Voter].FirstOrDefault(n => n != author && VoteCounter.HasVoter(n, VoteType.Rank));

                    return refVoter;
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
                VoteCounter.AddVote(ranksList, post.Author, post.ID, VoteType.Rank);
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
                    // When partitioning by line, promote the plan first
                    return PartitionByLine(PromoteLines(lines), author);
                case PartitionMode.ByBlock:
                    // When partitioning by block, plans are kept whole
                    return PartitionByNone(lines, author);
                case PartitionMode.ByPlanBlock:
                    // When partitioning by PlanBlock, the plan is partitioned by block after promotion
                    return PartitionByBlock(PromoteLines(lines), author);
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
                    foreach (var referral in referralVotes)
                        partitions.Add(referral);
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

                    foreach (var referral in referralVotes)
                        partitions.Add(referral);
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
        #endregion

        #region Functions dealing with plan names.

        /// <summary>
        /// Get the plan name from a vote line, if the vote line is formatted to define a plan.
        /// All BBCode is removed from the line, including URLs (such as @username markup).
        /// </summary>
        /// <param name="voteLine">The vote line being examined.  Cannot be null.</param>
        /// <param name="basePlan">Flag whether the vote line must be a "base plan", rather than any plan.</param>
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
        private IEnumerable<string> PromotePlanName(IEnumerable<string> lines)
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
