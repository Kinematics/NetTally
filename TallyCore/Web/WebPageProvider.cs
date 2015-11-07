using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public class WebPageProvider : IPageProvider
    {
        private enum StatusType
        {
            None,
            Requested,
            Retry,
            Error,
            Failed,
            Cancelled,
            Cached,
            Loaded
        }

        WebCache Cache { get; } = new WebCache();
        readonly SemaphoreSlim ss = new SemaphoreSlim(5);
        string UserAgent { get; }

        public WebPageProvider()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));
            UserAgent = $"{product.Product} ({version.InformationalVersion})";
        }

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
            Cache.Clear();
        }

        /// <summary>
        /// Load the pages for the given quest asynchronously.
        /// </summary>
        /// <param name="quest">Quest object containing query parameters.</param>
        /// <returns>Returns a list of web pages as HTML Documents.</returns>
        public async Task<List<HtmlDocument>> LoadQuestPages(IQuest quest, CancellationToken token)
        {
            try
            {
                // Determine the first and last page numbers to be loaded.

                int firstPageNumber = await quest.GetFirstPageNumber(this, token);

                HtmlDocument firstPage = await GetPage(quest.GetPageUrl(firstPageNumber), $"Page {firstPageNumber}", Caching.BypassCache, token)
                    .ConfigureAwait(false);

                if (firstPage == null)
                    throw new InvalidOperationException("Unable to load web page.");

                int lastPageNumber = await quest.GetLastPageNumber(firstPage, token);


                // We will store the loaded pages in a new List.
                List<HtmlDocument> pages = new List<HtmlDocument>();

                // First page is already loaded.
                pages.Add(firstPage);

                // Set parameters for which pages to try to load
                int pagesToScan = lastPageNumber - firstPageNumber;

                int? lastPageNumberLoaded = Cache.GetLastPageLoaded(quest.ThreadName);

                // Initiate the async tasks to load the pages
                if (pagesToScan > 0)
                {
                    // Initiate tasks for all pages other than the first page (which we already loaded)
                    var results = from pageNum in Enumerable.Range(firstPageNumber + 1, pagesToScan)
                                  let cacheMode = (lastPageNumberLoaded.HasValue && pageNum >= lastPageNumberLoaded) ? Caching.BypassCache : Caching.UseCache
                                  let pageUrl = quest.GetPageUrl(pageNum)
                                  select GetPage(pageUrl, $"Page {pageNum}", cacheMode, token);

                    // Wait for all the tasks to be completed.
                    HtmlDocument[] pageArray = await Task.WhenAll(results).ConfigureAwait(false);

                    if (pageArray.Any(p => p == null))
                    {
                        throw new ApplicationException("Not all pages loaded.  Rerun tally.");
                    }

                    // Add the results to our list of pages.
                    pages.AddRange(pageArray);
                }

                Cache.Update(quest.ThreadName, lastPageNumber);

                return pages;
            }
            catch (OperationCanceledException)
            {
                UpdateStatus(StatusType.Cancelled);
                throw;
            }
        }

        /// <summary>
        /// Load the specified thread page and return the document as an HtmlDocument.
        /// </summary>
        /// <param name="baseUrl">The thread URL.</param>
        /// <param name="pageNum">The page number in the thread to load.</param>
        /// <param name="caching">Whether to use or bypass the cache.</param>
        /// <param name="token">Cancellation token for the function.</param>
        /// <returns>An HtmlDocument for the specified page.</returns>
        public async Task<HtmlDocument> GetPage(string url, string shortDescription, Caching caching, CancellationToken token)
        {
            if (caching == Caching.UseCache)
            {
                HtmlDocument page = Cache.Get(url);

                if (page != null)
                {
                    UpdateStatus(StatusType.Cached, shortDescription);
                    return page;
                }
            }

            UpdateStatus(StatusType.Requested, url);

            // Limit to no more than 5 parallel requests
            await ss.WaitAsync(token);

            try
            {
                string result = null;
                int maxtries = 5;
                int tries = 0;
                HttpClient client;
                HttpResponseMessage response;

                HttpClientHandler handler = new HttpClientHandler();
                var cookies = ForumCookies.GetCookies(url);
                if (cookies.Count > 0)
                {
                    CookieContainer cookieJar = new CookieContainer();
                    cookieJar.Add(cookies);
                    handler.CookieContainer = cookieJar;
                }

                using (client = new HttpClient(handler) { MaxResponseContentBufferSize = 1000000 })
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                    try
                    {
                        while (result == null && tries < maxtries && token.IsCancellationRequested == false)
                        {
                            if (tries > 0)
                            {
                                // If we have to retry loading the page, give it a short delay.
                                await Task.Delay(TimeSpan.FromSeconds(4));
                                UpdateStatus(StatusType.Retry, shortDescription);
                            }

                            using (response = await client.GetAsync(url, token).ConfigureAwait(false))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                }
                                else if (response.StatusCode == HttpStatusCode.NotFound)
                                {
                                    tries = maxtries;
                                }
                                else
                                {
                                    tries++;
                                }
                            }
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        UpdateStatus(StatusType.Error, shortDescription, e);
                        throw;
                    }
                    catch (OperationCanceledException e)
                    {
                        Debug.WriteLine(string.Format("Operation was cancelled in task {0}.", Task.CurrentId.HasValue ? Task.CurrentId.Value : -1));
                        Debug.WriteLine($"Cancellation requested: {e.CancellationToken.IsCancellationRequested}  at source: {e.Source}");

                        if (token.IsCancellationRequested)
                            throw;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    return null;
                }

                if (result == null)
                {
                    UpdateStatus(StatusType.Failed, shortDescription);
                    return null;
                }

                HtmlDocument htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(result);

                Cache.Add(url, htmldoc);

                UpdateStatus(StatusType.Loaded, shortDescription);

                return htmldoc;
            }
            finally
            {
                ss.Release();
            }
        }

        /// <summary>
        /// Handle generating messages to send to OnStatusChanged handler.
        /// </summary>
        /// <param name="status">The type of status message being raised.</param>
        /// <param name="details">The details that get inserted into the status message.</param>
        /// <param name="e">The exception, for an error message.</param>
        private void UpdateStatus(StatusType status, string details = null, Exception e = null)
        {
            StringBuilder sb = new StringBuilder();

            if (status == StatusType.Cancelled)
            {
                sb.Append("Tally cancelled!");
            }
            else
            {
                if (details?.Length > 0)
                {
                    switch (status)
                    {
                        case StatusType.Requested:
                            sb.Append(details);
                            break;
                        case StatusType.Retry:
                            sb.Append("Retrying: ");
                            sb.Append(details);
                            break;
                        case StatusType.Error:
                            sb.Append(details);
                            sb.Append(" : ");
                            sb.Append(e?.Message);
                            break;
                        case StatusType.Failed:
                            sb.Append("Failed to load: ");
                            sb.Append(details);
                            break;
                        case StatusType.Cached:
                            sb.Append(details);
                            sb.Append(" loaded from memory!");
                            break;
                        case StatusType.Loaded:
                            sb.Append(details);
                            sb.Append(" loaded!");
                            break;
                        default:
                            break;
                    }
                }
            }

            if (sb.Length > 0)
            {
                sb.Append("\n");
                OnStatusChanged(sb.ToString());
            }
        }
        #endregion
    }
}
