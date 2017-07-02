using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using NetTally.Cache;
using NetTally.Extensions;
using NetTally.SystemInfo;

namespace NetTally.Web
{
    public class WebPageProvider : PageProviderBase, IPageProvider
    {
        #region Fields
        HttpClient client;
        const int retryLimit = 3;
        readonly TimeSpan timeout = TimeSpan.FromSeconds(7);
        readonly TimeSpan retryDelay = TimeSpan.FromSeconds(4);
        #endregion

        #region Construction, Setup, Disposal
        public WebPageProvider(HttpClientHandler handler, IClock clock)
            : base(handler, clock)
        {
            SetupHandler();
            SetupClient();
        }

        protected override void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                client.Dispose();
            }

            base.Dispose(itIsSafeToAlsoFreeManagedObjects);
        }

        private void SetupHandler()
        {
            ClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        private void SetupClient()
        {
            // In the event of slow response probably caused by
            // proxy lookup failures, can turn it off here.
            // See also: https://support.microsoft.com/en-us/help/2445570/slow-response-working-with-webdav-resources-on-windows-vista-or-windows-7
            //ClientHandler.UseProxy = false;

            client = new HttpClient(ClientHandler);

            client.Timeout = timeout;
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
        /// Asynchronously load a specific web page.
        /// </summary>
        /// <param name="url">The URL of the page to load.  Cannot be null.</param>
        /// <param name="shortDescrip">A short description that can be used in status updates.  If null, no update will be given.</param>
        /// <param name="caching">Indicator of whether to query the cache for the requested page.</param>
        /// <param name="shouldCache">Indicates whether the result of this page load should be cached.</param>
        /// <param name="suppressNotifications">Indicates whether notification messages should be sent to output.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>
        /// Returns an HTML document, if it can be loaded.
        /// </returns>
        public async Task<HtmlDocument> GetPage(string url, string shortDescrip, CachingMode caching, ShouldCache shouldCache,
            SuppressNotifications suppressNotifications, CancellationToken token)
        {
            HtmlDocument htmldoc = null;

            string content = await GetPageContent(url, shortDescrip, caching, shouldCache, suppressNotifications, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(content))
            {
                htmldoc = new HtmlDocument();

                await Task.Run(() => htmldoc.LoadHtml(content), token).ConfigureAwait(false);
            }

            return htmldoc;
        }

        /// <summary>
        /// Gets the XML page.
        /// </summary>
        /// <param name="url">The URL of the page to load.  Cannot be null.</param>
        /// <param name="shortDescrip">A short description that can be used in status updates.  If null, no update will be given.</param>
        /// <param name="caching">Indicator of whether to query the cache for the requested page.</param>
        /// <param name="shouldCache">Indicates whether the result of this page load should be cached.</param>
        /// <param name="suppressNotifications">Indicates whether notification messages should be sent to output.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an XML document, if it can be loaded.</returns>
        public async Task<XDocument> GetXmlPage(string url, string shortDescrip, CachingMode caching, ShouldCache shouldCache,
            SuppressNotifications suppressNotifications, CancellationToken token)
        {
            XDocument xmldoc = null;

            string content = await GetPageContent(url, shortDescrip, caching, shouldCache, suppressNotifications, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(content))
            {
                xmldoc = XDocument.Parse(content);
            }

            return xmldoc;
        }

        /// <summary>
        /// Loads the HEAD of the requested URL, and returns the response URL value.
        /// For a site that redirects some queries, this allows you to get the 'real' URL for a given short URL.
        /// </summary>
        /// <param name="url">The URL of the page to load.  Cannot be null.</param>
        /// <param name="shortDescrip">A short description that can be used in status updates.  If null, no update will be given.</param>
        /// <param name="caching">Indicator of whether to query the cache for the requested page.</param>
        /// <param name="shouldCache">Indicates whether the result of this page load should be cached.</param>
        /// <param name="suppressNotifications">Indicates whether notification messages should be sent to output.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>
        /// Returns the URL that the response headers say we requested.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">url</exception>
        /// <exception cref="System.ArgumentException">url</exception>
        public async Task<string> GetHeaderUrl(string url, string shortDescrip,
            CachingMode caching, ShouldCache shouldCache, SuppressNotifications suppressNotifications, CancellationToken token)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException($"Url is not valid: {url}", nameof(url));

            Uri uri = new Uri(url);

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
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, uri);

                while (tries < retryLimit && token.IsCancellationRequested == false)
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
                        // As long as we got a response (whether 200 or 404), we can extract what
                        // the server thinks the URL should be.
                        using (response = await client.SendAsync(request, token).ConfigureAwait(false))
                        {
                            return response.RequestMessage.RequestUri.AbsoluteUri;
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

            NotifyStatusChange(PageRequestStatusType.Loaded, url, shortDescrip, null, suppressNotifications);
            return null;
        }
        #endregion

        #region Private

        /// <summary>
        /// Gets a well-formed URI and unescaped URL based on the provided URL.
        /// </summary>
        /// <param name="url">The URL. Cannot be null.  Must be a well-formed URL.</param>
        /// <returns>Returns a URI and unescaped URL.</returns>
        /// <exception cref="System.ArgumentNullException">url</exception>
        /// <exception cref="System.ArgumentException">url</exception>
        private static (Uri uri, string url) GetVerifiedUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException($"Url is not valid: {url}", nameof(url));

            Uri uri = new Uri(url);
            url = Uri.UnescapeDataString(url);

            return (uri, url);
        }

        /// <summary>
        /// Gets the cached content for the provided URL, if any, and if flagged to use caching.
        /// </summary>
        /// <param name="url">The URL to search for.</param>
        /// <param name="caching">The caching mode.</param>
        /// <returns>Returns a (bool,string) tuple of whether there was cached content found, and what it was if found.</returns>
        private async Task<(bool found, string content)> GetCachedContent(string url, CachingMode caching)
        {
            if (caching == CachingMode.UseCache)
            {
                return await Cache.GetAsync(url).ConfigureAwait(false);
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// Gets the content of the requested page.
        /// </summary>
        /// <param name="url">The URL to load.</param>
        /// <param name="shortDescrip">The short description of the page (for notifications).</param>
        /// <param name="caching">The caching mode.</param>
        /// <param name="shouldCache">Whether the requested page should be cached.</param>
        /// <param name="suppressNotifications">Whether to suppress notifications.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns the loaded resource string.</returns>
        private async Task<string> GetPageContent(string url, string shortDescrip, CachingMode caching, ShouldCache shouldCache,
            SuppressNotifications suppressNotifications, CancellationToken token)
        {
            string content = string.Empty;

            var (uri, url2) = GetVerifiedUrl(url);

            var (found, cachedContent) = await GetCachedContent(url2, caching);

            if (found)
            {
                content = cachedContent;

                NotifyStatusChange(PageRequestStatusType.LoadedFromCache, url2, shortDescrip, null, suppressNotifications);
            }
            else
            {
                content = await GetUrlContent(uri, url2, shortDescrip, caching, shouldCache, suppressNotifications, token).ConfigureAwait(false);
            }

            return content;
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
        private async Task<string> GetUrlContent(Uri uri, string url, string shortDescrip,
            CachingMode caching, ShouldCache shouldCache, SuppressNotifications suppressNotifications, CancellationToken token)
        {
            string result = null;
            int tries = 0;
            DateTime expires = CacheInfo.DefaultExpiration;

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

                HttpResponseMessage response;
                Task<HttpResponseMessage> getResponseTask = null;

                do
                {
                    token.ThrowIfCancellationRequested();

                    if (tries > 0)
                    {
                        // Delay any additional attempts after the first.
                        await Task.Delay(retryDelay, token).ConfigureAwait(false);

                        // Notify the user if we're re-trying to load the page.
                        NotifyStatusChange(PageRequestStatusType.Retry, url, shortDescrip, null, suppressNotifications);
                    }

                    tries++;

                    try
                    {
                        getResponseTask = client.GetAsync(uri, token).TimeoutAfter(timeout, token);
                        Debug.WriteLine($"Get URI {uri} task ID: {getResponseTask.Id}");

                        using (response = await getResponseTask.ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                                // Get expires value
                                // Cannot get Expires value until we move to .NET Standard 2.0.

                                // If we get a successful result, we're done.
                                break;
                            }
                            else if (PageLoadFailed(response))
                            {
                                NotifyStatusChange(PageRequestStatusType.Failed, url,
                                    GetFailureMessage(response, shortDescrip, url), null, suppressNotifications);
                                return null;
                            }
                            else if (PageWasMoved(response))
                            {
                                url = response.Content.Headers.ContentLocation.AbsoluteUri;
                                uri = new Uri(url);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (token.IsCancellationRequested)
                        {
                            // user request
                            throw;
                        }
                        else
                        {
                            // timeout via cancellation
                            Debug.WriteLine($"Attempt to load {shortDescrip} timed out/self-cancelled (TA). Tries={tries}");
                        }
                    }
                    catch (TimeoutException)
                    {
                        Debug.WriteLine($"Attempt to load {shortDescrip} timed out. Tries={tries}");
                    }
                    catch (HttpRequestException e)
                    {
                        NotifyStatusChange(PageRequestStatusType.Error, url, shortDescrip, e, suppressNotifications);
                        throw;
                    }

                } while (tries < retryLimit);

                Debug.WriteLine($"Finished getting URI {uri} task ID: {getResponseTask.Id}");

                if (result == null && tries >= retryLimit)
                    client.CancelPendingRequests();
            }
            catch (OperationCanceledException)
            {
                // If it's not a user-requested cancellation, generate a failure message.
                if (!token.IsCancellationRequested)
                {
                    NotifyStatusChange(PageRequestStatusType.Failed, url, shortDescrip, null, suppressNotifications);
                }

                throw;
            }
            finally
            {
                ss.Release();
            }

            token.ThrowIfCancellationRequested();

            if (result == null)
            {
                NotifyStatusChange(PageRequestStatusType.Failed, url, shortDescrip, null, suppressNotifications);
                return null;
            }

            if (shouldCache == ShouldCache.Yes)
                await Cache.AddAsync(url, result, expires);

            NotifyStatusChange(PageRequestStatusType.Loaded, url, shortDescrip, null, suppressNotifications);

            return result;
        }

        #endregion

        #region Functions for load failures.

        /// <summary>
        /// Determines whether the specified HTTP response is a failure.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>Returns true if it's a failure response code.</returns>
        private bool PageLoadFailed(HttpResponseMessage response)
        {
            return ((int)response.StatusCode >= 400 && (int)response.StatusCode < 600);
        }

        /// <summary>
        /// Determine if the response indicated that the requested page was moved.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>Returns true if the page was moved.</returns>
        private bool PageWasMoved(HttpResponseMessage response)
        {
            return (response.StatusCode == HttpStatusCode.Moved ||
                    response.StatusCode == HttpStatusCode.MovedPermanently ||
                    response.StatusCode == HttpStatusCode.Redirect ||
                    response.StatusCode == HttpStatusCode.TemporaryRedirect) ;
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
                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({response.StatusCode})";
                if (ViewModels.ViewModelService.MainViewModel.Options.DebugMode)
                    failureDescrip += $"\nURL: {url}";
            }
            else
            {
                // Fail all 400/500 level responses
                // Includes 429 (Too Many Requests), proposed standard not in the standard enum list
                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({(int)response.StatusCode})";
                if (ViewModels.ViewModelService.MainViewModel.Options.DebugMode)
                    failureDescrip += $"\nURL: {url}";
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
