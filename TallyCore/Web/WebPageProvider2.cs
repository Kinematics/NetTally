using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
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
        string UserAgent { get; set; }

        IClock Clock { get; }

        bool _disposed = false;
        #endregion

        #region Constructor
        public WebPageProvider2(HttpClientHandler handler)
            : this(handler, new DefaultClock())
        {

        }

        public WebPageProvider2(HttpClientHandler handler, IClock clock)
        {
            SetupUserAgent();
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
        public async Task<HtmlDocument> GetPage(string url, string shortDescrip, CachingMode caching, CancellationToken token, bool shouldCache)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));


            if (caching == CachingMode.UseCache)
            {
                HtmlDocument page = Cache.Get(url);

                if (page != null)
                {
                    UpdateStatus(StatusType.Cached, shortDescrip);
                    return page;
                }
            }

            UpdateStatus(StatusType.Requested, url);

            // Limit to no more than N parallel requests
            await ss.WaitAsync(token).ConfigureAwait(false);

            try
            {
                Uri uri = new Uri(url);

                Match m = baseAddressRegex.Match(url);
                if (m.Success)
                {
                    client.BaseAddress = new Uri(m.Groups["baseAddress"].Value);
                }

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
                            UpdateStatus(StatusType.Retry, shortDescrip);
                        }

                        using (response = await client.GetAsync(url, token).ConfigureAwait(false))
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
                    UpdateStatus(StatusType.Error, shortDescrip, e);
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
                    UpdateStatus(StatusType.Failed, failureDescrip ?? shortDescrip);
                    return null;
                }

                HtmlDocument htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(result);

                if (shouldCache)
                    Cache.Add(url, result);

                UpdateStatus(StatusType.Loaded, shortDescrip);

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
        /// Setup the user agent string to be used when making requests.
        /// </summary>
        private void SetupUserAgent()
        {
            UserAgent = $"{ProductInfo.Name} ({ProductInfo.Version})";
        }

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
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
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
