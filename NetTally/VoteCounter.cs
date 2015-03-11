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
        IForumData forumData;

        /// <summary>
        /// Constructor
        /// </summary>
        public VoteCounter(IForumData forumData)
        {
            this.forumData = forumData;
            SetupFormattingRegexes();
        }

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


        // Public properties and variables

        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public bool UseVotePartitions { get; set; } = false;
        public bool PartitionByLine { get; set; } = true;


        // Private variables

        string threadAuthor = string.Empty;

        // A post with ##### at the start of one of the lines is a posting of tally results.  Don't read it.
        Regex tallyRegex = new Regex(@"^(\[/?[ibu]\]|\[color[^]]+\])*#####", RegexOptions.Multiline);
        // A valid vote line must start with [x] or -[x] (with any number of dashes).  It must be at the start of the line.
        Regex voteRegex = new Regex(@"^(\s|\[/?[ibu]\]|\[color[^]]+\])*-*\[[xX]\].*", RegexOptions.Multiline);
        // A voter referral is a user name on a vote line, possibly starting with 'Plan'.
        Regex voterRegex = new Regex(@"^\s*-*\[[xX]\]\s*([pP][lL][aA][nN]\s*)?(?<name>.*?)[.]?\s*$");
        // Clean extraneous information from a vote in order to compare with other votes.
        Regex cleanRegex = new Regex(@"(\[/?[ibu]\]|\[color[^]]+\]|\[/color\]|\s|\.)");
        // Clean extraneous information from a vote line in order to compare with other votes.
        Regex cleanLinePartRegex = new Regex(@"(^-+|\[/?[ibu]\]|\[color[^]]+\]|\[/color\]|\s|\.)");
        // Strip BBCode formatting from a vote line.  Use with Replace().
        Regex stripFormattingRegex = new Regex(@"\[/?[ibu]\]|\[/?color[^]]*\]");
        // Bad characters we want to remove
        // \u200b = Zero width space (8203 decimal/html).  Trim() does not remove this character.
        Regex badCharactersRegex = new Regex("\u200b");

        List<string> formattingTags = new List<string>() { "color", "b", "i", "u" };
        Dictionary<string, Regex> rxStart = new Dictionary<string, Regex>();
        Dictionary<string, Regex> rxEnd = new Dictionary<string, Regex>();

        Dictionary<string, string> cleanVoteLookup = new Dictionary<string, string>();


        /// <summary>
        /// Construct the votes Results from the provide list of HTML pages.
        /// </summary>
        /// <param name="pages"></param>
        public void TallyVotes(List<HtmlDocument> pages, int startPost, int endPost)
        {
            if (pages == null)
                throw new ArgumentNullException(nameof(pages));

            if (pages.Count == 0)
                return;

            Reset();

            // Set the thread author for reference.
            SetThreadAuthor(pages.FirstOrDefault()?.DocumentNode);

            foreach (var page in pages)
            {
                if (page != null)
                    ProcessPage(page.DocumentNode, startPost, endPost);
            }
        }

        /// <summary>
        /// Reset all tracking variables.
        /// </summary>
        private void Reset()
        {
            VotesWithSupporters.Clear();
            VoterMessageId.Clear();
            cleanVoteLookup.Clear();
            threadAuthor = string.Empty;
        }

        /// <summary>
        /// Function to set the thread author for the current processing run.
        /// </summary>
        /// <param name="root">Root node of a page of the thread.</param>
        private void SetThreadAuthor(HtmlNode root)
        {
            var pageDesc = root?.Descendants("p").FirstOrDefault(n => n.Id == "pageDescription");
            var authorAnchor = pageDesc?.Elements("a").FirstOrDefault(n => n.GetAttributeValue("class", "") == "username");

            if (authorAnchor != null)
                threadAuthor = HtmlEntity.DeEntitize(authorAnchor.InnerText);
        }

        /// <summary>
        /// Given a valid page, process the user posts on that page.
        /// </summary>
        /// <param name="root">The root node of the HTML page.</param>
        /// <param name="startPost">The first post number to process.</param>
        /// <param name="endPost">The last post number to process.</param>
        private void ProcessPage(HtmlNode root, int startPost, int endPost)
        {
            // Find the ordered list containing all the messages on this page.
            var postList = GetPostList(root);

            // Process each user post in the list.
            foreach (var post in GetPostsFromList(postList))
            {
                ProcessPost(post, startPost, endPost);
            }
        }

        /// <summary>
        /// Gets the HTML node containing the list of posts on the page.
        /// </summary>
        /// <param name="root">The root element of the page.</param>
        /// <returns>Returns the node containing all posts on the page.</returns>
        private HtmlNode GetPostList(HtmlNode root)
        {
            return root.Descendants("ol").First(n => n.Id == "messageList");
        }

        /// <summary>
        /// Gets an IEnumerable that provides all the post nodes on the page.
        /// </summary>
        /// <param name="postList">A list element of the page that contains the posts.</param>
        /// <returns>Returns the list of posts on the page.</returns>
        private IEnumerable<HtmlNode> GetPostsFromList(HtmlNode postList)
        {
            return postList.ChildNodes.Where(n => n.Name == "li");
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

            int postNumber = GetPostNumber(post);

            // Ignore posts outside the selected post range.
            if (postNumber < startPost || (endPost > 0 && postNumber > endPost))
                return;

            postAuthor = GetPostAuthor(post);

            // Ignore posts by the thread author.
            if (postAuthor == threadAuthor)
                return;

            postID = GetPostId(post);

            // Get the text from the post that can be searched for a vote.
            postText = ExtractPostText(post);

            // Attempt to get vote information from the post.
            ProcessPostContents(postText, postAuthor, postID);
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
        /// Get the author of the provided post.
        /// </summary>
        /// <param name="post">A user post.</param>
        /// <returns>Returns the author name.</returns>
        private string GetPostAuthor(HtmlNode post)
        {
            // The author is provided in a data attribute of the root element.
            string author = post.GetAttributeValue("data-author", "");
            author = HtmlEntity.DeEntitize(author);
            return author;
        }

        /// <summary>
        /// Get the universal ID number of the post.
        /// </summary>
        /// <param name="post">The user's post.</param>
        /// <returns>Returns the ID number of the post as a string.</returns>
        private string GetPostId(HtmlNode post)
        {
            // The universal post ID is the ID of the root element of the post.
            string id = post.Id;

            // The format is "post-12345678", but we only want the number (as text).
            if (id.StartsWith("post-"))
            {
                id = id.Substring("post-".Length);
            }

            return id;
        }

        /// <summary>
        /// Extract the text of the provided post, ignoring quotes, spoilers, or other
        /// invalid regions.  Clean up HTML entities as well.
        /// </summary>
        /// <param name="post">The li HTML node containing the user post.</param>
        /// <returns>A string containing all valid text lines.</returns>
        private string ExtractPostText(HtmlNode post)
        {
            // Extract the actual contents of the post.
            var postArticle = post.Descendants("article").First();
            var articleBlock = postArticle.Element("blockquote");

            string postText = ExtractNodeText(articleBlock);

            // Clean up the extracted text
            postText = postText.TrimStart();
            postText = HtmlEntity.DeEntitize(postText);
            postText = badCharactersRegex.Replace(postText, "");

            return postText;
        }

        /// <summary>
        /// Extract the text of the provided HTML node.  Recurses into nested
        /// divs.
        /// </summary>
        /// <param name="node">The node to pull text content from.</param>
        /// <returns>A string containing the text of the post, with formatting
        /// elements converted to BBCode tags.</returns>
        private string ExtractNodeText(HtmlNode node)
        {
            StringBuilder sb = new StringBuilder();

            // Search the post for valid element types, and put them all together
            // into a single string.
            foreach (var childNode in node.ChildNodes)
            {
                // Once we reach the end marker of the post, no more processing is needed.
                if (childNode.GetAttributeValue("class", "").Contains("messageTextEndMarker"))
                    return sb.ToString();

                // If we encounter a quote, skip past it
                if (childNode.GetAttributeValue("class", "").Contains("bbCodeQuote"))
                    continue;

                // A <br> element adds a newline
                if (childNode.Name == "br")
                {
                    sb.AppendLine("");
                    continue;
                }

                // If the node doesn't contain any text, move to the next.
                if (childNode.InnerText.Trim() == string.Empty)
                    continue;

                // Add BBCode markup in place of HTML format elements,
                // while collecting the text in the post.
                switch (childNode.Name)
                {
                    case "#text":
                        sb.Append(childNode.InnerText);
                        break;
                    case "i":
                        sb.Append("[i]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/i]");
                        break;
                    case "b":
                        sb.Append("[b]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/b]");
                        break;
                    case "u":
                        sb.Append("[u]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/u]");
                        break;
                    case "span":
                        string spanStyle = childNode.GetAttributeValue("style", "");
                        if (spanStyle.StartsWith("color:", StringComparison.OrdinalIgnoreCase))
                        {
                            string spanColor = spanStyle.Substring("color:".Length).Trim();
                            sb.Append("[color=");
                            sb.Append(spanColor);
                            sb.Append("]");
                            sb.Append(childNode.InnerText);
                            sb.Append("[/color]");
                        }
                        break;
                    case "a":
                        sb.Append("[url=\"");
                        sb.Append(childNode.GetAttributeValue("href", ""));
                        sb.Append("\"]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/url]");
                        break;
                    case "div":
                        // Recurse into divs
                        sb.Append(ExtractNodeText(childNode));
                        break;
                    default:
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Examine the text of the post, determine if it contains any votes, put the vote
        /// together, and update our vote and voter records.
        /// </summary>
        /// <param name="postText">The text of the post.</param>
        /// <param name="postAuthor">The author of the post.</param>
        /// <param name="postID">The ID of the post.</param>
        private void ProcessPostContents(string postText, string postAuthor, string postID)
        {
            if (IsTallyPost(postText))
                return;

            // Pull out actual vote lines from the post.
            MatchCollection matches = voteRegex.Matches(postText);
            if (matches.Count > 0)
            {
                // Remove the post author from any other existing votes.
                RemoveSupport(postAuthor);

                // Get the list of all vote partitions, built according to current preferences.
                // One of: By line, By block, or By post (ie: entire vote)
                List<string> votePartitions = GetVotePartitions(matches);

                foreach (var votePartition in votePartitions)
                {
                    // Find any existing vote that matches the current vote partition.
                    string voteKey = GetVoteKey(votePartition);

                    // Update the supporters list, and save this voter's post ID for linking.
                    VotesWithSupporters[voteKey].Add(postAuthor);
                    VoterMessageId[postAuthor] = postID;
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
        /// Given a collection of vote line matches, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="matches">Matches for a valid vote line.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> GetVotePartitions(MatchCollection matches)
        {
            var strings = from Match m in matches
                          select m.Value;

            return GetVotePartitions(strings);
        }

        /// <summary>
        /// Given a list of vote lines, combine them into a single string entity,
        /// or multiple blocks of strings if we're using vote partitions.
        /// </summary>
        /// <param name="lines">List of valid vote lines.</param>
        /// <returns>List of the combined partitions.</returns>
        private List<string> GetVotePartitions(IEnumerable<string> lines)
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

                string trimmedLine = line.Trim();

                // For lines that don't refer to other voters, compile them into
                // unit blocks if we're using vote partitions, or just add to the
                // end of the total string if not.
                if (UseVotePartitions)
                {
                    if (PartitionByLine)
                    {
                        // If partitioning by line, every line gets added to the partitions list.
                        partitions.Add(trimmedLine+"\r\n");
                    }
                    else
                    {
                        // If partitioning by block, work on collecting chunks
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
        private string GetVoteKey(string vote)
        {
            // If the vote already matches an existing key, we don't need to search again.
            if (VotesWithSupporters.ContainsKey(vote))
            {
                return vote;
            }

            var cleanVote = CleanVote(vote);

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
        private string CleanVote(string vote)
        {
            if (UseVotePartitions && PartitionByLine)
                return cleanLinePartRegex.Replace(vote, "").ToLower();
            else
                return cleanRegex.Replace(vote, "").ToLower();
        }
    }
}
