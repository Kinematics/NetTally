using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.CustomEventArgs;
using NetTally.Enums;
using NetTally.Input.Web.Handlers;
using NetTally.Utility;
using NetTally.Utility.Cache;
using NetTally.Web;

namespace NetTally.Input.Web;

public class WebPageProvider2 : IDisposable, IPageProvider
{
    private readonly ILogger<WebPageProvider2> logger;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly CacheService cacheService;
    private readonly IOptions<GlobalSettings> options;
    private readonly GlobalSettings settings;
    private readonly TimeProvider timeProvider;

    private readonly Dictionary<string, string> UrlDescriptions = [];
    private const int maxSimultaneousConnections = 4;
    private readonly SemaphoreSlim ss = new(maxSimultaneousConnections);
    private bool _disposed;

    private HttpClient? _client;

    #region Construction
    public WebPageProvider2(
        ILogger<WebPageProvider2> logger,
        IHttpClientFactory httpClientFactory,
        CacheService cacheService,
        IOptions<GlobalSettings> options,
        TimeProvider timeProvider
        )
    {
        this.logger = logger;
        this.httpClientFactory = httpClientFactory;
        this.cacheService = cacheService;
        this.options = options;
        this.settings = options.Value;
        this.timeProvider = timeProvider;

        RetryFailureHandler.RetryFailed += RetryFailureHandler_RetryFailed;
    }

    private HttpClient GetClient(string urlString)
    {
        if (_client is null)
        {
            Uri uri = new(urlString);

            _client = (uri.Host, settings.DisableWebProxy) switch
            {
                ("api.github.com", _) => httpClientFactory.CreateClient(ConfigStrings.Github),
                (_, true) => httpClientFactory.CreateClient(ConfigStrings.NoProxy),
                (_, false) => httpClientFactory.CreateClient(ConfigStrings.WithProxy),
            };

            Cookie? cookie = ForumCookies.GetCookie(uri, timeProvider);
            if (cookie is not null)
            {
                _client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Name}={cookie.Value}");
            }

            string? authorization = ForumAuthentications.GetAuthorization(uri);
            if (authorization != null)
            {
                _client.DefaultRequestHeaders.Add("Authorization", authorization);
            }
        }

        return _client;
    }
    #endregion Construction

    #region Disposal
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            RetryFailureHandler.RetryFailed -= RetryFailureHandler_RetryFailed;
            ss.Dispose();
        }

        _disposed = true;
    }
    #endregion Disposal

    #region IPageProvider Methods
    public async Task<HtmlDocument?> GetHtmlDocumentAsync(
        string urlString,
        string description,
        CachingMode cachingMode,
        SuppressNotifications suppressNotifications,
        CancellationToken token)
    {
        string? content = await GetDocumentContentAsync(
            urlString, description, "HTML",
            cachingMode, suppressNotifications, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (content is null)
        {
            return null;
        }

        HtmlDocument htmldoc = new();
        htmldoc.LoadHtml(content);

        return htmldoc;
    }

    public async Task<XDocument?> GetXmlDocumentAsync(
        string urlString,
        string description,
        CachingMode cachingMode,
        SuppressNotifications suppressNotifications,
        CancellationToken token)
    {
        string? content = await GetDocumentContentAsync(
            urlString, description, "XML",
            cachingMode, suppressNotifications, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (content is null)
        {
            return null;
        }

        XDocument xmlDoc = XDocument.Parse(content);

        return xmlDoc;
    }

    public async Task<string?> GetJsonDocumentAsync(
        string urlString,
        string description,
        CachingMode cachingMode,
        SuppressNotifications suppressNotifications,
        CancellationToken token)
    {
        string? content = await GetDocumentContentAsync(
            urlString, description, "JSON",
            cachingMode, suppressNotifications, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return content;
    }

    private async Task<string?> GetDocumentContentAsync(
        string urlString,
        string description,
        string docType,
        CachingMode cachingMode,
        SuppressNotifications suppressNotifications,
        CancellationToken token)
    {
        logger.LogInformation("Requested {docType} document \"{description}\" ({url})",
            docType, description, urlString);

        UrlDescriptions[urlString] = description;

        if (!TryGetContentFromCache(urlString, description, cachingMode, suppressNotifications,
            out string? content))
        {
            content = await GetContentFromWeb(urlString, description, cachingMode, suppressNotifications, token)
                .ConfigureAwait(ConfigureAwaitOptions.None);
        }

        return content;
    }

    public async Task<string> GetRedirectUrlAsync(
        string urlString,
        string description,
        SuppressNotifications suppressNotifications,
        CancellationToken token)
    {
        logger.LogDebug("Requested URL redirect for \"{description}\"", description);

        UrlDescriptions[urlString] = description;

        string? responseUri = await GetRedirectedHeaderRequestUri(urlString, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (string.IsNullOrEmpty(responseUri))
            logger.LogDebug("Redirect request failed for \"{description}\".", description);
        else
            logger.LogDebug("Redirect request succeeded. Using {responseUri}", responseUri);

        return responseUri ?? urlString;
    }
    #endregion IPageProvider Methods

    #region Read Content

    private bool TryGetContentFromCache(
        string urlString,
        string description,
        CachingMode cachingMode,
        SuppressNotifications suppressNotifications,
        [NotNullWhen(true)] out string? content)
    {
        if (cachingMode is CachingMode.ReadOnly or CachingMode.ReadWrite)
        {
            var (_, url) = GetVerifiedUrl(urlString);

            if (cacheService.TryGet(url, out content))
            {
                NotifyStatusChange(PageRequestStatusType.LoadedFromCache,
                    urlString, description, null, suppressNotifications);

                return true;
            }
        }

        content = default;
        return false;
    }

    private async Task<string?> GetContentFromWeb(
        string urlString,
        string description,
        CachingMode cachingMode,
        SuppressNotifications suppressNotifications,
        CancellationToken token)
    {
        var (_, url) = GetVerifiedUrl(urlString);
        var client = GetClient(url);

        // Limit to no more than N parallel requests
        await ss.WaitAsync(token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        token.ThrowIfCancellationRequested();

        try
        {
            var response = await client.GetAsync(url, token)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            token.ThrowIfCancellationRequested();

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync(token)
                    .ConfigureAwait(ConfigureAwaitOptions.None);

                result = result.RemoveUnsafeCharacters().Trim();

                if (!string.IsNullOrEmpty(result))
                {
                    if (cachingMode is CachingMode.ReadWrite or CachingMode.WriteOnly)
                        cacheService.Add(url, result);

                    NotifyStatusChange(PageRequestStatusType.Loaded,
                        urlString, description, null, suppressNotifications);

                    return result;
                }

                NotifyStatusChange(PageRequestStatusType.Failed, url,
                    $"{description} - No content", null, suppressNotifications);

                return null;
            }

            NotifyStatusChange(PageRequestStatusType.Failed, url,
                GetFailureMessage(response, description, url), null, suppressNotifications);

            return null;
        }
        catch (OperationCanceledException e)
        {
            // If it's not a user-requested cancellation, generate a failure message.
            if (token.IsCancellationRequested)
            {
                NotifyStatusChange(PageRequestStatusType.Cancelled, url, description, e, suppressNotifications);
            }
            else
            {
                NotifyStatusChange(PageRequestStatusType.Failed, url, description, e, suppressNotifications);
            }

            throw;
        }
        finally
        {
            ss.Release();
        }
    }

    private async Task<string?> GetRedirectedHeaderRequestUri(string urlString, CancellationToken token)
    {
        var (uri, url) = GetVerifiedUrl(urlString);
        var client = GetClient(url);

        using HttpRequestMessage request = new(HttpMethod.Head, uri);

        // As long as we got a response (whether 200 or 404), we can extract what
        // the server thinks the URL should be.
        using HttpResponseMessage response = await client.SendAsync(request, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        return response.RequestMessage?.RequestUri?.AbsoluteUri;
    }

    private static (Uri uri, string url) GetVerifiedUrl(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            throw new ArgumentException($"Url is not valid: {url}", nameof(url));

        Uri uri = new(url);
        string url2 = Uri.UnescapeDataString(url);

        return (uri, url2);
    }
    #endregion Read Content

    #region Messaging
    public event EventHandler<MessageEventArgs>? StatusChanged;

    private void OnStatusChanged(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        MessageEventArgs eventArgs = new(message);

        StatusChanged?.Invoke(this, eventArgs);
    }

    private void NotifyStatusChange(
        PageRequestStatusType status,
        string url,
        string description,
        Exception? e,
        SuppressNotifications suppressNotifications)
    {
        string msg = status switch
        {
            PageRequestStatusType.Requested => $"{url}\n",
            PageRequestStatusType.Loaded => $"{description} loaded!\n",
            PageRequestStatusType.LoadedFromCache => $"{description} loaded from memory!\n",
            PageRequestStatusType.Cancelled => "Tally cancelled!\n",
            PageRequestStatusType.Retry when e is not null => $"Retrying: {description}\n>> {e.Message}\n",
            PageRequestStatusType.Retry => $"Retrying: {description}\n",
            PageRequestStatusType.Failed when e is not null => $"Failed to load: {description}{(settings.DebugMode ? $" ({url})" : "")}\n>> {e.Message}\n",
            PageRequestStatusType.Failed => $"Failed to load: {description}{(settings.DebugMode ? $" ({url})" : "")}\n",
            PageRequestStatusType.Error => $"{description}: {e?.Message ?? "(unknown error)"}\n",
            _ => ""
        };

        logger.LogDebug("{msg}", msg);

        if (suppressNotifications == SuppressNotifications.No)
            OnStatusChanged(msg);
    }

    private void RetryFailureHandler_RetryFailed(object sender, RetryFailedEventArgs e)
    {
        if (!UrlDescriptions.TryGetValue(e.Url, out string? description))
        {
            description = e.Response.RequestMessage?.RequestUri?.AbsolutePath ?? "";
        }

        if (e.ReachedMaxRetries)
        {
            logger.LogDebug("Tried: {description} - Attempt {count} - Failed", description, e.RetryCount);
            NotifyStatusChange(PageRequestStatusType.Failed, e.Url, description, e.Exception, SuppressNotifications.No);
        }
        else
        {
            logger.LogDebug("Tried: {description} - Attempt {count} - Retrying", description, e.RetryCount);
            NotifyStatusChange(PageRequestStatusType.Retry, e.Url, description, e.Exception, SuppressNotifications.No);
        }
    }

    /// <summary>
    /// Gets the failure message for a given response code.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="description">The short descrip.</param>
    /// <param name="url">The URL.</param>
    /// <returns></returns>
    private string GetFailureMessage(HttpResponseMessage response, string description, string url)
    {
        StringBuilder failure = new();

        failure.AppendLine(description);
        failure.Append("Reason: ");
        failure.Append(response.ReasonPhrase);
        failure.Append(" (");
        failure.Append((int)response.StatusCode);
        failure.Append(')');
        if (settings.DebugMode)
        {
            failure.Append("\nURL: ");
            failure.Append(url);
        }
        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            failure.Append("\nConsider contacting the site administrator.");
        }

        return failure.ToString();
    }
    #endregion Messaging
}
