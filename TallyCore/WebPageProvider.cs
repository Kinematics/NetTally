﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

        readonly int maxCacheEntries = 50;

        static readonly SemaphoreSlim ss = new SemaphoreSlim(5);

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

                int pagesToScan = lastPageNumber - firstPageNumber;
                int lastPageLoaded = 0;

                if (pagesToScan > 0)
                {
                    // Initiate tasks for all pages other than the first page (which we already loaded)
                    var results = from pageNum in Enumerable.Range(firstPageNumber + 1, pagesToScan)
                                  let cacheMode = (lastPageLoadedFor.TryGetValue(quest.ThreadName, out lastPageLoaded) && pageNum >= lastPageLoaded) ? Caching.BypassCache : Caching.UseCache
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

                lastPageLoadedFor[quest.ThreadName] = lastPageNumber;

                ReviewCache();

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
        /// <param name="caching">Whether to use or bypass the cache.</param>
        /// <param name="token">Cancellation token for the function.</param>
        /// <returns>An HtmlDocument for the specified page.</returns>
        public async Task<HtmlDocument> GetPage(string url, string shortDescription, Caching caching, CancellationToken token)
        {
            // Attempt to use the cached version of the page if it was loaded less than 30 minutes ago.
            if (caching == Caching.UseCache && pageCache.ContainsKey(url))
            {
                var cache = pageCache[url];
                var cacheAge = DateTime.Now - cache.Timestamp;

                if (cacheAge.TotalMinutes < 30)
                {
                    if (cacheAge.TotalSeconds > 4)
                        OnStatusChanged(shortDescription + " loaded from memory!\n");
                    return cache.Doc;
                }
            }

            OnStatusChanged(url + "\n");

            // Limit to no more than 5 parallel requests
            await ss.WaitAsync(token);

            try
            {
                HtmlDocument htmldoc = new HtmlDocument();

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

                    try
                    {
                        while (result == null && tries < maxtries && token.IsCancellationRequested == false)
                        {
                            if (tries > 0)
                            {
                                // If we have to retry loading the page, give it a short delay.
                                await Task.Delay(TimeSpan.FromSeconds(4));
                                OnStatusChanged("Retrying: " + shortDescription + "\n");
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
                        OnStatusChanged(shortDescription + ": " + e.Message);
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
                    OnStatusChanged("Failed to load: " + shortDescription + "\n");
                    return null;
                }

                htmldoc.LoadHtml(result);

                pageCache[url] = new CachedPage(htmldoc);

                OnStatusChanged(shortDescription + " loaded!\n");

                return htmldoc;
            }
            finally
            {
                ss.Release();
            }
        }

        #endregion

        #region Private assist functions
        /// <summary>
        /// Review cache to prevent its size from getting out of hand.
        /// </summary>
        private void ReviewCache()
        {
            if (pageCache.Count > maxCacheEntries)
            {
                var newestEntries = pageCache.OrderByDescending(p => p.Value.Timestamp).Take(maxCacheEntries);
                var olderEntries = pageCache.Except(newestEntries).ToList();

                foreach (var entry in olderEntries)
                {
                    pageCache.Remove(entry.Key);
                }

                GC.Collect();
            }
        }
        #endregion
    }
}
