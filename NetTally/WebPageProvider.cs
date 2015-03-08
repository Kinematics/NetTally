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
        const string SVThreadURL = "http://forums.sufficientvelocity.com/threads/";

        Dictionary<string, CachedPage> pageCache = new Dictionary<string, CachedPage>();
        Dictionary<string, int> lastPageLoaded = new Dictionary<string, int>();

        // Make sure that the quest name is valid to be inserted into a URL (no spaces),
        //and has the proper form (thread number at end).
        Regex validateQuestNameForUrl = new Regex(@"^\S+\.\d+$");

        public event EventHandler<MessageEventArgs> StatusChanged;

        /// <summary>
        /// Function to raise events when page load status has been updated.
        /// </summary>
        /// <param name="message">The message to send to any listeners.</param>
        protected void OnStatusChanged(string message)
        {
            StatusChanged?.Invoke(this, new MessageEventArgs(message));
        }


        #region Public interface functions
        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            pageCache.Clear();
            lastPageLoaded.Clear();
        }

        /// <summary>
        /// Load the pages for the given quest.
        /// </summary>
        /// <param name="questTitle"></param>
        /// <param name="startPost"></param>
        /// <param name="endPost"></param>
        /// <returns>Returns a list of web pages as HTML Documents.</returns>
        public List<HtmlDocument> LoadPages(string questTitle, int startPost, int endPost)
        {
            // We just wrap the async version of the code, then wait on the results syncrhonously.
            var asyncPages = LoadPagesAsync(questTitle, startPost, endPost);
            asyncPages.Wait();

            return asyncPages.Result;
        }

        /// <summary>
        /// Load the pages for the given quest asynchronously.
        /// </summary>
        /// <param name="questTitle">The name of the quest thread to load.</param>
        /// <param name="startPost">The first post we're interested in tallying.</param>
        /// <param name="endPost">The last post we're interested in tallying.</param>
        /// <returns>Returns a list of web pages as HTML Documents.</returns>
        public async Task<List<HtmlDocument>> LoadPagesAsync(string questTitle, int startPost, int endPost)
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
        /// Construct the full SV web site base URL based on the quest title.
        /// </summary>
        /// <param name="questTitle">The title of the quest thread.</param>
        /// <returns>The full website URL</returns>
        private string GetThreadBaseUrl(string questTitle)
        {
            // URL should not have any whitespace in it, and should end with a thread number (eg: .11111).
            if (!validateQuestNameForUrl.Match(questTitle).Success)
                throw new ArgumentException("The quest name is not valid.\nCheck for spaces, and make sure it ends with the thread number.");

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
                    OnStatusChanged("Page " + pageNum.ToString() + " loaded from memory!\n");
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

                    OnStatusChanged("Page " + pageNum.ToString() + " loaded!\n");
                }
                catch (HttpRequestException e)
                {
                    OnStatusChanged("Page " + pageNum.ToString() + ": " + e.Message);
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
