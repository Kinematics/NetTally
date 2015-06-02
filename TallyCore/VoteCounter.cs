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
        readonly Regex voteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\[[xX+✓✔]\].*", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex rankVoteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\[[xX+✓✔1-9]\].*", RegexOptions.Multiline);
        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        readonly Regex basePlanRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\[[xX+✓✔]\]\s*base\s*plan(:|\s)+(?<baseplan>.+)$", RegexOptions.IgnoreCase);
        // A voter referral is a user name on a vote line, possibly starting with 'Plan'.
        readonly Regex voterRegex = new Regex(@"^-*\[[xX+✓✔]\]\s*(plan\s+)?(?<name>.*?)[.]?$", RegexOptions.IgnoreCase);
        // A voter referral is a user name on a vote line, possibly starting with 'Plan'.
        readonly Regex rankVoteLineRegex = new Regex(@"^\[[1-9]\].+");
        // Regex to match any markup that we'll want to remove during comparisons.
        readonly Regex markupRegex = new Regex(@"\[/?[ibu]\]|\[color[^]]*\]|\[/color\]");
        // Regex to allow us to collapse a vote to a commonly comparable version.
        readonly Regex collapseRegex = new Regex(@"\s|\.");
        // Regex to allow us to strip leading dashes from a per-line vote.
        readonly Regex leadHyphenRegex = new Regex(@"^-+");
        #endregion

        #region Public Interface
        public string Title { get; set; } = string.Empty;

        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> RankedVoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> PlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool HasRankedVotes => RankedVotesWithSupporters.Count > 0;

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
                                     where forumAdapter.GetAuthorOfPost(post) != threadAuthor &&
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
                string planName = GetPlanName(plan);

                // Remove the post author from any other existing votes.
                RemoveSupport(planName, VotesWithSupporters);
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
                    string voteKey = GetVoteKey(votePartition, quest);

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
                RemoveSupport(postAuthor, VotesWithSupporters);
                // Add/update the post author's post ID to the tracking hashset.
                VoterMessageId[postAuthor] = postID;

                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<string> votePartitions = GetVotePartitions(vote, quest, VoteType.Vote);

                foreach (var votePartition in votePartitions)
                {
                    // Find any existing vote that matches the current vote partition.
                    string voteKey = GetVoteKey(votePartition, quest);

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

            foreach (var rankLines in ranks)
            {
                // Remove the post author from any other existing votes.
                RemoveSupport(postAuthor, RankedVotesWithSupporters);

                RankedVoterMessageId[postAuthor] = postID;

                List<string> votePartitions = GetVotePartitions(rankLines, quest, VoteType.Rank);

                foreach (var votePartition in votePartitions)
                {
                    // Find any existing vote that matches the current vote partition.
                    string voteKey = GetRankedVoteKey(votePartition, quest);

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
            string cleanText = CleanVote(postText);
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
            List<string> normalVote = postLines.TakeWhile(a => rankVoteLineRegex.Match(CleanVote(a)).Success == false).ToList();

            if (normalVote.Count > 0)
                results.Add(normalVote, VoteType.Vote);

            // Then put together all rank vote lines, each as a separate entry.
            if (postLines.Count > normalVote.Count)
            {
                var rankLines = postLines.Skip(normalVote.Count);

                foreach (string line in rankLines)
                {
                    string nonMarkupLine = CleanVote(line);

                    Match m = rankVoteLineRegex.Match(nonMarkupLine);

                    if (m.Success)
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
        private string GetPlanName(List<string> planLines)
        {
            Match m = basePlanRegex.Match(planLines.First());
            if (m.Success)
            {
                return m.Groups["baseplan"].Value.Trim();
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

            return new List<string>(promotedLines);
        }

        /// <summary>
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        /// <param name="votesDict">Vote support dictionary to remove voter support from.</param>
        private void RemoveSupport(string voter, Dictionary<string, HashSet<string>> votesDict)
        {
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
        /// Given a list of vote lines, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="lines">List of valid vote lines.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> GetVotePartitions(IEnumerable<string> lines, IQuest quest, VoteType voteType)
        {
            List<string> partitions = new List<string>();
            StringBuilder sb = new StringBuilder();

            // Work through the list of matched lines
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (voteType == VoteType.Rank)
                {
                    partitions.Add(trimmedLine + "\r\n");
                    continue;
                }

                // If a line refers to another voter, pull that voter's votes
                Match vm = voterRegex.Match(CleanVote(trimmedLine));
                if (vm.Success)
                {
                    var referralVotes = FindVotesForVoter(vm.Groups["name"].Value);

                    if (referralVotes.Count > 0)
                    {
                        // If we're using vote partitions, add each of the other voter's
                        // votes to our partition list.  Otherwise, append them all onto
                        // the currently being built string.
                        if (quest.UseVotePartitions)
                        {
                            partitions.AddRange(referralVotes);
                        }
                        else
                        {
                            foreach (var v in referralVotes)
                                sb.Append(v);
                        }

                        // Go to the next vote line if we were successful in pulling a referral vote.
                        continue;
                    }
                }

                // For lines that don't refer to other voters, compile them into
                // unit blocks if we're using vote partitions, or just add to the
                // end of the total string if not.
                if (quest.UseVotePartitions)
                {
                    if (quest.PartitionByLine)
                    {
                        // If partitioning by line, every line gets added to the partitions list.
                        partitions.Add(trimmedLine+"\r\n");
                    }
                    else if (voteType == VoteType.Plan)
                    {
                        // If partitioning a Base Plan by block, simply collate all lines together.
                        // The entire plan is considered a single block.
                        sb.AppendLine(trimmedLine);
                    }
                    else
                    {
                        // If partitioning a vote by block, work on collecting chunks together.
                        if (sb.Length == 0)
                        {
                            sb.AppendLine(trimmedLine);
                        }
                        else if (CleanVote(trimmedLine).StartsWith("-"))
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
                }
                else
                {
                    sb.AppendLine(trimmedLine);
                }
            }


            if (sb.Length > 0)
                partitions.Add(sb.ToString());

            // Make sure any BBCode formatting is cleaned up in each partition result.
            CloseFormattingTags(partitions);

            return partitions;
        }

        /// <summary>
        /// Find all votes that a given voter is supporting.
        /// </summary>
        /// <param name="voter">The name of the voter.</param>
        /// <returns>A list of all votes that that voter currently supports.</returns>
        private List<string> FindVotesForVoter(string voter)
        {
            var votes = from v in VotesWithSupporters
                        where v.Value.Contains(voter)
                        select v.Key;

            return votes.ToList();
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
        private string GetVoteKey(string vote, IQuest quest)
        {
            // If the vote already matches an existing key, we don't need to search again.
            if (VotesWithSupporters.ContainsKey(vote))
            {
                return vote;
            }

            var minVote = MinimizeVote(vote, quest);

            // If it matches a lookup value, use the existing key
            string lookupVote = string.Empty;
            if (cleanVoteLookup.TryGetValue(minVote, out lookupVote))
            {
                if (!VotesWithSupporters.ContainsKey(lookupVote))
                    VotesWithSupporters[lookupVote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return lookupVote;
            }

            cleanVoteLookup[minVote] = vote;

            // Otherwise create a new hashtable for vote supporters for the new vote key.
            VotesWithSupporters[vote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return vote;
        }

        /// <summary>
        /// Attempt to find any existing vote that matches with the vote we have,
        /// and can be used as a key in the VotesWithSupporters table.
        /// </summary>
        /// <param name="vote">The vote to search for.</param>
        /// <returns>Returns the string that can be used as a key in the VotesWithSupporters table.</returns>
        private string GetRankedVoteKey(string vote, IQuest quest)
        {
            // If the vote already matches an existing key, we don't need to search again.
            if (RankedVotesWithSupporters.ContainsKey(vote))
            {
                return vote;
            }

            var minVote = MinimizeVote(vote, quest);

            // If it matches a lookup value, use the existing key
            string lookupVote = string.Empty;
            if (cleanVoteLookup.TryGetValue(minVote, out lookupVote))
            {
                if (!RankedVotesWithSupporters.ContainsKey(lookupVote))
                    RankedVotesWithSupporters[lookupVote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return lookupVote;
            }

            cleanVoteLookup[minVote] = vote;

            // Otherwise create a new hashtable for vote supporters for the new vote key.
            RankedVotesWithSupporters[vote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return vote;
        }

        /// <summary>
        /// Given a vote line, remove any BBCode formatting chunks.
        /// </summary>
        /// <param name="voteLine">The vote line to examine.</param>
        /// <returns>Returns the vote line without any BBCode formatting.</returns>
        private string CleanVote(string voteLine)
        {
            return markupRegex.Replace(voteLine, "");
        }

        /// <summary>
        /// Collapse a vote to a minimized form, for comparison.
        /// All BBCode markup is removed, along with all spaces and periods,
        /// and leading dashes when partitioning by line.  The text is then
        /// lowercased.
        /// </summary>
        /// <param name="vote">Original vote line to minimize.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns a minimized version of the vote string.</returns>
        private string MinimizeVote(string vote, IQuest quest)
        {
            string cleaned = CleanVote(vote);
            cleaned = collapseRegex.Replace(cleaned, "");
            cleaned = cleaned.ToLower();
            if (quest.UseVotePartitions && quest.PartitionByLine)
                cleaned = leadHyphenRegex.Replace(cleaned, "");

            return cleaned;
        }
        #endregion
    }
}
