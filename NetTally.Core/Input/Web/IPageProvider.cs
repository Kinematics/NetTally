using System;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using HtmlAgilityPack;
using NetTally.CustomEventArgs;

namespace NetTally.Web
{
    public interface IPageProvider : IDisposable
    {
        /// <summary>
        /// Asynchronously load a specific HTML page.
        /// </summary>
        /// <param name="url">The URL of the page to load.  Cannot be null.</param>
        /// <param name="shortDescrip">A short description that can be used in status updates.  If null, no update will be given.</param>
        /// <param name="caching">Indicator of whether to query the cache for the requested page.</param>
        /// <param name="shouldCache">Indicates whether the result of this page load should be cached.</param>
        /// <param name="suppressNotifications">Indicates whether notification messages should be sent to output.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an HTML document, if it can be loaded.</returns>
        /// <exception cref="ArgumentNullException">If url is null or empty.</exception>
        Task<HtmlDocument?> GetPageAsync(string url, string shortDescrip, 
            CachingMode caching, ShouldCache shouldCache, 
            SuppressNotifications suppressNotifications, CancellationToken token);

        /// <summary>
        /// Asynchronously load a specific XML page.
        /// </summary>
        /// <param name="url">The URL of the page to load.  Cannot be null.</param>
        /// <param name="shortDescrip">A short description that can be used in status updates.  If null, no update will be given.</param>
        /// <param name="caching">Indicator of whether to query the cache for the requested page.</param>
        /// <param name="shouldCache">Indicates whether the result of this page load should be cached.</param>
        /// <param name="suppressNotifications">Indicates whether notification messages should be sent to output.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns an XML document, if it can be loaded.</returns>
        /// <exception cref="ArgumentNullException">If url is null or empty.</exception>
        Task<XDocument?> GetXmlPageAsync(string url, string shortDescrip,
            CachingMode caching, ShouldCache shouldCache,
            SuppressNotifications suppressNotifications, CancellationToken token);

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
        /// <returns>Returns the URL that the response headers say we requested.</returns>
        Task<string> GetRedirectUrlAsync(string url, string? shortDescrip,
            CachingMode caching, ShouldCache shouldCache,
            SuppressNotifications suppressNotifications, CancellationToken token);

        /// <summary>
        /// Have an event that can be watched for status messages.
        /// </summary>
        event EventHandler<MessageEventArgs> StatusChanged;

        /// <summary>
        /// Clear the cache of any previously loaded pages.
        /// </summary>
        void ClearPageCache();
    }
}
