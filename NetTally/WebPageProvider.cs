using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public class WebPageProvider : IPageProvider
    {
        // Base URL for Sufficient Velocity web forums.
        const string SVUrl = "http://forums.sufficientvelocity.com/";
        const string SVThreadUrl = "http://forums.sufficientvelocity.com/threads/";

        Dictionary<string, CachedPage> pageCache = new Dictionary<string, CachedPage>();
        Dictionary<string, int> lastPageLoaded = new Dictionary<string, int>();

        // Make sure that the quest name is valid to be inserted into a URL (no spaces),
        //and has the proper form (thread number at end).
        Regex validateQuestNameForUrl = new Regex(@"^\S+\.\d+$");


        #region Event handlers
        public event EventHandler<MessageEventArgs> StatusChanged;

        /// <summary>
        /// Function to raise events when page load status has been updated.
        /// </summary>
        /// <param name="message">The message to send to any listeners.</param>
        protected void OnStatusChanged(string message)
        {
            StatusChanged?.Invoke(this, new MessageEventArgs(message));
        }
        #endregion

        #region Public interface functions
        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        public bool CheckForLastThreadmark { get; set; }

        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            pageCache.Clear();
            lastPageLoaded.Clear();
        }

        /// <summary>
        /// Load the pages for the given quest asynchronously.
        /// </summary>
        /// <param name="quest">Quest object containing query parameters.</param>
        /// <returns>Returns a list of web pages as HTML Documents.</returns>
        public async Task<List<HtmlDocument>> LoadPages(IQuest quest)
        {
            // URL should not have any whitespace in it, and should end with a thread number (eg: .11111).
            if (!validateQuestNameForUrl.Match(quest.Name).Success)
                throw new ArgumentException("The quest name is not valid.\nCheck for spaces, and make sure it ends with the thread number.");

            int startPost = await GetStartPost(quest.Name, quest.StartPost).ConfigureAwait(false);

            int startPage = GetPageNumberFromPost(startPost);
            int endPage = GetPageNumberFromPost(quest.EndPost);

            // Get the first page and extract the last page number of the thread from that (bypass the cache).
            var firstPage = await GetPage(GetPageUrl(quest.Name, startPage), startPage.ToString(), true).ConfigureAwait(false);

            int lastPageNum = GetLastPageNumber(firstPage);

            // Limit the end page based on the last page number of the thread.
            if (quest.ReadToEndOfThread || lastPageNum < endPage)
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
                            select GetPage(GetPageUrl(quest.Name, pNum), pNum.ToString(),
                                (lastPageLoaded.ContainsKey(quest.Name) && pNum >= lastPageLoaded[quest.Name]));

                // Wait for all the tasks to be completed.
                HtmlDocument[] pageArray = await Task.WhenAll(tasks).ConfigureAwait(false);

                // Add the results to our list of pages.
                pages.AddRange(pageArray);
            }

            lastPageLoaded[quest.Name] = endPage;

            return pages;
        }

        #endregion

        #region Calculate start post based on last threadmark
        private async Task<int> GetStartPost(string questTitle, int startPost)
        {
            // Use the provided start post if we aren't trying to find the threadmarks.
            if (!CheckForLastThreadmark)
                return startPost;

            var threadmarkPage = await GetPage(GetThreadmarksPageUrl(questTitle), "Threadmarks", true);

            if (IsValidThreadmarksPage(threadmarkPage))
            {
                string postLink = GetLastThreadmarkLink(threadmarkPage);
                string postIndex = GetPostIndexFromPostLink(postLink);

                var lastThreadmarkPage = await GetPage(GetPostUrl(postLink), postLink, false);
                var threadmarkPost = GetPostFromPage(lastThreadmarkPage, postIndex);
                int threadmarkPostNumber = GetPostNumberFromPost(threadmarkPost);

                if (threadmarkPostNumber > 0)
                    return threadmarkPostNumber + 1;
                else
                    return startPost;
            }
            else
            {
                return startPost;
            }
        }

        private bool IsValidThreadmarksPage(HtmlDocument threadmarkPage)
        {
            var root = threadmarkPage.DocumentNode;

            var content = root?.Element("html")?.Element("body")?.Elements("div")?.FirstOrDefault(a => a.Id == "headerMover")?.
                Elements("div")?.FirstOrDefault(a => a.Id == "content");

            string contentClass = content?.GetAttributeValue("class", "");

            if (contentClass == "error")
            {
                OnStatusChanged("No threadmarks available for this quest!\n");
                return false;
            }
            else if (contentClass == "threadmarks")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private string GetLastThreadmarkLink(HtmlDocument threadmarkPage)
        {
            var root = threadmarkPage.DocumentNode;

            var content = root?.Element("html")?.Element("body")?.Elements("div")?.FirstOrDefault(a => a.Id == "headerMover")?.
                Elements("div")?.FirstOrDefault(a => a.Id == "content");

            var threadmarkList = content?.Descendants("ol")?.FirstOrDefault(a => a.GetAttributeValue("class", "") == "overlayScroll");

            var lastThreadmark = threadmarkList?.Elements("li")?.LastOrDefault();

            return lastThreadmark?.Element("a")?.GetAttributeValue("href", "");
        }

        private string GetPostIndexFromPostLink(string postLink)
        {
            Regex postLinkRegex = new Regex(@"posts/(?<postId>\d+)/");
            var m = postLinkRegex.Match(postLink);
            if (m.Success)
                return m.Groups["postId"].Value;

            throw new ArgumentException("Unable to extract post ID from link:\n" + postLink, nameof(postLink));
        }

        private HtmlNode GetPostFromPage(HtmlDocument root, string postIndex)
        {
            var postList = root.DocumentNode.Descendants("ol").First(n => n.Id == "messageList");
            string checkIndex = "post-" + postIndex;
            return postList.ChildNodes.Where(n => n.Name == "li").FirstOrDefault(a => a.Id == checkIndex);
        }

        private int GetPostNumberFromPost(HtmlNode post)
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
        #endregion

        #region String concatenation functions for constructing URLs.
        private string GetThreadUrl(string questTitle) => SVThreadUrl + questTitle;

        private string GetThreadPageBaseUrl(string questTitle) => GetThreadUrl(questTitle) + "/page-";

        private string GetThreadmarksPageUrl(string questTitle) => GetThreadUrl(questTitle) + "/threadmarks";

        private string GetPageUrl(string questTitle, int page) => GetThreadPageBaseUrl(questTitle) + page.ToString();

        private string GetPostUrl(string post) => SVUrl + post;
        #endregion


        /// <summary>
        /// Calculate the page number that corresponds to the post number given.
        /// </summary>
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        private static int GetPageNumberFromPost(int post)
        {
            return ((post - 1) / 25) + 1;
        }

        /// <summary>
        /// Load the specified thread page and return the document as an HtmlDocument.
        /// </summary>
        /// <param name="baseUrl">The thread URL.</param>
        /// <param name="pageNum">The page number in the thread to load.</param>
        /// <param name="bypassCache">Whether to skip checking the cache.</param>
        /// <returns>An HtmlDocument for the specified page.</returns>
        private async Task<HtmlDocument> GetPage(string url, string shortDescrip, bool bypassCache)
        {
            // Attempt to use the cached version of the page if it was loaded less than 30 minutes ago.
            if (!bypassCache && pageCache.ContainsKey(url))
            {
                var cache = pageCache[url];
                var age = (DateTime.Now - cache.Timestamp).TotalMinutes;
                if (age < 30)
                {
                    OnStatusChanged("Page " + shortDescrip + " loaded from memory!\n");
                    return cache.Doc;
                }
            }

            OnStatusChanged(url + "\n");

            HtmlDocument htmldoc = new HtmlDocument();

            using (HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 })
            {
                // Call asynchronous network methods in a try/catch block to handle exceptions 
                try
                {
                    string responseBody = await client.GetStringAsync(url).ConfigureAwait(false);

                    htmldoc.LoadHtml(responseBody);

                    pageCache[url] = new CachedPage(htmldoc);

                    OnStatusChanged("Page " + shortDescrip + " loaded!\n");
                }
                catch (HttpRequestException e)
                {
                    OnStatusChanged("Page " + shortDescrip + ": " + e.Message);
                    throw;
                }
            }

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
    }
}
