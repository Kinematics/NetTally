using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Utility;

namespace NetTally.Web
{
    public class WebPageProvider2 : IPageProvider
    {
        #region Fields
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

        // Maximum number of simultaneous connections allowed, to guard against hammering the server.
        // Setting it to 5 or higher causes it to hang for several seconds on the last page when
        // loading SB and SV pages.
        const int maxSimultaneousConnections = 4;
        readonly SemaphoreSlim ss = new SemaphoreSlim(maxSimultaneousConnections);

        HttpClientHandler webHandler;
        HttpClient client;
        static readonly Regex baseAddressRegex = new Regex(@"^(?<baseAddress>https?://\w+(\.\w+)+(:\d+)?/)");

        WebCache Cache { get; } = WebCache.Instance;
        string UserAgent { get; } = $"{ProductInfo.Name} ({ProductInfo.Version})";

        IClock Clock { get; }

        bool _disposed;
        #endregion

        #region Constructor
        public WebPageProvider2(HttpClientHandler handler)
            : this(handler, new DefaultClock())
        {

        }

        public WebPageProvider2(HttpClientHandler handler, IClock clock)
        {
            SetupHandler(handler);
            SetupClient();

            Clock = clock ?? new DefaultClock();
        }
        #endregion

        #region Disposal
        ~WebPageProvider2()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true); //I am calling you from Dispose, it's safe
            GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
        }

        protected virtual void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                ss.Dispose();
                client?.Dispose();
                webHandler?.Dispose();
            }

            _disposed = true;
        }
        #endregion

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
        public async Task<HtmlDocument> GetPage(string url, string shortDescrip, CachingMode caching, CancellationToken token, bool shouldCache, bool suppress = false)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));


            if (caching == CachingMode.UseCache)
            {
                HtmlDocument page = Cache.Get(url);

                if (page != null)
                {
                    NotifyResult(StatusType.Cached, shortDescrip, suppress);
                    return page;
                }
            }

            NotifyRequest(url, suppress);
            

            // Limit to no more than N parallel requests
            await ss.WaitAsync(token).ConfigureAwait(false);

            try
            {
                Uri uri = new Uri(url);

                Cookie cookie = ForumCookies.GetCookie(uri);
                if (cookie != null)
                {
                    webHandler.CookieContainer.Add(uri, cookie);
                }

                string result = null;
                string failureDescrip = null;
                int maxtries = 5;
                int tries = 0;
                HttpResponseMessage response;

                try
                {
                    while (result == null && tries < maxtries && token.IsCancellationRequested == false)
                    {
                        if (tries > 0)
                        {
                            // If we have to retry loading the page, give it a short delay.
                            await Task.Delay(TimeSpan.FromSeconds(4)).ConfigureAwait(false);
                            NotifyResult(StatusType.Retry, shortDescrip, suppress);
                        }

                        using (response = await client.GetAsync(uri, token).ConfigureAwait(false))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = await response.Content.ReadAsStringAsync();
                            }
                            else if (response.StatusCode == HttpStatusCode.NotFound ||
                                     response.StatusCode == HttpStatusCode.BadRequest ||
                                     response.StatusCode == HttpStatusCode.Gone ||
                                     response.StatusCode == HttpStatusCode.HttpVersionNotSupported ||
                                     response.StatusCode == HttpStatusCode.InternalServerError ||
                                     response.StatusCode == HttpStatusCode.NotAcceptable ||
                                     response.StatusCode == HttpStatusCode.RequestUriTooLong ||
                                     response.StatusCode == HttpStatusCode.ServiceUnavailable
                                     )
                            {
                                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({response.StatusCode})\nURL: {url}";
                                tries = maxtries;
                            }
                            else if (response.StatusCode == HttpStatusCode.Forbidden ||
                                     response.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                // Forbidden/unauthorized results might be because of CloudFlare bot blocking or similar.
                                // Let the user know they may need to contact the site admin.
                                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({(int)response.StatusCode})\nURL: {url}\nConsider contacting the site administrator.";
                                tries = maxtries;
                            }
                            else if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 600)
                            {
                                // Fail all 400/500 level responses
                                // Includes 429 (Too Many Requests), proposed standard not in the standard enum list
                                failureDescrip = $"{shortDescrip}\nReason: {response.ReasonPhrase} ({(int)response.StatusCode})\nURL: {url}";
                                tries = maxtries;
                            }
                            else if (response.StatusCode == HttpStatusCode.Moved ||
                                     response.StatusCode == HttpStatusCode.MovedPermanently ||
                                     response.StatusCode == HttpStatusCode.Redirect ||
                                     response.StatusCode == HttpStatusCode.TemporaryRedirect)
                            {
                                url = response.Content.Headers.ContentLocation.AbsoluteUri;
                                tries++;
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
                    NotifyError(shortDescrip, e);
                    throw;
                }
                catch (OperationCanceledException e)
                {
                    Debug.WriteLine(string.Format("Operation was cancelled in task {0}.", Task.CurrentId.HasValue ? Task.CurrentId.Value : -1));
                    Debug.WriteLine($"Cancellation requested: {e.CancellationToken.IsCancellationRequested}  at source: {e.Source}");

                    if (token.IsCancellationRequested)
                        throw;
                }

                if (token.IsCancellationRequested)
                {
                    return null;
                }

                if (result == null)
                {
                    NotifyResult(StatusType.Failed, failureDescrip ?? shortDescrip, suppress);
                    return null;
                }

                HtmlDocument htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(result);

                if (shouldCache)
                    Cache.Add(url, result);

                NotifyResult(StatusType.Loaded, shortDescrip, suppress);

                return htmldoc;
            }
            finally
            {
                ss.Release();
            }
        }
        #endregion

        #region Private support functions
        /// <summary>
        /// Set up the HTTP client handler object for use in managing underlying connections.
        /// </summary>
        private void SetupHandler(HttpClientHandler handler)
        {
            webHandler = handler ?? new HttpClientHandler();

            webHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        }

        /// <summary>
        /// Set up the client to be used for making requests.
        /// </summary>
        private void SetupClient()
        {
            client = new HttpClient(webHandler);

            client.MaxResponseContentBufferSize = 1000000;
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "text/html");
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");

            var handlerInfo = webHandler.GetType().GetTypeInfo();
            var infoName = handlerInfo.FullName;

            // Native client handler breaks if we set the accept-encoding.
            // It handles auto-compression on its own.
            if (infoName != "ModernHttpClient.NativeMessageHandler")
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");

            Uri baseUri = new Uri("http://forums.sufficientvelocity.com/");
            client.BaseAddress = baseUri;
        }


        /// <summary>
        /// Send status update when requesting a page URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="suppress">if set to <c>true</c> [suppress].</param>
        private void NotifyRequest(string url, bool suppress)
        {
            if (!suppress && !string.IsNullOrEmpty(url))
            {
                OnStatusChanged($"{url}\n");
            }
        }

        /// <summary>
        /// Sends a status update that the tally was cancelled.
        /// </summary>
        private void NotifyCancel()
        {
            OnStatusChanged($"Tally cancelled!\n");
        }

        /// <summary>
        /// Sends a status update indicating that there was an error.
        /// </summary>
        /// <param name="shortDescrip">The short descrip.</param>
        /// <param name="e">The e.</param>
        private void NotifyError(string shortDescrip, Exception e)
        {
            if (string.IsNullOrEmpty(shortDescrip))
                return;

            OnStatusChanged($"{shortDescrip}: {e?.Message ?? "(unknown error)"}");
        }

        /// <summary>
        /// Sends a status update for a load attempt result.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="shortDescrip">The short descrip.</param>
        /// <param name="suppress">if set to <c>true</c> [suppress].</param>
        private void NotifyResult(StatusType status, string shortDescrip, bool suppress)
        {
            if (string.IsNullOrEmpty(shortDescrip) || suppress)
                return;

            StringBuilder sb = new StringBuilder();

            switch (status)
            {
                case StatusType.Retry:
                    sb.Append("Retrying: ");
                    sb.Append(shortDescrip);
                    break;
                case StatusType.Failed:
                    sb.Append("Failed to load: ");
                    sb.Append(shortDescrip);
                    break;
                case StatusType.Cached:
                    sb.Append(shortDescrip);
                    sb.Append(" loaded from memory!");
                    break;
                case StatusType.Loaded:
                    sb.Append(shortDescrip);
                    sb.Append(" loaded!");
                    break;
                default:
                    return;
            }

            sb.Append("\n");
            OnStatusChanged(sb.ToString());
        }
        #endregion
    }
}
