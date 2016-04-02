using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Utility;

namespace NetTally.Web
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

        // Maximum number of simultaneous connections allowed, to guard against hammering the server.
        const int maxSimultaneousConnections = 4;
        readonly SemaphoreSlim ss = new SemaphoreSlim(maxSimultaneousConnections);

        WebCache Cache { get; } = WebCache.Instance;
        string UserAgent { get; }

        IClock Clock { get; }

        bool _disposed = false;

        public WebPageProvider(IClock clock = null)
        {
            var assembly = typeof(WebPageProvider).GetTypeInfo().Assembly;
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));
            UserAgent = $"{product.Product} ({version.InformationalVersion})";

            Clock = clock ?? new DefaultClock();
        }

        #region Disposal
        ~WebPageProvider()
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
        public async Task<HtmlDocument> GetPage(string url, string shortDescrip, CachingMode caching, CancellationToken token, bool shouldCache = true)
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

            // Limit to no more than 5 parallel requests
            await ss.WaitAsync(token).ConfigureAwait(false);

            try
            {
                string result = null;
                int maxtries = 5;
                int tries = 0;
                HttpClient client;
                HttpResponseMessage response;
                HttpClientHandler handler = GetHandler(url);

                using (client = new HttpClient(handler))
                {
                    client.MaxResponseContentBufferSize = 1000000;
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("Accept", "text/html");
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate");

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
                                    StreamReader reader;

                                    if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                                    {
                                        reader = new StreamReader(
                                            new GZipStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress));
                                    }
                                    else if (response.Content.Headers.ContentEncoding.Contains("deflate"))
                                    {
                                        reader = new StreamReader(
                                            new DeflateStream(await response.Content.ReadAsStreamAsync(), CompressionMode.Decompress));
                                    }
                                    else
                                    {
                                        reader = new StreamReader(await response.Content.ReadAsStreamAsync());
                                    }

                                    result = await reader.ReadToEndAsync();

                                    reader.Dispose();
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
                }

                if (token.IsCancellationRequested)
                {
                    return null;
                }

                if (result == null)
                {
                    UpdateStatus(StatusType.Failed, shortDescrip);
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
        /// Get an HTTP client handler object for a given URL.
        /// Insert any needed cookies.
        /// </summary>
        /// <param name="url">The URL for the client handler to service.</param>
        /// <returns>Returns a client handler, with cookies if needed.</returns>
        private static HttpClientHandler GetHandler(string url)
        {
            HttpClientHandler handler = new HttpClientHandler();

            CookieCollection cookies = ForumCookies.GetCookies(url);
            if (cookies.Count > 0)
            {
                CookieContainer cookieJar = new CookieContainer();
                cookieJar.Add(cookies);
                handler.CookieContainer = cookieJar;
            }

            return handler;
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
