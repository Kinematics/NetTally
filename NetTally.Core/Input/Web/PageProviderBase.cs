using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using NetTally.Cache;
using NetTally.CustomEventArgs;
using NetTally.SystemInfo;

namespace NetTally.Web
{
    public abstract class PageProviderBase : IDisposable
    {
        #region Fields
        // Maximum number of simultaneous connections allowed, to guard against hammering the server.
        // Setting it to 5 or higher causes it to hang for several seconds on the last page when
        // loading SB and SV pages.
        protected const int maxSimultaneousConnections = 4;
        protected readonly SemaphoreSlim ss = new SemaphoreSlim(maxSimultaneousConnections);
        #endregion

        #region Properties
        protected HttpClientHandler ClientHandler { get; }
        protected IClock Clock { get; }
        protected ICache<string> Cache { get; }

        protected string UserAgent { get; } = $"{ProductInfo.Name} ({ProductInfo.Version})";
        #endregion

        #region Constructors
        protected PageProviderBase(HttpClientHandler handler, ICache<string> pageCache, IClock clock)
        {
            ClientHandler = handler;
            Cache = pageCache;
            Clock = clock;

            Cache.SetClock(Clock);
        }
        #endregion

        #region Disposal
        protected bool _disposed;

        ~PageProviderBase()
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
                ClientHandler?.Dispose();
                ss.Dispose();
            }

            _disposed = true;
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
            Cache.InvalidateCache();
        }

        /// <summary>
        /// Event handler hook for status messages.
        /// </summary>
        public event EventHandler<MessageEventArgs> StatusChanged;

        /// <summary>
        /// Function to raise events when page load status has been updated.
        /// </summary>
        /// <param name="message">The message to send to any listeners.</param>
        protected void OnStatusChanged(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            StatusChanged?.Invoke(this, new MessageEventArgs(message));
        }
        #endregion

        #region Notification functions

        protected void NotifyStatusChange(PageRequestStatusType status, string url, string? shortDescrip, Exception? e, SuppressNotifications suppressNotifications)
        {
            if (suppressNotifications == SuppressNotifications.Yes)
                return;

            if (status == PageRequestStatusType.Requested)
            {
                NotifyRequest(url);
                return;
            }

            if (status == PageRequestStatusType.Cancelled)
            {
                NotifyCancel();
                return;
            }

            if (status == PageRequestStatusType.Error)
            {
                NotifyError(shortDescrip, e);
                return;
            }

            NotifyResult(status, shortDescrip);
        }


        /// <summary>
        /// Send status update when requesting a page URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="suppress">if set to <c>true</c> [suppress].</param>
        private void NotifyRequest(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            OnStatusChanged($"{url}\n");
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
        private void NotifyError(string? shortDescrip, Exception? e)
        {
            if (string.IsNullOrEmpty(shortDescrip))
                return;

            OnStatusChanged($"{shortDescrip}: {e?.Message ?? "(unknown error)"}\n");
        }

        /// <summary>
        /// Sends a status update for a load attempt result.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="shortDescrip">The short descrip.</param>
        /// <param name="suppress">if set to <c>true</c> [suppress].</param>
        private void NotifyResult(PageRequestStatusType status, string? shortDescrip)
        {
            if (string.IsNullOrEmpty(shortDescrip))
                return;

            StringBuilder sb = new StringBuilder();

            switch (status)
            {
                case PageRequestStatusType.Retry:
                    sb.Append("Retrying: ");
                    sb.Append(shortDescrip);
                    break;
                case PageRequestStatusType.Failed:
                    sb.Append("Failed to load: ");
                    sb.Append(shortDescrip);
                    break;
                case PageRequestStatusType.LoadedFromCache:
                    sb.Append(shortDescrip);
                    sb.Append(" loaded from memory!");
                    break;
                case PageRequestStatusType.Loaded:
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
