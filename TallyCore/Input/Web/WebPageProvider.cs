﻿using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Utility;

namespace NetTally.Web
{
    public class WebPageProvider : PageProviderBase, IPageProvider
    {
        #region Fields
        HttpClient client;
        const int retryLimit = 5;
        #endregion

        #region Constructor/Disposal
        public WebPageProvider()
            : this(null, null, null)
        {

        }

        public WebPageProvider(HttpClientHandler handler)
            : this(handler, null, null)
        {

        }

        public WebPageProvider(HttpClientHandler handler, WebCache cache)
            : this(handler, cache, null)
        {

        }

        public WebPageProvider(HttpClientHandler handler, IClock clock)
            : this(handler, null, clock)
        {

        }

        public WebPageProvider(HttpClientHandler handler, WebCache cache, IClock clock)
            : base(handler, cache, clock)
        {
            SetupHandler();
            SetupClient();
        }

        protected override void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            base.Dispose(itIsSafeToAlsoFreeManagedObjects);

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                client?.Dispose();
            }

        }
        #endregion

        #region Setup
        private void SetupHandler()
        {
            ClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        private void SetupClient()
        {
            client = new HttpClient(ClientHandler);

            client.MaxResponseContentBufferSize = 1000000;
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "text/html");
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");

            // Native client handler breaks if we set the accept-encoding.
            // It handles auto-compression on its own.
            var handlerInfo = ClientHandler.GetType().GetTypeInfo();
            if (handlerInfo.FullName != "ModernHttpClient.NativeMessageHandler")
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");

            // Have to set the BaseAddress for mobile client code to work properly.
            client.BaseAddress = new Uri("http://forums.sufficientvelocity.com/");
        }
        #endregion

        #region IPageProvider
        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            Cache.Clear();
        }

        /// <summary>
        /// If we're notified that a given attempt to load pages is done, we can
        /// tell the web page cache to expire old data.
        /// </summary>
        public void DoneLoading()
        {
            Cache.ExpireCache(Clock.Now);
        }

        /// <summary>
        /// Asynchronously load a specific page.
        /// </summary>
        /// <param name="url">The URL of the page to load.  Cannot be null.</param>
        /// <param name="shortDescrip">A short description that can be used in status updates.  If null, no update will be given.</param>
        /// <param name="caching">Indicator of whether to query the cache for the requested page.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="shouldCache">Indicates whether the result of this page load should be cached.</param>
        /// <returns>Returns an HTML document, if it can be loaded.</returns>
        /// <exception cref="ArgumentNullException">If url is null or empty.</exception>
        /// <exception cref="ArgumentException">If url is not a valid absolute url.</exception>
        public async Task<HtmlDocument> GetPage(string url, string shortDescrip,
            CachingMode caching, ShouldCache shouldCache, SuppressNotifications suppressNotifications, CancellationToken token)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException($"Url is not valid: {url}", nameof(url));

            Uri uri = new Uri(url);
            HtmlDocument htmldoc;
            string result = null;
            string failureDescrip = null;

            var (found, doc) = await TryGetCachedPageAsync(url, shortDescrip, caching, suppressNotifications).ConfigureAwait(false);
            if (found)
                return doc;

            NotifyStatusChange(PageRequestStatusType.Requested, url, shortDescrip, null, suppressNotifications);

            // Limit to no more than N parallel requests
            await ss.WaitAsync(token).ConfigureAwait(false);

            try
            {
                Cookie cookie = ForumCookies.GetCookie(uri);
                if (cookie != null)
                {
                    ClientHandler.CookieContainer.Add(uri, cookie);
                }

                int tries = 0;
                HttpResponseMessage response;

                while (result == null && tries < retryLimit && token.IsCancellationRequested == false)
                {
                    if (tries > 0)
                    {
                        // If we have to retry loading the page, give it a short delay.
                        await Task.Delay(TimeSpan.FromSeconds(4)).ConfigureAwait(false);
                        NotifyStatusChange(PageRequestStatusType.Retry, url, shortDescrip, null, suppressNotifications);
                    }
                    tries++;

                    try
                    {
                        using (response = await client.GetAsync(uri, token).ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = await response.Content.ReadAsStringAsync();
                            }
                            else if (IsFailure(response))
                            {
                                failureDescrip = GetFailureMessage(response, shortDescrip, url);
                                tries = retryLimit;
                            }
                            else if (response.StatusCode == HttpStatusCode.Moved ||
                                     response.StatusCode == HttpStatusCode.MovedPermanently ||
                                     response.StatusCode == HttpStatusCode.Redirect ||
                                     response.StatusCode == HttpStatusCode.TemporaryRedirect)
                            {
                                url = response.Content.Headers.ContentLocation.AbsoluteUri;
                            }
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        NotifyStatusChange(PageRequestStatusType.Error, url, shortDescrip, e, suppressNotifications);
                        throw;
                    }

                }
            }
            finally
            {
                ss.Release();
            }

            if (token.IsCancellationRequested)
            {
                return null;
            }

            if (result == null)
            {
                if (string.IsNullOrEmpty(failureDescrip))
                    NotifyStatusChange(PageRequestStatusType.Failed, url, shortDescrip, null, suppressNotifications);
                else
                    NotifyStatusChange(PageRequestStatusType.Failed, url, failureDescrip, null, suppressNotifications);

                return null;
            }

            if (shouldCache == ShouldCache.Yes)
                Cache.Add(url, result);

            htmldoc = new HtmlDocument();
            await Task.Run(() => htmldoc.LoadHtml(result));

            NotifyStatusChange(PageRequestStatusType.Loaded, url, shortDescrip, null, suppressNotifications);

            return htmldoc;
        }
        #endregion

        #region Utility Functions        
        /// <summary>
        /// Tries to get the cached version of the requested page.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="shortDescrip">The short descrip.</param>
        /// <param name="caching">The caching.</param>
        /// <param name="suppressNotifyMessages">if set to <c>true</c> [suppress notify messages].</param>
        /// <returns>Returns whether it found the cached document, and the document, if found.</returns>
        private async Task<(bool found, HtmlDocument doc)> TryGetCachedPageAsync(string url, string shortDescrip, CachingMode caching, SuppressNotifications suppressNotifications)
        {
            HtmlDocument htmldoc = null;

            if (caching == CachingMode.UseCache)
            {
                htmldoc = await Cache.GetAsync(url).ConfigureAwait(false);

                if (htmldoc != null)
                    NotifyStatusChange(PageRequestStatusType.LoadedFromCache, url, shortDescrip, null, suppressNotifications);
            }

            return (htmldoc != null, htmldoc);
        }

        /// <summary>
        /// Determines whether the specified HTTP response is a failure.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>Returns true if it's a failure response code.</returns>
        private bool IsFailure(HttpResponseMessage response)
        {
            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 600)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the failure message for a given response code.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="shortDescrip">The short descrip.</param>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string GetFailureMessage(HttpResponseMessage response, string shortDescrip, string url)
        {
            string failureDescrip = "";

            if (Enum.IsDefined(typeof(HttpStatusCode), response.StatusCode))
            {
                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({response.StatusCode})\nURL: {url}";
            }
            else
            {
                // Fail all 400/500 level responses
                // Includes 429 (Too Many Requests), proposed standard not in the standard enum list
                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({(int)response.StatusCode})\nURL: {url}";
            }

            if (response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.Unauthorized)
            {
                failureDescrip += "\nConsider contacting the site administrator.";
            }

            return failureDescrip;
        }
        #endregion
    }
}
