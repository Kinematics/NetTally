﻿using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Cache;
using NetTally.Extensions;
using NetTally.Options;
using NetTally.SystemInfo;
using NetTally.Types.Enums;

namespace NetTally.Web
{
    public class WebPageProvider : PageProviderBase, IPageProvider
    {
        #region Fields
        readonly HttpClient httpClient;
        readonly ILogger<WebPageProvider> logger;

        const int retryLimit = 3;
        readonly TimeSpan timeout = TimeSpan.FromSeconds(7);
        readonly TimeSpan retryDelay = TimeSpan.FromSeconds(4);

        readonly IGeneralInputOptions inputOptions;
        #endregion

        #region Construction, Setup, Disposal
        public WebPageProvider(HttpClientHandler handler,
            ICache<string> pageCache, IClock clock,
            IGeneralInputOptions inputOptions, ILogger<WebPageProvider> logger)
            : base(handler, pageCache, clock)
        {
            this.inputOptions = inputOptions;
            this.logger = logger;

            SetupHandler();
            httpClient = SetupClient();
        }

        protected override void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                if (httpClient != null)
                    httpClient.Dispose();
            }

            base.Dispose(itIsSafeToAlsoFreeManagedObjects);
        }

        /// <summary>
        /// Setup properties on the Client Handler.
        /// </summary>
        private void SetupHandler()
        {
            ClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        /// <summary>
        /// Create a new HTTP Client based on the client handler.
        /// Setup properties on the HTTP Client.
        /// </summary>
        private HttpClient SetupClient()
        {
            // In the event of slow response probably caused by
            // proxy lookup failures, we can turn it off here.
            // See also: https://support.microsoft.com/en-us/help/2445570/slow-response-working-with-webdav-resources-on-windows-vista-or-windows-7
            ClientHandler.UseProxy = !inputOptions.DisableWebProxy;

            HttpClient client = new(ClientHandler);

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

            return client;
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
        public async Task<HtmlDocument?> GetHtmlDocumentAsync(string url, string shortDescrip, CachingMode caching, ShouldCache shouldCache,
            SuppressNotifications suppressNotifications, CancellationToken token)
        {
            logger.LogInformation("Requested HTML document \"{shortDescrip}\"", shortDescrip);
            HtmlDocument? htmldoc = null;

            string content = await GetPageContent(url, shortDescrip, caching, shouldCache, suppressNotifications, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(content))
            {
                logger.LogInformation("\"{shortDescrip}\" successfully loaded from web.", shortDescrip);
                htmldoc = new HtmlDocument();

                await Task.Run(() => htmldoc.LoadHtml(content), token).ConfigureAwait(false);
                logger.LogDebug("\"{shortDescrip}\" successfully parsed into HtmlDocument.", shortDescrip);
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
        public async Task<XDocument?> GetXmlDocumentAsync(string url, string shortDescrip, CachingMode caching, ShouldCache shouldCache,
            SuppressNotifications suppressNotifications, CancellationToken token)
        {
            logger.LogInformation("Requested XML document \"{shortDescrip}\"", shortDescrip);
            XDocument? xmldoc = null;

            string content = await GetPageContent(url, shortDescrip, caching, shouldCache, suppressNotifications, token).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(content))
            {
                logger.LogInformation("\"{shortDescrip}\" successfully loaded.", shortDescrip);
                xmldoc = XDocument.Parse(content);
                logger.LogDebug("\"{shortDescrip}\" successfully parsed into XDocument.", shortDescrip);
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
        public async Task<string> GetRedirectUrlAsync(string url, string? shortDescrip,
            CachingMode caching, ShouldCache shouldCache, SuppressNotifications suppressNotifications, CancellationToken token)
        {
            logger.LogInformation("Requested URL redirect for \"{shortDescrip}\"", shortDescrip);
            Uri? responseUri = await GetRedirectedHeaderRequestUri(url, shortDescrip, suppressNotifications, token);

            string result = responseUri?.AbsoluteUri ?? string.Empty;

            if (string.IsNullOrEmpty(result))
                logger.LogDebug("Redirect request failed for \"{shortDescrip}\".", shortDescrip);
            else
                logger.LogDebug("Redirect request succeeded. Using {result}", result);

            return result;
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

            Uri uri = new(url);
            string url2 = Uri.UnescapeDataString(url);

            return (uri, url2);
        }

        /// <summary>
        /// Gets the cached content for the provided URL, if any, and if flagged to use caching.
        /// </summary>
        /// <param name="url">The URL to search for.</param>
        /// <param name="caching">The caching mode.</param>
        /// <returns>Returns a (bool,string) tuple of whether there was cached content found, and what it was if found.</returns>
        private (bool found, string content) GetCachedContent(string url, CachingMode caching)
        {
            if (caching == CachingMode.UseCache)
            {
                return Cache.Get(url);
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
            var (uri, url2) = GetVerifiedUrl(url);

            var (found, content) = GetCachedContent(url2, caching);

            if (found)
            {
                NotifyStatusChange(PageRequestStatusType.LoadedFromCache, url2, shortDescrip, null, suppressNotifications);
            }
            else
            {
                content = await GetUrlContent(uri, url2, shortDescrip, shouldCache, suppressNotifications, token).ConfigureAwait(false) ?? string.Empty;
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
        private async Task<string?> GetUrlContent(Uri uri, string url, string shortDescrip,
            ShouldCache shouldCache, SuppressNotifications suppressNotifications, CancellationToken token)
        {
            string? result = null;
            int tries = 0;
            DateTime expires = CacheInfo.DefaultExpiration;

            NotifyStatusChange(PageRequestStatusType.Requested, url, shortDescrip, null, suppressNotifications);

            // Limit to no more than N parallel requests
            await ss.WaitAsync(token).ConfigureAwait(false);

            try
            {
                Cookie? cookie = ForumCookies.GetCookie(uri);
                if (cookie != null)
                {
                    ClientHandler.CookieContainer.Add(uri, cookie);
                }

                string? authorization = ForumAuthentications.GetAuthorization(uri);
                if (authorization != null && !httpClient.DefaultRequestHeaders.Contains("Authorization"))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", authorization);
                }

                Task<HttpResponseMessage>? getResponseTask = null;

                do
                {
                    token.ThrowIfCancellationRequested();

                    if (tries > 0)
                    {
                        // Delay any additional attempts after the first.
                        await Task.Delay(retryDelay, token).ConfigureAwait(false);

                        // Notify the user if we're making another attempt to load the page.
                        NotifyStatusChange(PageRequestStatusType.Retry, url, shortDescrip, null, suppressNotifications);
                    }

                    tries++;

                    try
                    {
                        getResponseTask = httpClient.GetAsync(uri, token).TimeoutAfter(timeout, token);
                        logger.LogDebug("Get URI {uri} task ID: {Id}", uri, getResponseTask.Id);

                        using (var response = await getResponseTask.ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

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
                                if (response.Content.Headers.ContentLocation is Uri contentLocation)
                                {
                                    url = contentLocation.AbsoluteUri;
                                    uri = new Uri(url);
                                }
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
                            logger.LogDebug("Attempt to load {shortDescrip} timed out/self-cancelled (TA). Tries={tries}",
                                shortDescrip, tries);
                        }
                    }
                    catch (TimeoutException)
                    {
                        logger.LogDebug("Attempt to load {shortDescrip} timed out. Tries={tries}",
                            shortDescrip, tries);
                    }
                    catch (HttpRequestException e)
                    {
                        NotifyStatusChange(PageRequestStatusType.Error, url, shortDescrip, e, suppressNotifications);
                        throw;
                    }

                } while (tries < retryLimit);

                logger.LogDebug("Finished getting URI {uri} task ID: {getResponseTaskId}",
                    uri, getResponseTask?.Id ?? 0);

                if (result == null && tries >= retryLimit)
                    httpClient.CancelPendingRequests();
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
                Cache.Add(url, result, expires);

            NotifyStatusChange(PageRequestStatusType.Loaded, url, shortDescrip, null, suppressNotifications);

            return result;
        }

        /// <summary>
        /// Loads the HEAD of the requested URL, and returns the URI from the returned request header.
        /// </summary>
        /// <param name="url">The URL to load.</param>
        /// <param name="shortDescrip">Short description of the page being loaded.</param>
        /// <param name="suppressNotifications">Whether to suppress notifications.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns the URI, if the page is loaded. Otherwise null.</returns>
        private async Task<Uri?> GetRedirectedHeaderRequestUri(string url, string? shortDescrip, SuppressNotifications suppressNotifications, CancellationToken token)
        {
            var (uri, url2) = GetVerifiedUrl(url);

            NotifyStatusChange(PageRequestStatusType.Requested, url, shortDescrip, null, suppressNotifications);

            // Limit to no more than N parallel requests
            await ss.WaitAsync(token).ConfigureAwait(false);

            try
            {
                Cookie? cookie = ForumCookies.GetCookie(uri);
                if (cookie != null)
                {
                    ClientHandler.CookieContainer.Add(uri, cookie);
                }

                string? authorization = ForumAuthentications.GetAuthorization(uri);
                if (authorization != null)
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", authorization);
                }

                int tries = 0;

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
                        using HttpRequestMessage request = new(HttpMethod.Head, uri);
                        // As long as we got a response (whether 200 or 404), we can extract what
                        // the server thinks the URL should be.
                        using (HttpResponseMessage response = await httpClient.SendAsync(request, token).ConfigureAwait(false))
                        {
                            return response.RequestMessage?.RequestUri;
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        NotifyStatusChange(PageRequestStatusType.Error, url, shortDescrip, e, suppressNotifications);
                        throw;
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
                            logger.LogDebug("Attempt to load {shortDescrip} timed out/self-cancelled (TA). Tries={tries}",
                                shortDescrip, tries);
                        }
                    }
                    catch (TimeoutException)
                    {
                        logger.LogDebug("Attempt to load {shortDescrip} timed out. Tries={tries}",
                            shortDescrip, tries);
                    }

                } while (tries < retryLimit);
            }
            finally
            {
                httpClient.DefaultRequestHeaders.Remove("Authorization");

                ss.Release();
            }

            return null;
        }
        #endregion

        #region Functions for load failure checks.
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
                    response.StatusCode == HttpStatusCode.TemporaryRedirect);
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
            string failureDescrip;

            if (Enum.IsDefined(typeof(HttpStatusCode), response.StatusCode))
            {
                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({response.StatusCode})";
                if (inputOptions.DebugMode)
                    failureDescrip += $"\nURL: {url}";
            }
            else
            {
                // Fail all 400/500 level responses
                // Includes 429 (Too Many Requests), proposed standard not in the standard enum list
                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({(int)response.StatusCode})";
                if (inputOptions.DebugMode)
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
