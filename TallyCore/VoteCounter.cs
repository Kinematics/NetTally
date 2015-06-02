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
        readonly Regex tallyRegex = new Regex(@"^(\[/?[ibu]\]|\[color[^]]+\])*#####", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex voteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\[[xX+✓✔]\].*", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        readonly Regex rankVoteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\[[xX+✓✔1-9]\].*", RegexOptions.Multiline);
        // Check for a vote line that marks a portion of the user's post as an abstract base plan.
        readonly Regex basePlanRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\[[xX+✓✔]\]\s*base\s*plan(:|\s)+(?<baseplan>.+)$", RegexOptions.IgnoreCase);
        // A voter referral is a user name on a vote line, possibly starting with 'Plan'.
        readonly Regex voterRegex = new Regex(@"^\s*-*\[[xX+✓✔]\]\s*([pP][lL][aA][nN]\s*)?(?<name>.*?)[.]?\s*$");
        // A voter referral is a user name on a vote line, possibly starting with 'Plan'.
        readonly Regex rankVoteLineRegex = new Regex(@"^\[[1-9]\].+");
        // Regex to match any markup that we'll want to remove during comparisons.
        readonly Regex markupRegex = new Regex(@"\[/?[ibu]\]|\[color[^]]+\]|\[/color\]");
        // Clean extraneous information from a vote in order to compare with other votes.
        readonly Regex cleanRegex = new Regex(@"(\[/?[ibu]\]|\[color[^]]+\]|\[/color\]|\s|\.)");
        // Clean extraneous information from a vote line in order to compare with other votes.
        readonly Regex cleanLinePartRegex = new Regex(@"(^-+|\[/?[ibu]\]|\[color[^]]+\]|\[/color\]|\s|\.)");
        // Strip BBCode formatting from a vote line.  Use with Replace().
        readonly Regex stripFormattingRegex = new Regex(@"\[/?[ibu]\]|\[/?color[^]]*\]");

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
            MatchCollection matches = voteRegex.Matches(postText);
            if (matches.Count > 0)
            {
                List<string> matchStrings = GetVoteLineStrings(matches);

                Dictionary<List<string>, VoteType> voteLinesGrouped = SeparateVoteTypes(matchStrings);

                var plans = voteLinesGrouped.Where(v => v.Value == VoteType.Plan).Select(vs => vs.Key);

                foreach (var plan in plans)
                {
                    string planName = GetPlanName(plan);

                    // Remove the post author from any other existing votes.
                    RemoveSupport(planName);

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
                        VoterMessageId[planName] = postID;
                        PlanNames.Add(planName);
                    }
                }
                
                var vote = voteLinesGrouped.FirstOrDefault(v => v.Value == VoteType.Vote).Key;

                if (vote != null)
                {
                    // Remove the post author from any other existing votes.
                    RemoveSupport(postAuthor);

                    // Get the list of all vote partitions, built according to current preferences.
                    // One of: By line, By block, or By post (ie: entire vote)
                    List<string> votePartitions = GetVotePartitions(vote, quest, VoteType.Vote);

                    foreach (var votePartition in votePartitions)
                    {
                        // Find any existing vote that matches the current vote partition.
                        string voteKey = GetVoteKey(votePartition, quest);

                        // Update the supporters list, and save this voter's post ID for linking.
                        VotesWithSupporters[voteKey].Add(postAuthor);
                        VoterMessageId[postAuthor] = postID;
                    }
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
            // If the post contains ##### at the start of the line for part of its text,
            // it's a tally result.
            return (tallyRegex.Matches(postText).Count > 0);
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

            return new List<string>(strings);
        }

        /// <summary>
        /// Given a list of vote lines from a post, separate out any that correspond to a
        /// defined Base Plan, separate from the user's own vote.
        /// </summary>
        /// <param name="postLines">All the vote lines of the post.</param>
        /// <returns>Returns a dict with lists of strings, each labeled according to
        /// the section of the post vote they correspond to (either plan or vote).</returns>
        private Dictionary<List<string>, VoteType> SeparateVoteTypes(List<string> postLines)
        {
            if (postLines == null || postLines.Count == 0)
                throw new ArgumentNullException(nameof(postLines));

            Dictionary<List<string>, VoteType> results = new Dictionary<List<string>, VoteType>();

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

            if (postLines.Count > 0)
                results.Add(postLines, VoteType.Vote);

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
        private void RemoveSupport(string voter)
        {
            List<string> emptyVotes = new List<string>();

            foreach (var vote in VotesWithSupporters)
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
                VotesWithSupporters.Remove(vote);
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
                // If a line refers to another voter, pull that voter's votes
                Match vm = voterRegex.Match(StripFormatting(line));
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

                string trimmedLine = line.Trim();

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
                        else if (StripFormatting(trimmedLine).StartsWith("-"))
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
        /// Given a vote line, strip off any leading BBCode formatting chunks.
        /// </summary>
        /// <param name="voteLine">The vote line to examine.</param>
        /// <returns>Returns the vote line without any leading formatting.</returns>
        private string StripFormatting(string voteLine)
        {
            return stripFormattingRegex.Replace(voteLine, "");
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

            var cleanVote = CleanVote(vote, quest);

            // If it matches a lookup value, use the existing key
            string lookupVote = string.Empty;
            if (cleanVoteLookup.TryGetValue(cleanVote, out lookupVote))
            {
                if (!VotesWithSupporters.ContainsKey(lookupVote))
                    VotesWithSupporters[lookupVote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return lookupVote;
            }

            cleanVoteLookup[cleanVote] = vote;

            // Otherwise create a new hashtable for vote supporters for the new vote key.
            VotesWithSupporters[vote] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            return vote;
        }

        /// <summary>
        /// Strip all markup from the vote.
        /// </summary>
        /// <param name="vote">Original vote with possible markup.</param>
        /// <returns>Return the vote without any BBCode markup.</returns>
        private string CleanVote(string vote, IQuest quest)
        {
            if (quest.UseVotePartitions && quest.PartitionByLine)
                return cleanLinePartRegex.Replace(vote, "").ToLower();
            else
                return cleanRegex.Replace(vote, "").ToLower();
        }
        #endregion
    }
}
