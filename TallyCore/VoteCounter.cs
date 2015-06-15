using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NetTally
{
    public class VoteCounter : IVoteCounter
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public VoteCounter()
        {
            SetupFormattingRegexes();
        }

        #region Private variables

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

        readonly List<string> formattingTags = new List<string>() { "color", "b", "i", "u" };
        readonly Dictionary<string, Regex> rxStart = new Dictionary<string, Regex>();
        readonly Dictionary<string, Regex> rxEnd = new Dictionary<string, Regex>();

        /// <summary>
        /// Reset all tracking variables.
        /// </summary>
        private void Reset()
        {
            VotesWithSupporters.Clear();
            VoterMessageId.Clear();
            RankedVotesWithSupporters.Clear();
            RankedVoterMessageId.Clear();
            PlanNames.Clear();
            cleanVoteLookup.Clear();
            Title = string.Empty;
        }


        readonly Dictionary<string, string> cleanVoteLookup = new Dictionary<string, string>();

        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        readonly Regex tallyRegex = new Regex(@"^#####", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex voteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\s*\[\s*[xX+✓✔]\s*\].*", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex rankVoteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\s*\[\s*[xX+✓✔1-9]\s*\].*", RegexOptions.Multiline);
        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        readonly Regex basePlanRegex = new Regex(@"base\s*plan(:|\s)+(?<baseplan>.+)", RegexOptions.IgnoreCase);
        #endregion

        #region Public Interface
        public string Title { get; set; } = string.Empty;

        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> RankedVoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> PlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool HasRankedVotes => RankedVotesWithSupporters.Count > 0;

        public Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVotesWithSupporters;
            else
                return VotesWithSupporters;
        }

        public Dictionary<string, string> GetVotersCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVoterMessageId;
            else
                return VoterMessageId;
        }

        /// <summary>
        /// Construct the votes Results from the provide list of HTML pages.
        /// </summary>
        /// <param name="pages"></param>
        public void TallyVotes(IQuest quest, List<HtmlDocument> pages)
        {
            if (pages == null)
                throw new ArgumentNullException(nameof(pages));

            if (pages.Count == 0)
                return;

            IForumAdapter forumAdapter = quest.GetForumAdapter();

            Reset();

            // Set the thread author for reference.
            string threadAuthor = forumAdapter.GetAuthorOfThread(pages.First());

            foreach (var page in pages)
            {
                if (page != null)
                {
                    if (Title == string.Empty)
                        Title = forumAdapter.GetPageTitle(page);

                    // Get a list of valid posts to process from all posts on the page.
                    var validPosts = from post in forumAdapter.GetPostsFromPage(page)
                                     where post != null
                                     let postNumber = forumAdapter.GetPostNumberOfPost(post)
                                     where (DebugMode.Instance.Active || forumAdapter.GetAuthorOfPost(post) != threadAuthor) &&
                                        postNumber >= quest.FirstTallyPost && (quest.ReadToEndOfThread || postNumber <= quest.EndPost)
                                     select post;


                    // Process each user post in the list.
                    foreach (var post in validPosts)
                    {
                        ProcessPost(post, quest);
                    }
                }
            }
        }

        /// <summary>
        /// Merges the specified from vote into the specified to vote, assuming the votes aren't the same.
        /// Moves the voters from the from vote into the to vote list, and removes the from vote's key.
        /// </summary>
        /// <param name="fromVote">Vote that is being merged.</param>
        /// <param name="toVote">Vote that is being merged into.</param>
        public bool Merge(string fromVote, string toVote, VoteType voteType)
        {
            if (fromVote == null)
                throw new ArgumentNullException(nameof(fromVote));
            if (toVote == null)
                throw new ArgumentNullException(nameof(toVote));
            if (fromVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(fromVote), "Vote string is empty.");
            if (toVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(toVote), "Vote string is empty.");
            if (fromVote == toVote)
                return false;

            Dictionary<string, HashSet<string>> votesSet;

            if (voteType == VoteType.Rank)
                votesSet = RankedVotesWithSupporters;
            else
                votesSet = VotesWithSupporters;

            HashSet<string> fromVoters;
            HashSet<string> toVoters;

            if (!votesSet.TryGetValue(fromVote, out fromVoters))
                throw new ArgumentException(nameof(fromVote) + " does not exist.");
            if (!votesSet.TryGetValue(toVote, out toVoters))
                throw new ArgumentException(nameof(toVote) + " does not exist.");

            toVoters.UnionWith(fromVoters);

            votesSet.Remove(fromVote);

            return true;
        }

        /// <summary>
        /// Delete a vote from the vote list specified.
        /// </summary>
        /// <param name="vote">The vote to remove.</param>
        /// <param name="voteType">The type of vote to remove.</param>
        /// <returns>Returns true if a vote was removed.</returns>
        public bool Delete(string vote, VoteType voteType)
        {
            if (vote == null && vote == string.Empty)
                return false;

            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;

            bool removed = false;

            if (votesDict.ContainsKey(vote))
            {
                var votersToTrim = votesDict[vote];

                removed = votesDict.Remove(vote);

                foreach (var voter in votersToTrim)
                    TrimVoter(voter, voteType);
            }

            return removed;
        }

        public bool Rename(string oldVote, string newVote, VoteType voteType)
        {
            if (oldVote == null)
                throw new ArgumentNullException(nameof(oldVote));
            if (newVote == null)
                throw new ArgumentNullException(nameof(newVote));
            if (oldVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(oldVote), "Vote string is empty.");
            if (newVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(newVote), "Vote string is empty.");
            if (oldVote == newVote)
                return false;

            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;

            if (votesDict.ContainsKey(newVote))
            {
                return Merge(oldVote, newVote, voteType);
            }

            HashSet<string> votes;
            if (votesDict.TryGetValue(oldVote, out votes))
            {
                votesDict.Remove(oldVote);
                votesDict[newVote] = votes;
                return true;
            }

            return false;
        }
        #endregion

        #region Private support methods
        /// <summary>
        /// Function to process individual posts within the thread.
        /// Updates the vote records maintained in the class.
        /// </summary>
        /// <param name="post">The list item node containing the post.</param>
        /// <param name="startPost">The first post number of the thread to check.</param>
        /// <param name="endPost">The last post number of the thread to check.</param>
        private void ProcessPost(HtmlNode post, IQuest quest)
        {
            if (post == null)
                return;

            IForumAdapter forumAdapter = quest.GetForumAdapter();
            string postAuthor = forumAdapter.GetAuthorOfPost(post);
            string postID = forumAdapter.GetIdOfPost(post);
            string postText = forumAdapter.GetTextOfPost(post);

            if (DebugMode.Instance.Active)
                postAuthor = postAuthor + "_" + postID;

            // Attempt to get vote information from the post.
            ProcessPostContents(postText, postAuthor, postID, quest);
        }
        
        /// <summary>
        /// Examine the text of the post, determine if it contains any votes, put the vote
        /// together, and update our vote and voter records.
        /// </summary>
        /// <param name="postText">The text of the post.</param>
        /// <param name="postAuthor">The author of the post.</param>
        /// <param name="postID">The ID of the post.</param>
        private void ProcessPostContents(string postText, string postAuthor, string postID, IQuest quest)
        {
            if (IsTallyPost(postText))
                return;

            // Pull out actual vote lines from the post.
            MatchCollection matches;

            // Use the regex that allows [1-9] if we're allowing ranked votes
            if (quest.AllowRankedVotes)
                matches = rankVoteRegex.Matches(postText);
            else
                matches = voteRegex.Matches(postText);

            // If we found any matches, continue
            if (matches.Count > 0)
            {
                // Pull the matched string out of the Match objects to make it easier to work with.
                List<string> matchStrings = GetVoteLineStrings(matches);

                Dictionary<List<string>, VoteType> voteLinesGrouped = SeparateVoteTypes(matchStrings);

                ProcessPlans(voteLinesGrouped, postAuthor, postID, quest);
                ProcessVotes(voteLinesGrouped, postAuthor, postID, quest);
                ProcessRanks(voteLinesGrouped, postAuthor, postID, quest);
            }
        }

        /// <summary>
        /// Put any plans found in the grouped vote lines into the standard tracking sets.
        /// </summary>
        /// <param name="voteLinesGrouped"></param>
        /// <param name="postAuthor"></param>
        /// <param name="postID"></param>
        /// <param name="quest"></param>
        private void ProcessPlans(Dictionary<List<string>, VoteType> voteLinesGrouped, string postAuthor, string postID, IQuest quest)
        {
            var plans = voteLinesGrouped.Where(v => v.Value == VoteType.Plan).Select(vs => vs.Key);

            foreach (var plan in plans)
            {
                string planName = GetBasePlanName(plan);

                // Remove the post author from any other existing votes.
                RemoveSupport(planName, VoteType.Plan);
                // Add/update the plan's post ID to the tracking hashset.
                VoterMessageId[planName] = postID;
                PlanNames.Add(planName);

                List<string> planLines = PromotePlanLines(plan);

                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<string> votePartitions = GetVotePartitions(planLines, quest, VoteType.Plan);

                foreach (var votePartition in votePartitions)
                {
                    // Find any existing vote that matches the current vote partition.
                    string voteKey = GetVoteKey(votePartition, quest, VoteType.Plan);

                    // Update the supporters list, and save this voter's post ID for linking.
                    VotesWithSupporters[voteKey].Add(planName);
                }
            }
        }

        /// <summary>
        /// Put any votes found in the grouped vote lines into the standard tracking sets.
        /// </summary>
        /// <param name="voteLinesGrouped"></param>
        /// <param name="postAuthor"></param>
        /// <param name="postID"></param>
        /// <param name="quest"></param>
        private void ProcessVotes(Dictionary<List<string>, VoteType> voteLinesGrouped, string postAuthor, string postID, IQuest quest)
        {
            var vote = voteLinesGrouped.FirstOrDefault(v => v.Value == VoteType.Vote).Key;

            if (vote != null)
            {
                // Remove the post author from any other existing votes.
                RemoveSupport(postAuthor, VoteType.Vote);
                // Add/update the post author's post ID to the tracking hashset.
                VoterMessageId[postAuthor] = postID;

                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<string> votePartitions = GetVotePartitions(vote, quest, VoteType.Vote);

                foreach (var votePartition in votePartitions)
                {
                    // Find any existing vote that matches the current vote partition.
                    string voteKey = GetVoteKey(votePartition, quest, VoteType.Vote);

                    // Update the supporters list, and save this voter's post ID for linking.
                    VotesWithSupporters[voteKey].Add(postAuthor);
                }
            }
        }

        /// <summary>
        /// Put any ranking votes found in the grouped vote lines into the standard tracking sets.
        /// </summary>
        /// <param name="voteLinesGrouped"></param>
        /// <param name="postAuthor"></param>
        /// <param name="postID"></param>
        /// <param name="quest"></param>
        private void ProcessRanks(Dictionary<List<string>, VoteType> voteLinesGrouped, string postAuthor, string postID, IQuest quest)
        {
            var ranks = voteLinesGrouped.Where(v => v.Value == VoteType.Rank).Select(vs => vs.Key);

            // Remove the post author from any other existing votes.
            RemoveSupport(postAuthor, VoteType.Rank);

            RankedVoterMessageId[postAuthor] = postID;

            foreach (var rankLines in ranks)
            {
                List<string> votePartitions = GetVotePartitions(rankLines, quest, VoteType.Rank);

                foreach (var votePartition in votePartitions)
                {
                    // Find any existing vote that matches the current vote partition.
                    string voteKey = GetVoteKey(votePartition, quest, VoteType.Rank);

                    // Update the supporters list, and save this voter's post ID for linking.
                    RankedVotesWithSupporters[voteKey].Add(postAuthor);
                }
            }
        }

        /// <summary>
        /// Determine if the provided post text is someone posting the results of a tally.
        /// </summary>
        /// <param name="postText">The text of the post to check.</param>
        /// <returns>Returns true if the post contains tally results.</returns>
        private bool IsTallyPost(string postText)
        {
            // If the post contains ##### at the start of the line for part of its text, it's a tally result.
            string cleanText = VoteLine.CleanVote(postText);
            return (tallyRegex.Matches(cleanText).Count > 0);
        }

        /// <summary>
        /// Convert a collection of regex matches into a list of strings.
        /// </summary>
        /// <param name="matches">Collection of regex matches.</param>
        /// <returns>Returns the list of strings corresponding to the matched values.</returns>
        private List<string> GetVoteLineStrings(MatchCollection matches)
        {
            var strings = from Match m in matches
                          select m.Value.Trim();

            return strings.ToList();
        }

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
        private Dictionary<List<string>, VoteType> SeparateVoteTypes(List<string> postLines)
        {
            if (postLines == null || postLines.Count == 0)
                throw new ArgumentNullException(nameof(postLines));

            Dictionary<List<string>, VoteType> results = new Dictionary<List<string>, VoteType>();

            // First put together all base plans
            while (postLines.Count > 0 && basePlanRegex.Match(postLines.First()).Success)
            {
                List<string> basePlan = new List<string>();
                // Add the "Base Plan" line
                basePlan.Add(postLines.First());
                // Add all sub-lines after that (-[x])
                basePlan.AddRange(postLines.Skip(1).TakeWhile(a => a.StartsWith("-")));

                // As long as the plan has component lines, add it to the grouping
                // collection.  If it has no component lines, it gets ignored, but
                // we keep trying to see if there are any more base plans.
                if (basePlan.Count > 1)
                {
                    results.Add(basePlan, VoteType.Plan);
                }

                postLines = new List<string>(postLines.Skip(basePlan.Count));
            }


            // Then put together the normal vote
            List<string> normalVote = postLines.TakeWhile(a => VoteLine.IsRankedVote(a) == false).ToList();

            if (normalVote.Count > 0)
                results.Add(normalVote, VoteType.Vote);


            // Then put together all rank vote lines, each as a separate entry.
            if (postLines.Count > normalVote.Count)
            {
                var rankLines = postLines.Skip(normalVote.Count);

                foreach (string line in rankLines)
                {
                    if (VoteLine.IsRankedVote(line))
                    {
                        results.Add(new List<string>(1) { line }, VoteType.Rank);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Given a list of lines that corresponds to a base plan as part of a user's post,
        /// extract the name of the plan.
        /// </summary>
        /// <param name="planLines">Vote lines that start with a Base Plan name.</param>
        /// <returns>Returns the name of the base plan.</returns>
        private string GetBasePlanName(List<string> planLines)
        {
            string firstLine = planLines.First();
            string lineContent = VoteLine.GetVoteContent(firstLine);

            Match m = basePlanRegex.Match(lineContent);
            if (m.Success)
            {
                string planName = m.Groups["baseplan"].Value.Trim();

                planName = Utility.Text.PlanNameMarker + planName;

                return planName;
            }

            throw new InvalidOperationException("These are not the lines for a base plan.");
        }

        /// <summary>
        /// Given a list of lines that corresponds to a base plan as part of a user's post,
        /// remove the line with the plan's name, and promote all other lines one level.
        /// Promotion = Turn a -[x] line to a [x] line.
        /// </summary>
        /// <param name="planLines">Vote lines that start with a Base Plan name.</param>
        /// <returns>Returns the plan's vote lines as if they were their own vote.</returns>
        private List<string> PromotePlanLines(List<string> planLines)
        {
            var promotedLines = from p in planLines.Skip(1)
                                select p.Substring(1);

            return promotedLines.ToList();
        }

        /// <summary>
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        /// <param name="votesDict">Vote support dictionary to remove voter support from.</param>
        private void RemoveSupport(string voter, VoteType voteType)
        {
            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;

            List<string> emptyVotes = new List<string>();

            foreach (var vote in votesDict)
            {
                if (vote.Value.Remove(voter))
                {
                    if (vote.Value.Count == 0)
                    {
                        emptyVotes.Add(vote.Key);
                    }
                }
            }

            foreach (var vote in emptyVotes)
            {
                votesDict.Remove(vote);
            }
        }

        /// <summary>
        /// Will remove the specified voter from the voter ID list if there are no
        /// votes that they are currently supporting.
        /// </summary>
        /// <param name="voter">The voter to trim.</param>
        /// <param name="voteType">The type of vote to check.</param>
        private void TrimVoter(string voter, VoteType voteType)
        {
            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;
            var votersDict = voteType == VoteType.Rank ? RankedVoterMessageId : VoterMessageId;

            if (!votesDict.Values.Any(v => v.Contains(voter)))
            {
                votersDict.Remove(voter);
            }
        }

        /// <summary>
        /// Given a list of vote lines, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="lines">List of valid vote lines.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> GetVotePartitions(IEnumerable<string> lines, IQuest quest, VoteType voteType)
        {
            List<string> partitions = new List<string>();
            StringBuilder sb = new StringBuilder();
            StringBuilder holding_sb = new StringBuilder();
            string currentTask = "";
            string taskHeader = "";
            bool addedTopLevelLine = false;
            string prefix;
            string marker;
            string task;
            string content;

            // Work through the list of matched lines
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // Ranked vote lines are all treated individually.
                if (voteType == VoteType.Rank)
                {
                    partitions.Add(trimmedLine + "\r\n");
                    continue;
                }

                // If a line refers to another voter or base plan, pull that voter's votes
                var referralVotes = FindVotesForVoter(trimmedLine);
                if (referralVotes.Count > 0)
                {
                    // If we're not using vote partitions, append all lines onto the current vote string.
                    // Otherwise, add each of the other voter's votes to our partition list.
                    if (quest.PartitionMode == PartitionMode.None)
                    {
                        foreach (var v in referralVotes)
                            sb.Append(v);
                    }
                    else
                    {
                        partitions.AddRange(referralVotes);
                    }

                    // Go to the next vote line if we were successful in pulling a referral vote.
                    continue;
                }

                // For lines that don't refer to other voters, just keep adding the
                // lines to the complete vote if we're not partitioning them, or
                // compile them into unit blocks if we're using vote partitions.
                if (quest.PartitionMode == PartitionMode.None)
                {
                    sb.AppendLine(trimmedLine);
                }
                else if (quest.PartitionMode == PartitionMode.ByLine)
                {
                    // If partitioning by line, every line gets added to the partitions list.
                    partitions.Add(trimmedLine + "\r\n");
                }
                else if (voteType == VoteType.Plan)
                {
                    // If partitioning a Base Plan by block or task, simply collate all lines together.
                    // The entire plan is considered a single block.
                    sb.AppendLine(trimmedLine);
                }
                else if (quest.PartitionMode == PartitionMode.ByBlock)
                {
                    // If partitioning a vote by block, work on collecting chunks together.
                    if (sb.Length == 0)
                    {
                        sb.AppendLine(trimmedLine);
                    }
                    else if (VoteLine.CleanVote(trimmedLine).StartsWith("-"))
                    {
                        sb.AppendLine(trimmedLine);
                    }
                    else
                    {
                        partitions.Add(sb.ToString());
                        sb.Clear();
                        sb.AppendLine(trimmedLine);
                    }
                }
                else if (quest.PartitionMode == PartitionMode.ByTask)
                {
                    // Group lines by task

                    // If string builder is empty, start adding.
                    if (sb.Length == 0)
                    {
                        sb.AppendLine(trimmedLine);
                    }
                    else if (VoteLine.GetVoteTask(trimmedLine) != string.Empty)
                    {
                        // We've reached a new task block
                        partitions.Add(sb.ToString());
                        sb.Clear();
                        sb.AppendLine(trimmedLine);
                    }
                    else
                    {
                        sb.AppendLine(trimmedLine);
                    }
                }
                else if (quest.PartitionMode == PartitionMode.ByTaskBlock)
                {
                    // A blend of task and block breakdowns
                    // Top-level elements are retained within the current block if
                    // we're inside a task segment.
                    // However top-level elements with sub-elements get their own partition even if a
                    // new task wasn't on that line.
                    // Applies task name to each sub-block encountered.

                    // Get vote line components, since we'll be using them a bunch
                    VoteLine.GetVoteComponents(trimmedLine, out prefix, out marker, out task, out content);


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

                        sb.AppendLine(trimmedLine);

                        // Save details
                        addedTopLevelLine = (prefix == string.Empty);
                        currentTask = task;

                        if (content == string.Empty)
                            taskHeader = trimmedLine;
                        else
                            taskHeader = "";
                    }
                    else if (sb.Length == 0)
                    {
                        // If string builder is empty, start adding new stuff.
                        sb.AppendLine(trimmedLine);

                        // Save details
                        addedTopLevelLine = (prefix == string.Empty);
                        currentTask = task;

                        // If the line is nothing but a task (no content), save this as a task header
                        if (task != string.Empty && content == string.Empty)
                            taskHeader = trimmedLine;
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
                            holding_sb.AppendLine(trimmedLine);
                        }
                        else
                        {
                            // If it's a sub-line, we started a new block
                            if (sb.Length > 0)
                            {
                                // If the current sb has any actual content in it, add to the partitions
                                if (VoteLine.GetVoteContent(sb.ToString()) != string.Empty)
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
                                string tasked_holding_line = VoteLine.ReplaceTask(holding_sb.ToString(), currentTask);
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
                            sb.AppendLine(trimmedLine);
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
                            sb.AppendLine(trimmedLine);
                            addedTopLevelLine = false;
                        }

                        // If we're adding a new top-level line, it gets added to the holding string
                        // if the previous line was also top-level.
                        
                        else if (addedTopLevelLine)
                        {
                            holding_sb.AppendLine(trimmedLine);
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

                            if (taskHeader != string.Empty)
                            {
                                sb.AppendLine(taskHeader);
                                sb.AppendLine(trimmedLine);
                            }
                            else if (currentTask != string.Empty)
                            {
                                sb.AppendLine(VoteLine.ReplaceTask(trimmedLine, currentTask));
                            }
                            else
                            {
                                sb.AppendLine(trimmedLine);
                            }

                            addedTopLevelLine = true;
                        }
                    }
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

            // Make sure any BBCode formatting is cleaned up in each partition result.
            CloseFormattingTags(partitions);

            return partitions;
        }

        /// <summary>
        /// Find all votes tied to a given vote line.
        /// The "plan name" (possibly user name) is checked with the
        /// standard and alternate extractions (adding a special marker character
        /// depending on whether the word "plan" is used, and whether it's 
        /// standard or alt) in order to look up votes that said (possible) voter
        /// supports.
        /// </summary>
        /// <param name="voteLine">The vote line to be checked.</param>
        /// <returns>Returns a list of all votes supported by the user or plan
        /// specified in the vote line, if found.  Otherwise returns an
        /// empty list.</returns>
        private List<string> FindVotesForVoter(string voteLine)
        {
            string planName = VoteLine.GetVotePlanName(voteLine);

            var planVotes = VotesWithSupporters.Where(v => v.Value.Contains(planName));

            if (planVotes.Count() > 0)
                return planVotes.Select(v => v.Key).ToList();

            planName = VoteLine.GetAltVotePlanName(voteLine);

            planVotes = VotesWithSupporters.Where(v => v.Value.Contains(planName));

            if (planVotes.Count() > 0)
                return planVotes.Select(v => v.Key).ToList();

            return new List<string>();
        }

        /// <summary>
        /// Make sure each vote string in the provided list closes any opened BBCode formatting it uses,
        /// and that orphan closing tags are removed.
        /// </summary>
        /// <param name="partitions">List of vote strings.</param>
        private void CloseFormattingTags(List<string> partitions)
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
        /// Attempt to find any existing vote that matches with the vote we have,
        /// and can be used as a key in the VotesWithSupporters table.
        /// </summary>
        /// <param name="vote">The vote to search for.</param>
        /// <returns>Returns the string that can be used as a key in the VotesWithSupporters table.</returns>
        private string GetVoteKey(string vote, IQuest quest, VoteType voteType)
        {
            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;

            // If the vote already matches an existing key, we don't need to search again.
            if (votesDict.ContainsKey(vote))
            {
                return vote;
            }

            var minVote = VoteLine.MinimizeVote(vote, quest);

            // If it matches a lookup value, use the existing key
            string lookupVote = string.Empty;
            if (cleanVoteLookup.TryGetValue(minVote, out lookupVote))
            {
                if (!votesDict.ContainsKey(lookupVote))
                    votesDict[lookupVote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return lookupVote;
            }

            cleanVoteLookup[minVote] = vote;

            // Otherwise create a new hashtable for vote supporters for the new vote key.
            votesDict[vote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return vote;
        }
        #endregion
    }
}
