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
        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        Regex tallyRegex = new Regex(@"^#####", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        Regex voteRegex = new Regex(@"^\s*-*\[[xX]\].*", RegexOptions.Multiline);
        // A voter referral is a user name on a vote line, possibly starting with 'Plan'.
        Regex voterRegex = new Regex(@"^\s*-*\[[xX]\]\s*([pP][lL][aA][nN]\s*)?(?<name>.*?)[.]?\s*$");
        // Clean extraneous information from a vote in order to compare with other votes.
        Regex cleanRegex = new Regex(@"(\[/?[ibu]\]|\[color[^]]+\]|\[/color\]|\s|\.)");
        // Clean extraneous information from a vote line in order to compare with other votes.
        Regex cleanLinePartRegex = new Regex(@"(^-+|\[/?[ibu]\]|\[color[^]]+\]|\[/color\]|\s|\.)");

        Dictionary<string, string> cleanVoteLookup = new Dictionary<string, string>();
        string threadAuthor = string.Empty;

        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public bool UseVotePartitions { get; set; } = false;
        public bool PartitionByLine { get; set; } = true;


        private void Reset()
        {
            VotesWithSupporters.Clear();
            VoterMessageId.Clear();
            cleanVoteLookup.Clear();
            threadAuthor = string.Empty;
        }

        /// <summary>
        /// Construct the votes Results from the provide list of HTML pages.
        /// </summary>
        /// <param name="pages"></param>
        public void TallyVotes(List<HtmlDocument> pages, int startPost, int endPost)
        {
            if (pages == null)
                throw new ArgumentNullException(nameof(pages));

            Reset();

            foreach (var page in pages)
            {
                if (page == null)
                    continue;

                // Root of the tree.  Make sure we actually have a document.
                var root = page.DocumentNode;
                if (!root.HasChildNodes)
                    continue;

                // Set the thread author for reference.
                SetThreadAuthor(root);

                // Find the ordered list containing all the messages on this page.
                var postList = root.Descendants("ol").First(n => n.Id == "messageList");

                // Process each <li> child node as a message.
                foreach (var post in postList.ChildNodes.Where(n => n.Name == "li"))
                {
                    ProcessPost(post, startPost, endPost);
                }
            }
        }

        /// <summary>
        /// Function to set the thread author for the current processing run.
        /// </summary>
        /// <param name="root">Root node of a page of the thread.</param>
        private void SetThreadAuthor(HtmlNode root)
        {
            if (threadAuthor == string.Empty)
            {
                var pageDesc = root.Descendants("p").First(n => n.Id == "pageDescription");
                var authorAnchor = pageDesc.Elements("a").First(n => n.GetAttributeValue("class", "") == "username");
                threadAuthor = authorAnchor.InnerText;
            }
        }

        /// <summary>
        /// Function to process individual posts within the thread.
        /// Updates the vote records maintained in the class.
        /// </summary>
        /// <param name="post">The list item node containing the post.</param>
        /// <param name="startPost">The first post number of the thread to check.</param>
        /// <param name="endPost">The last post number of the thread to check.</param>
        private void ProcessPost(HtmlNode post, int startPost, int endPost)
        {
            string postAuthor;
            string postID;
            string postText;

            MatchCollection matches;

            int postNumber = GetPostNumber(post);

            // Ignore posts outside the selected numeric post range.
            if (postNumber < startPost || (endPost > 0 && postNumber > endPost))
                return;

            // The post author is contained in the <li> element.
            postAuthor = post.GetAttributeValue("data-author", "");

            // Ignore posts by the thread author.
            if (postAuthor == threadAuthor)
                return;

            // The post ID is also contained in the <li> element.
            postID = post.Id;
            // Extract only the numeric portion.
            if (postID.StartsWith("post-"))
            {
                postID = postID.Substring("post-".Length);
            }

            // Get the text from the post that can be searched for a vote.
            postText = ExtractPostText(post);

            // If the post contains ##### at the start of the line for part of its text,
            // it's a tally result.  Skip the post.
            matches = tallyRegex.Matches(postText);
            if (matches.Count > 0)
                return;

            // Pull out actual vote lines from the post.
            matches = voteRegex.Matches(postText);
            if (matches.Count > 0)
            {
                // Remove the post author from any other existing votes.
                RemoveSupport(postAuthor);

                // Build the blocks of the vote, possibly partitioned, and including
                // referral votes.
                List<string> votePartitions = CombineVotePartitions(matches);

                foreach (var votePartition in votePartitions)
                {
                    // If the vote names another user, pull that user's vote to fill in this vote. (not for partitioned votes)
                    // Also check for other votes that might match except for BBCode.
                    string voteKey = FindMatchingVote(votePartition, matches);

                    // Create a new hashtable for vote supporters, if necessary.
                    if (!VotesWithSupporters.ContainsKey(voteKey))
                    {
                        VotesWithSupporters[voteKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    }

                    // Update the supporters list, and save this voter's post ID for linking.
                    VotesWithSupporters[voteKey].Add(postAuthor);
                    VoterMessageId[postAuthor] = postID;
                }
            }
        }

        /// <summary>
        /// Given a collection of vote line matches, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="matches">Matches for a valid vote line.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> CombineVotePartitions(MatchCollection matches)
        {
            List<string> matchStrings = new List<string>();
            foreach (Match match in matches)
            {
                matchStrings.Add(match.Value);
            }

            return CombineVotePartitions(matchStrings);
        }

        /// <summary>
        /// Given a list of vote lines, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="lines">List of valid vote lines.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> CombineVotePartitions(List<string> lines)
        {
            List<string> partitions = new List<string>();
            StringBuilder sb = new StringBuilder();

            // Work through the list of matched lines
            foreach (string line in lines)
            {
                // If a line refers to another voter, pull that voter's votes
                Match vm = voterRegex.Match(line);
                if (vm.Success)
                {
                    var referralVotes = FindVotesForVoter(vm.Groups["name"].Value);

                    if (referralVotes.Count > 0)
                    {
                        // If we're using vote partitions, add each of the other voter's
                        // votes to our partition list.  Otherwise, append them all onto
                        // the currently being built string.
                        if (UseVotePartitions)
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
                if (UseVotePartitions)
                {
                    if (sb.Length == 0)
                    {
                        sb.AppendLine(line.Trim());
                    }
                    else if (PartitionByLine)
                    {
                        partitions.Add(sb.ToString());
                        sb.Clear();
                        sb.AppendLine(line.Trim());
                    }
                    else if (line.Trim().StartsWith("-"))
                    {
                        sb.AppendLine(line.Trim());
                    }
                    else
                    {
                        partitions.Add(sb.ToString());
                        sb.Clear();
                        sb.AppendLine(line.Trim());
                    }
                }
                else
                {
                    sb.AppendLine(line.Trim());
                }
            }


            if (sb.Length > 0)
                partitions.Add(sb.ToString());

            return partitions;
        }

        /// <summary>
        /// Given an li node containing a post message, extract the thread sequence number from it.
        /// </summary>
        /// <param name="post">LI node containing a post message.</param>
        /// <returns>Returns the numeric thread sequence number of the post.</returns>
        private int GetPostNumber(HtmlNode post)
        {
            int postNum = 0;

            try
            {
                // post > div.primaryContent > div.messageMeta > div.publicControls > a.postNumber

                // Step down into the post to find the post number value.
                //var a = post.Elements("div").First(n => n.GetAttributeValue("class", "").Contains("primaryContent"));
                //var b = a.Elements("div").First(n => n.GetAttributeValue("class", "").Contains("messageMeta"));
                //var c = b.Elements("div").First(n => n.GetAttributeValue("class", "").Contains("publicControls"));
                //var d = c.Elements("a").First(n => n.GetAttributeValue("class", "").Contains("postNumber"));

                // Find the anchor node that contains the post number value.
                var anchor = post.Descendants("a").First(n => n.GetAttributeValue("class", "").Contains("postNumber"));

                // Post number is written as #1123.  Remove the leading #.
                var postNumText = anchor.InnerText;
                if (postNumText.StartsWith("#"))
                    postNumText = postNumText.Substring(1);

                int.TryParse(postNumText, out postNum);
            }
            catch (Exception)
            {
                // If any of the above fail, just return 0 as the post number.
            }

            return postNum;
        }

        /// <summary>
        /// Extract the text of the provided post, ignoring quotes, spoilers, or other
        /// invalid regions.  Clean up HTML entities as well.
        /// </summary>
        /// <param name="post">The li HTML node containing the user post.</param>
        /// <returns>A string containing all valid text lines.</returns>
        private string ExtractPostText(HtmlNode post)
        {
            StringBuilder sb = new StringBuilder();

            // Extract the actual contents of the post.
            var postArticle = post.Descendants("article").First();
            var articleBlock = postArticle.Element("blockquote");

            // Search the post for valid element types, and put them all together
            // into a single string.
            foreach (var node in articleBlock.ChildNodes)
            {
                if (node.Name == "br")
                {
                    sb.AppendLine("");
                    continue;
                }

                if (node.InnerText.Trim() == string.Empty)
                    continue;

                switch (node.Name)
                {
                    case "#text":
                        sb.Append(node.InnerText);
                        break;
                    case "i":
                        sb.Append("[i]");
                        sb.Append(node.InnerText);
                        sb.Append("[/i]");
                        break;
                    case "b":
                        sb.Append("[b]");
                        sb.Append(node.InnerText);
                        sb.Append("[/b]");
                        break;
                    case "u":
                        sb.Append("[u]");
                        sb.Append(node.InnerText);
                        sb.Append("[/u]");
                        break;
                    case "span":
                        string spanStyle = node.GetAttributeValue("style", "");
                        if (spanStyle.StartsWith("color:", StringComparison.OrdinalIgnoreCase))
                        {
                            string spanColor = spanStyle.Substring("color:".Length).Trim();
                            sb.Append("[color=");
                            sb.Append(spanColor);
                            sb.Append("]");
                            sb.Append(node.InnerText);
                            sb.Append("[/color]");
                        }
                        break;
                    case "a":
                        sb.Append("[url=\"");
                        sb.Append(node.GetAttributeValue("href", ""));
                        sb.Append("\"]");
                        sb.Append(node.InnerText);
                        sb.Append("[/url]");
                        break;
                    default:
                        break;
                }
            }


            string postText = sb.ToString().TrimStart();
            postText = HtmlEntity.DeEntitize(postText);

            return postText;
        }

        /// <summary>
        /// Attempt to find any existing vote that matches what the the vote we have.
        /// Allow for a comparison using a version cleaned of any BBCode.
        /// </summary>
        /// <param name="vote"></param>
        /// <param name="originalMatches"></param>
        /// <returns></returns>
        private string FindMatchingVote(string vote, MatchCollection originalMatches)
        {
            // If the vote already matches an existing key, we don't need to search again.
            if (VotesWithSupporters.ContainsKey(vote))
            {
                return vote;
            }

            var cleanVote = CleanVote(vote);

            string lookupVote = string.Empty;
            if (cleanVoteLookup.TryGetValue(cleanVote, out lookupVote))
            {
                return lookupVote;
            }

            cleanVoteLookup[cleanVote] = vote;

            return vote;
        }

        /// <summary>
        /// Strip all markup from the vote.
        /// </summary>
        /// <param name="vote">Original vote with possible markup.</param>
        /// <returns>Return the vote without any BBCode markup.</returns>
        private string CleanVote(string vote)
        {
            if (UseVotePartitions && PartitionByLine)
                return cleanLinePartRegex.Replace(vote, "").ToLower();
            else
                return cleanRegex.Replace(vote, "").ToLower();
        }

        /// <summary>
        /// Find all votes that a given voter is supporting.
        /// </summary>
        /// <param name="voter">The name of the voter.</param>
        /// <returns>A list of all votes that that voter currently supports.</returns>
        private List<string> FindVotesForVoter(string voter)
        {
            List<string> votes = new List<string>();

            foreach (var vote in VotesWithSupporters)
            {
                if (vote.Value.Contains(voter))
                {
                    votes.Add(vote.Key);
                }
            }

            return votes;
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
    }
}
