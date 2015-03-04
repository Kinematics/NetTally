using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Http;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace NetTally
{
    class Tally : INotifyPropertyChanged
    {
        const string SVThreadURL = "http://forums.sufficientvelocity.com/threads/";
        const string SVPostURL = "http://forums.sufficientvelocity.com/posts/";

        Dictionary<string, CachedPage> pageCache = new Dictionary<string, CachedPage>();
        Dictionary<string, string> voterMessageId = new Dictionary<string, string>();
        Dictionary<string, HashSet<string>> voteSupporters = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        Regex voteRegex = new Regex(@"^\s*-*\[[xX]\].*", RegexOptions.Multiline);
        Regex voterRegex = new Regex(@"^\s*-*\[[xX]\]\s*([pP][lL][aA][nN]\s*)?(?<name>.*?)[.]?\s*$");


        #region Results property that can be used to notify watchers of data changes.

        private string results = string.Empty;

        public string TallyResults
        {
            get { return results; }
            set
            {
                results = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("TallyResults");
            }
        }

        // Declare the event 
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion


        #region Internal functions
        internal void ClearPageCache()
        {
            pageCache.Clear();
        }
        #endregion


        public async Task Run(string questTitle, int startPost, int endPost)
        {
            if (startPost < 1)
                throw new ArgumentOutOfRangeException(nameof(startPost), startPost, "Vote tally must start at at least post 1.");
            if (endPost < 0)
                throw new ArgumentOutOfRangeException(nameof(endPost), endPost, "Vote tally ending must be at at least post 0.");

            int startPage = GetPageNumberFromPost(startPost);
            int endPage = GetPageNumberFromPost(endPost);

            string baseUrl = GetSufficientVelocityUrl(questTitle);

            TallyResults = string.Empty;

            // Get the first scanned page and extract the last page number of the thread from that.
            var page = GetPage(baseUrl, startPage);
            var firstPage = await page;

            int lastPageNum = GetLastPageNumber(firstPage);

            // Limit the end page based on the last page number of the thread.
            if (endPost == 0 || lastPageNum < endPage)
            {
                endPage = lastPageNum;
            }

            // Construct a list of tasks for loading up all the pages we want to read from the thread.
            List<Task<HtmlDocument>> taskList = new List<Task<HtmlDocument>>();
            List<HtmlDocument> pages = new List<HtmlDocument>();
            pages.Add(firstPage);

            for (int pageNum = startPage + 1; pageNum <= endPage; pageNum++)
            {
                taskList.Add(GetPage(baseUrl, pageNum));
            }

            try
            {
                // Read the pages (async wait)
                foreach (var task in taskList)
                {
                    await task;
                }

                // Compile the read pages into a list.
                foreach (var task in taskList)
                {
                    pages.Add(task.Result);
                }

                // Tally the votes and compose the final result string.
                TallyVotes(pages);

                ConstructResults();
            }
            catch (AggregateException e)
            {
                Debug.WriteLine(e.Message);
            }
        }


        /// <summary>
        /// Calculate the page number the corresponds to the post number given.
        /// </summary>
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        private static int GetPageNumberFromPost(int post)
        {
            return ((post - 1) / 25) + 1;
        }


        /// <summary>
        /// Construct the full SV web site base URL based on the quest title.
        /// </summary>
        /// <param name="questTitle">The title of the quest thread.</param>
        /// <returns>The full website URL</returns>
        private string GetSufficientVelocityUrl(string questTitle)
        {
            StringBuilder url = new StringBuilder(SVThreadURL);
            url.Append(questTitle);
            url.Append("/page-");
            return url.ToString();
        }


        /// <summary>
        /// Load the specified thread page and return the document as an HtmlDocument.
        /// </summary>
        /// <param name="baseUrl">The thread URL.</param>
        /// <param name="pageNum">The page number in the thread to load.</param>
        /// <returns>An HtmlDocument for the specified page.</returns>
        private async Task<HtmlDocument> GetPage(string baseUrl, int pageNum)
        {
            string url = baseUrl + pageNum.ToString();

            TallyResults = TallyResults + url + "\n";

            // Attempt to use the cached version of the page if it was loaded less than 30 minutes ago.
            if (pageCache.ContainsKey(url))
            {
                var cache = pageCache[url];
                var age = (DateTime.Now - cache.Timestamp).TotalMinutes;
                if (age < 30)
                {
                    return cache.Doc;
                }
            }

            HtmlDocument htmldoc = new HtmlDocument();

            using (HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 })
            {
                // Call asynchronous network methods in a try/catch block to handle exceptions 
                try
                {
                    string responseBody = await client.GetStringAsync(url);

                    htmldoc.LoadHtml(responseBody);

                    pageCache[url] = new CachedPage(htmldoc);
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine("Message :{0} ", e.Message);
                }
            }

            TallyResults = TallyResults + "Page " + pageNum.ToString() + " loaded!\n";

            return htmldoc;
        }


        /// <summary>
        /// Get the last page number of the thread, based on info from the provided page.
        /// </summary>
        /// <param name="doc">The HtmlDocument of the page we're examining.</param>
        /// <returns>The last page number of the thread.</returns>
        private int GetLastPageNumber(HtmlDocument doc)
        {
            // Root of the tree
            var root = doc.DocumentNode;

            var content = root.Descendants("div").First(n => n.Id == "content");
            var pageNav = content.Descendants("div").First(n => n.GetAttributeValue("class", "") == "PageNav");
            string lastPageStr = pageNav.GetAttributeValue("data-last", "1");

            int lastPage = 0;
            int.TryParse(lastPageStr, out lastPage);

            return lastPage;
        }


        /// <summary>
        /// Construct the votes Results from the provide list of HTML pages.
        /// </summary>
        /// <param name="pages"></param>
        private void TallyVotes(List<HtmlDocument> pages)
        {
            foreach (var page in pages)
            {
                // Root of the tree
                var root = page.DocumentNode;

                // Thread author
                var pageDesc = root.Descendants("p").First(n => n.Id == "pageDescription");
                var authorAnchor = pageDesc.Elements("a").First(n => n.GetAttributeValue("class", "") == "username");
                string threadAuthor = authorAnchor.InnerText;

                string postAuthor;
                string postID;
                string postText;

                MatchCollection matches;

                // Find the ordered list containing all the messages on this page.
                var messageList = root.Descendants("ol").First(n => n.Id == "messageList");

                // Process each <li> child node as a message.
                foreach (var msg in messageList.ChildNodes.Where(n => n.Name == "li"))
                {
                    // The post author is contained in the <li> element.
                    postAuthor = msg.GetAttributeValue("data-author", "");

                    // Ignore posts by the thread author.
                    if (postAuthor != threadAuthor)
                    {
                        // The post ID is also contained in the <li> element.
                        postID = msg.Id;
                        if (postID.StartsWith("post-"))
                        {
                            postID = postID.Substring("post-".Length);
                        }

                        postText = ExtractVote(msg);

                        // Pull out actual vote lines from the post.
                        matches = voteRegex.Matches(postText);
                        if (matches.Count > 0)
                        {
                            string vote = CombineMatchesIntoVote(matches);

                            string voteKey = FindMatchingVote(vote, matches);

                            if (!voteSupporters.ContainsKey(voteKey))
                            {
                                voteSupporters[voteKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            }

                            RemoveSupport(postAuthor);
                            voteSupporters[voteKey].Add(postAuthor);
                            voterMessageId[postAuthor] = postID;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Extract the text of any vote contained within the provided post.
        /// </summary>
        /// <param name="post">The li HTML node containing the user post.</param>
        /// <returns>A string containing all proper voting lines.</returns>
        private string ExtractVote(HtmlNode post)
        {
            StringBuilder sb = new StringBuilder();

            // Extract the actual contents of the post.
            var postArticle = post.Descendants("article").First();
            var articleBlock = postArticle.Element("blockquote");

            // Handle each type of valid sub-node and convert it back into BBCode.
            foreach (var node in articleBlock.ChildNodes)
            {
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
                    case "br":
                        sb.AppendLine("");
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
        /// Given a collection of vote line matches, combine them into a single string entity.
        /// </summary>
        /// <param name="matches">Matches for a valid vote line.</param>
        /// <returns>String containing all the matches on individual lines.</returns>
        private string CombineMatchesIntoVote(MatchCollection matches)
        {
            StringBuilder sb = new StringBuilder();

            // The regex matches do not contain EOL characters.
            foreach (Match match in matches)
            {
                sb.AppendLine(match.Value);
            }

            return sb.ToString();
        }


        /// <summary>
        /// Attempt to find any existing vote that matches what the current vote describes.
        /// If the current vote includes a voter name, pull the full version of their currently
        /// supported vote and insert it in place of that line in this one.
        /// </summary>
        /// <param name="vote"></param>
        /// <param name="originalMatches"></param>
        /// <returns></returns>
        private string FindMatchingVote(string vote, MatchCollection originalMatches)
        {
            if (voteSupporters.ContainsKey(vote))
            {
                return vote;
            }

            List<string> expandedVote = new List<string>();
            StringBuilder sb = new StringBuilder();

            foreach (Match match in originalMatches)
            {
                Match m = voterRegex.Match(match.Value);
                if (m.Success)
                {
                    string refVote = FindVoteForVoter(m.Groups["name"].Value);

                    if (refVote != string.Empty)
                    {
                        sb.Append(refVote);
                    }
                    else
                    {
                        sb.AppendLine(match.Value);
                    }
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Find which vote a given voter is currently voting for.
        /// </summary>
        /// <param name="voter">The voter to check on.</param>
        /// <returns>Returns the vote key for the vote the voter is currently supporting, if any.</returns>
        private string FindVoteForVoter(string voter)
        {
            foreach (var vote in voteSupporters)
            {
                if (vote.Value.Contains(voter))
                {
                    return vote.Key;
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// Remove the author's support for any existing votes.
        /// </summary>
        /// <param name="postAuthor">The author name to check for.</param>
        private void RemoveSupport(string postAuthor)
        {
            List<string> emptyVotes = new List<string>();

            foreach (var vote in voteSupporters)
            {
                if (vote.Value.Remove(postAuthor))
                {
                    if (vote.Value.Count == 0)
                    {
                        emptyVotes.Add(vote.Key);
                    }
                }
            }

            foreach (var vote in emptyVotes)
            {
                voteSupporters.Remove(vote);
            }
        }


        /// <summary>
        /// Compose the stored results into a string to put in the Results property.
        /// </summary>
        private void ConstructResults()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[b]Vote Tally[/b]");
            sb.AppendLine("");

            foreach (var vote in voteSupporters)
            {
                sb.Append(vote.Key);

                sb.Append("[b]No. of Votes: ");
                sb.Append(vote.Value.Count);
                sb.AppendLine("[/b]");


                foreach (var supporter in vote.Value)
                {
                    sb.Append("[url=\"");
                    sb.Append(SVPostURL);
                    sb.Append(voterMessageId[supporter]);
                    sb.Append("/\"]");
                    sb.Append(supporter);
                    sb.AppendLine("[/url]");
                }

                sb.AppendLine("");
            }

            TallyResults = sb.ToString();
        }


    }
}
