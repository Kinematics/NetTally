using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public class WebPageProvider : IPageProvider
    {
        readonly Dictionary<string, CachedPage> pageCache = new Dictionary<string, CachedPage>();
        readonly Dictionary<string, int> lastPageLoadedFor = new Dictionary<string, int>();

        #region Event handlers
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

        public event EventHandler<MessageEventArgs> StatusChanged;

        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            pageCache.Clear();
            lastPageLoadedFor.Clear();
        }

        /// <summary>
        /// Load the pages for the given quest asynchronously.
        /// </summary>
        /// <param name="quest">Quest object containing query parameters.</param>
        /// <returns>Returns a list of web pages as HTML Documents.</returns>
        public async Task<List<HtmlDocument>> LoadPages(IQuest quest, CancellationToken token)
        {
            try
            {
                IForumAdapter forumAdapter = quest.GetForumAdapter();

                if (!forumAdapter.IsValidThreadName(quest.Name))
                    throw new ArgumentException("The quest name is not valid.");

                int startPost = await forumAdapter.GetStartingPostNumber(this, quest, token).ConfigureAwait(false);

                int startPage = forumAdapter.GetPageNumberFromPostNumber(startPost);
                int endPage = forumAdapter.GetPageNumberFromPostNumber(quest.EndPost);

                // Get the first page and extract the last page number of the thread from that (bypass the cache).
                var firstPage = await GetPage(forumAdapter.GetPageUrl(quest.Name, startPage), startPage.ToString(), true, token).ConfigureAwait(false);

                int lastPageNum = forumAdapter.GetLastPageNumberOfThread(firstPage);

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
                int lastPageLoaded = 0;

                if (pagesToScan > 0)
                {
                    // Initiate tasks for all pages other than the first page (which we already loaded)
                    var tasks = from pNum in Enumerable.Range(startPage + 1, pagesToScan)
                                select GetPage(forumAdapter.GetPageUrl(quest.Name, pNum), pNum.ToString(),
                                    (lastPageLoadedFor.TryGetValue(quest.Name, out lastPageLoaded) && pNum >= lastPageLoaded), token);

                    // Wait for all the tasks to be completed.
                    HtmlDocument[] pageArray = await Task.WhenAll(tasks).ConfigureAwait(false);

                    // Add the results to our list of pages.
                    pages.AddRange(pageArray);
                }

                lastPageLoadedFor[quest.Name] = endPage;


                return pages;
            }
            catch (OperationCanceledException)
            {
                OnStatusChanged("Tally cancelled!\n");
                throw;
            }
        }

        /// <summary>
        /// Load the specified thread page and return the document as an HtmlDocument.
        /// </summary>
        /// <param name="baseUrl">The thread URL.</param>
        /// <param name="pageNum">The page number in the thread to load.</param>
        /// <param name="bypassCache">Whether to skip checking the cache.</param>
        /// <param name="token">Cancellation token for the function.</param>
        /// <returns>An HtmlDocument for the specified page.</returns>
        public async Task<HtmlDocument> GetPage(string url, string shortDescrip, bool bypassCache, CancellationToken token)
        {
            // Attempt to use the cached version of the page if it was loaded less than 30 minutes ago.
            if (!bypassCache && pageCache.ContainsKey(url))
            {
                var cache = pageCache[url];
                var cacheAge = DateTime.Now - cache.Timestamp;

                if (cacheAge.TotalMinutes < 30)
                {
                    if (cacheAge.TotalSeconds > 4)
                        OnStatusChanged("Page " + shortDescrip + " loaded from memory!\n");
                    return cache.Doc;
                }
            }

            OnStatusChanged(url + "\n");

            HtmlDocument htmldoc = new HtmlDocument();

            string result = null;
            int tries = 0;
            using (HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 })
            {
                client.Timeout = TimeSpan.FromSeconds(2);

                try
                {
                    while (result == null && tries < 3 && token.IsCancellationRequested == false)
                    {
                        using (var response = await client.GetAsync(url, token).ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            }
                            else
                            {
                                OnStatusChanged("Retrying page " + shortDescrip + "\n");
                                tries++;
                            }
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    OnStatusChanged("Page " + shortDescrip + ": " + e.Message);
                    throw;
                }
            }

            if (token.IsCancellationRequested)
            {
                return null;
            }

            if (result == null)
            {
                OnStatusChanged("Failed to load page " + shortDescrip + "\n");
                return null;
            }

            htmldoc.LoadHtml(result);

            pageCache[url] = new CachedPage(htmldoc);

            OnStatusChanged("Page " + shortDescrip + " loaded!\n");

            return htmldoc;
        }

        #endregion

    }
}
