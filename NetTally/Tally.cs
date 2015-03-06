using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    class Tally : INotifyPropertyChanged
    {
        const string SVThreadURL = "http://forums.sufficientvelocity.com/threads/";
        const string SVPostURL = "http://forums.sufficientvelocity.com/posts/";

        Dictionary<string, CachedPage> pageCache = new Dictionary<string, CachedPage>();
        Dictionary<string, string> voterMessageId = new Dictionary<string, string>();
        Dictionary<string, HashSet<string>> votesWithSupporters = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> lastPageLoaded = new Dictionary<string, int>();

        Regex voteRegex = new Regex(@"^\s*-*\[[xX]\].*", RegexOptions.Multiline);
        Regex voterRegex = new Regex(@"^\s*-*\[[xX]\]\s*([pP][lL][aA][nN]\s*)?(?<name>.*?)[.]?\s*$");

        string threadAuthor = string.Empty;

        #region Results property that can be used to notify watchers of data changes.

        private string results = string.Empty;

        public string TallyResults
        {
            get { return results; }
            set
            {
                results = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }

        // Declare the event 
        public event PropertyChangedEventHandler PropertyChanged;


        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Functions for resetting stuff
        /// <summary>
        /// Initialize variables that will be used during the run to a clean state.
        /// </summary>
        private void InitForRun()
        {
            TallyResults = string.Empty;
            threadAuthor = string.Empty;
            voterMessageId.Clear();
            votesWithSupporters.Clear();
            cleanVoteLookup.Clear();
        }

        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        internal void ClearPageCache()
        {
            pageCache.Clear();
            lastPageLoaded.Clear();
        }
        #endregion


        /// <summary>
        /// Run the actual tally.
        /// </summary>
        /// <param name="questTitle">The name of the quest thread to scan.</param>
        /// <param name="startPost">The starting post number.</param>
        /// <param name="endPost">The ending post number.</param>
        /// <returns></returns>
        public async Task Run(string questTitle, int startPost, int endPost)
        {
            if (startPost < 1)
                throw new ArgumentOutOfRangeException(nameof(startPost), startPost, "Vote tally must start at at least post 1.");
            if (endPost < 0)
                throw new ArgumentOutOfRangeException(nameof(endPost), endPost, "Vote tally ending cannot be negative.");

            InitForRun();

            // Load pages from the website
            var pages = await LoadPages(questTitle, startPost, endPost);

            // Tally the votes from the loaded pages.
            TallyVotes(pages, startPost, endPost);

            // Compose the final result string from the compiled votes.
            ConstructResults();
        }

        private async Task<List<HtmlDocument>> LoadPages(string questTitle, int startPost, int endPost)
        {
            int startPage = GetPageNumberFromPost(startPost);
            int endPage = GetPageNumberFromPost(endPost);

            string baseUrl = GetThreadBaseUrl(questTitle);

            // Get the first page and extract the last page number of the thread from that (bypass the cache).
            var firstPage = await GetPage(baseUrl, startPage, true).ConfigureAwait(false);

            int lastPageNum = GetLastPageNumber(firstPage);

            // Limit the end page based on the last page number of the thread.
            if (endPost == 0 || lastPageNum < endPage)
            {
                endPage = lastPageNum;
            }

            // We will store the loaded pages in a new List.
            List<HtmlDocument> pages = new List<HtmlDocument>();

            // First page is already loaded.
            pages.Add(firstPage);

            int pagesToScan = endPage - startPage;
            if (pagesToScan > 0)
            {
                // Initiate tasks for all pages other than the first page (which we already loaded)
                var tasks = from pNum in Enumerable.Range(startPage + 1, pagesToScan)
                            select GetPage(baseUrl, pNum,
                                (lastPageLoaded.ContainsKey(questTitle) && pNum >= lastPageLoaded[questTitle]));

                // Wait for all the tasks to be completed.
                HtmlDocument[] pageArray = await Task.WhenAll(tasks).ConfigureAwait(false);

                // Add the results to our list of pages.
                pages.AddRange(pageArray);
            }

            lastPageLoaded[questTitle] = endPage;

            return pages;
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
        private string GetThreadBaseUrl(string questTitle)
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
        /// <param name="bypassCache">Whether to skip checking the cache.</param>
        /// <returns>An HtmlDocument for the specified page.</returns>
        private async Task<HtmlDocument> GetPage(string baseUrl, int pageNum, bool bypassCache)
        {
            string url = baseUrl + pageNum.ToString();

            // Attempt to use the cached version of the page if it was loaded less than 30 minutes ago.
            if (!bypassCache && pageCache.ContainsKey(url))
            {
                var cache = pageCache[url];
                var age = (DateTime.Now - cache.Timestamp).TotalMinutes;
                if (age < 30)
                {
                    TallyResults = TallyResults + "Page " + pageNum.ToString() + " loaded from memory!\n";
                    return cache.Doc;
                }
            }

            TallyResults = TallyResults + url + "\n";

            HtmlDocument htmldoc = new HtmlDocument();

            using (HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 })
            {
                // Call asynchronous network methods in a try/catch block to handle exceptions 
                try
                {
                    string responseBody = await client.GetStringAsync(url).ConfigureAwait(false);

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
            int lastPage = 1;

            try
            {
                // Root of the tree
                var root = doc.DocumentNode;

                if (root.HasChildNodes == false)
                    return lastPage;

                var content = root.Descendants("div").First(n => n.Id == "content");

                // Page should always have this div
                var pageNavLinkGroup = content.Descendants("div").FirstOrDefault(n => n.GetAttributeValue("class", "").Contains("pageNavLinkGroup"));

                if (pageNavLinkGroup != null)
                {
                    // Threads with only one page won't have a pageNav div, so be careful with this.
                    var pageNav = pageNavLinkGroup.ChildNodes.FirstOrDefault(n => n.GetAttributeValue("class", "").Contains("PageNav"));

                    if (pageNav != null)
                    {
                        string lastPageStr = pageNav.GetAttributeValue("data-last", "1");
                        int.TryParse(lastPageStr, out lastPage);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            return lastPage;
        }


        /// <summary>
        /// Construct the votes Results from the provide list of HTML pages.
        /// </summary>
        /// <param name="pages"></param>
        private void TallyVotes(List<HtmlDocument> pages, int startPost, int endPost)
        {
            foreach (var page in pages)
            {
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

            // Pull out actual vote lines from the post.
            matches = voteRegex.Matches(postText);
            if (matches.Count > 0)
            {
                // Remove the post author from any other existing votes.
                RemoveSupport(postAuthor);

                // Create a single string composite of the matched vote lines.
                string vote = CombineMatchesIntoVote(matches);

                // If the vote names another user, pull that user's vote to fill in this vote.
                string voteKey = FindMatchingVote(vote, matches);

                // Create a new hashtable for vote supporters, if necessary.
                if (!votesWithSupporters.ContainsKey(voteKey))
                {
                    votesWithSupporters[voteKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                // Update the supporters list, and save this voter's post ID for linking.
                votesWithSupporters[voteKey].Add(postAuthor);
                voterMessageId[postAuthor] = postID;
            }
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
            // If the vote already matches an existing key, we don't need to search again.
            if (votesWithSupporters.ContainsKey(vote))
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
            foreach (var vote in votesWithSupporters)
            {
                if (vote.Value.Contains(voter))
                {
                    return vote.Key;
                }
            }

            return string.Empty;
        }


        /// <summary>
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        private void RemoveSupport(string voter)
        {
            List<string> emptyVotes = new List<string>();

            foreach (var vote in votesWithSupporters)
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
                votesWithSupporters.Remove(vote);
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

            foreach (var vote in votesWithSupporters)
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
