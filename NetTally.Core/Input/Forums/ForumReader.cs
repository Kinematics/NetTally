using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.CustomEventArgs;
using NetTally.Web;

namespace NetTally.Forums
{
    public class ForumReader : IDisposable
    {
        #region Constructor
        readonly IPageProvider pageProvider;

        public ForumReader(IPageProvider provider)
        {
            pageProvider = provider;

            pageProvider.StatusChanged += PageProvider_StatusChanged;
        }

        public void Dispose()
        {
            if (pageProvider != null)
            {
                pageProvider.StatusChanged -= PageProvider_StatusChanged;
                pageProvider.Dispose();
            }
        }
        #endregion

        #region Event passing
        private void PageProvider_StatusChanged(object sender, MessageEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Message))
            {
                StatusChanged?.Invoke(sender, e);
            }
        }

        /// <summary>
        /// Event handler hook for status messages.
        /// </summary>
        public event EventHandler<MessageEventArgs> StatusChanged;
        #endregion

        #region Public method
        /// <summary>
        /// Collects the posts out of a quest based on the quest's configuration.
        /// </summary>
        /// <param name="quest">The quest to read.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a list of posts extracted from the quest.</returns>
        public async Task<(string threadTitle, List<PostComponents> posts)> ReadQuestAsync(IQuest quest, CancellationToken token)
        {
            IForumAdapter adapter = await GetForumAdapterAsync(quest, token).ConfigureAwait(false);

            if (adapter == null)
                throw new InvalidOperationException("Unable to acquire forum adapter for the quest.");

            ThreadRangeInfo rangeInfo = await GetStartInfoAsync(quest, adapter, token).ConfigureAwait(false);

            if (rangeInfo == null)
                throw new InvalidOperationException("Unable to determine post range for the quest.");

            List<Task<HtmlDocument>> loadedPages = await LoadQuestPagesAsync(quest, adapter, rangeInfo, token).ConfigureAwait(false);

            if (loadedPages == null)
                throw new InvalidOperationException("Unable to load pages for the quest.");

            var (title, posts) = await GetPostsFromPagesAsync(quest, adapter, rangeInfo, loadedPages, token).ConfigureAwait(false);

            if (posts == null)
                throw new InvalidOperationException("Unable to extract posts from quest pages.");

            return (title, posts);
        }
        #endregion

        #region Helper functions
        /// <summary>
        /// Get the forum adapter for the provided quest.
        /// </summary>
        /// <param name="quest">The quest to get a forum adapter for.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns the forum adapter that knows how to read the forum of the quest thread.</returns>
        private async Task<IForumAdapter> GetForumAdapterAsync(IQuest quest, CancellationToken token)
        {

            var adapter = await ForumAdapterFactory.GetForumAdapterAsync(quest.ThreadUri, pageProvider, token);

            if (quest.PostsPerPage == 0)
                quest.PostsPerPage = adapter.DefaultPostsPerPage;

            quest.LineBreak = adapter.LineBreak;

            quest.PermalinkForId = adapter.GetPermalinkForId;

            if (adapter.HasRSSThreadmarks == BoolEx.True && quest.UseRSSThreadmarks == BoolEx.Unknown)
                quest.UseRSSThreadmarks = BoolEx.True;

            return adapter;
        }

        /// <summary>
        /// Gets the thread range info (page and post numbers) based on the quest configuration.
        /// May load pages (such as for checking threadmarks), so will use the ViewModel's page provider.
        /// </summary>
        /// <param name="quest">The quest we're getting thread info for.</param>
        /// <param name="adapter">The quest's forum adapter.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns the quest's thread range info.</returns>
        private async Task<ThreadRangeInfo> GetStartInfoAsync(IQuest quest, IForumAdapter adapter, CancellationToken token)
        {
            ThreadRangeInfo rangeInfo = await adapter.GetStartingPostNumberAsync(quest, pageProvider, token).ConfigureAwait(false);

            return rangeInfo;
        }

        /// <summary>
        /// Loads the HTML pages that are relevant to a quest's tally.
        /// </summary>
        /// <param name="quest">The quest being loaded.</param>
        /// <param name="adapter">The quest's forum adapter, used to forum the URLs to load.</param>
        /// <param name="threadRangeInfo">The range info that determines which pages to load.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a list of tasks that are handling the async loading of the requested pages.</returns>
        private async Task<List<Task<HtmlDocument>>> LoadQuestPagesAsync(
            IQuest quest, IForumAdapter adapter, ThreadRangeInfo threadRangeInfo, CancellationToken token)
        {
            var (firstPageNumber, lastPageNumber, pagesToScan) = await GetPagesToScanAsync(quest, adapter, threadRangeInfo, token).ConfigureAwait(false);

            // We will store the loaded pages in a new List.
            List<Task<HtmlDocument>> pages = new List<Task<HtmlDocument>>();

            // Initiate the async tasks to load the pages
            if (pagesToScan > 0)
            {
                // Initiate tasks for all pages other than the first page (which we already loaded)
                var results = from pageNum in Enumerable.Range(firstPageNumber, pagesToScan)
                              let pageUrl = adapter.GetUrlForPage(pageNum, quest.PostsPerPage)
                              let shouldCache = (pageNum == lastPageNumber) ? ShouldCache.No : ShouldCache.Yes
                              select pageProvider.GetHtmlDocumentAsync(
                                  pageUrl, $"Page {pageNum}", CachingMode.UseCache, shouldCache, SuppressNotifications.No, token);

                pages.AddRange(results.ToList());
            }

            return pages;
        }

        /// <summary>
        /// Determines the page number range that will be loaded for the quest.
        /// Returns a tuple of first page number, last page number, and pages to scan.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="adapter">The forum adapter for the quest.</param>
        /// <param name="threadRangeInfo">The thread range info, as provided by the adapter.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a tuple of the page number info that was determined.</returns>
        private async Task<(int firstPageNumber, int lastPageNumber, int pagesToScan)> GetPagesToScanAsync(
            IQuest quest, IForumAdapter adapter, ThreadRangeInfo threadRangeInfo, CancellationToken token)
        {
            int firstPageNumber = threadRangeInfo.GetStartPage(quest);
            int lastPageNumber = 0;
            int pagesToScan = 0;

            if (threadRangeInfo.Pages > 0)
            {
                // If the startInfo obtained the thread pages info, just use that.
                lastPageNumber = threadRangeInfo.Pages;
            }
            else if (quest.ReadToEndOfThread || threadRangeInfo.IsThreadmarkSearchResult)
            {
                // If we're reading to the end of the thread (end post 0, or based on a threadmark),
                // then we need to load the first page to find out how many pages there are in the thread.
                // Make sure to bypass the cache, since it may have changed since the last load.

                string firstPageUrl = adapter.GetUrlForPage(firstPageNumber, quest.PostsPerPage);

                HtmlDocument? page = await pageProvider.GetHtmlDocumentAsync(firstPageUrl, $"Page {firstPageNumber}",
                    CachingMode.BypassCache, ShouldCache.Yes, SuppressNotifications.No, token)
                    .ConfigureAwait(false);

                if (page == null)
                    throw new InvalidOperationException($"Unable to load web page: {firstPageUrl}");

                lastPageNumber = adapter.GetThreadInfo(page).Pages;
            }
            else
            {
                // If we're not reading to the end of the thread, just calculate
                // what the last page number will be.  Pages to scan will be the
                // difference in pages +1.
                lastPageNumber = ThreadInfo.GetPageNumberOfPost(quest.EndPost, quest);
            }

            pagesToScan = lastPageNumber - firstPageNumber + 1;

            return (firstPageNumber, lastPageNumber, pagesToScan);
        }

        /// <summary>
        /// Gets a list of posts from the provided pages from a quest.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="adapter">The quest's forum adapter.</param>
        /// <param name="rangeInfo">The thread range info for the tally.</param>
        /// <param name="pages">The pages that are being loaded.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a list of PostComponents comprising the posts from the threads that fall within the specified range.</returns>
        private async Task<(string threadTitle, List<PostComponents> posts)> GetPostsFromPagesAsync(
            IQuest quest, IForumAdapter adapter, ThreadRangeInfo rangeInfo, List<Task<HtmlDocument>> pages, CancellationToken token)
        {
            List<PostComponents> postsList = new List<PostComponents>();

            if (pages.Count == 0)
                return (string.Empty, postsList);

            var firstPageTask = pages.First();

            while (pages.Any())
            {
                var finishedPage = await Task.WhenAny(pages).ConfigureAwait(false);
                pages.Remove(finishedPage);

                if (finishedPage.IsCanceled)
                {
                    throw new OperationCanceledException();
                }

                // This will throw any pending exceptions that occurred while trying to load the page.
                // This removes the need to check for finishedPage.IsFaulted.
                var page = await finishedPage.ConfigureAwait(false);

                if (page == null)
                {
                    Exception ae = new Exception("Not all pages loaded.  Rerun tally.");
                    ae.Data["Application"] = true;
                    throw ae;
                }

                var posts = from post in adapter.GetPosts(page, quest)
                            where post.IsVote && post.IsAfterStart(rangeInfo) &&
                                (quest.ReadToEndOfThread || rangeInfo.IsThreadmarkSearchResult || post.Number <= quest.EndPost)
                            select post;

                postsList.AddRange(posts);
            }

            var firstPage = firstPageTask.Result;

            ThreadInfo threadInfo = adapter.GetThreadInfo(firstPage);

            // Get all posts that are not filtered out, either explicitly, or (for the thread author) implicity.
            postsList = postsList
                .Where(p =>
                        ((quest.UseCustomUsernameFilters && !quest.UsernameFilter.Match(p.Author)) || (!quest.UseCustomUsernameFilters && p.Author != threadInfo.Author))
                     && (!quest.UseCustomPostFilters || !(quest.PostsToFilter.Contains(p.Number) || quest.PostsToFilter.Contains(p.IDValue)))
                      )
                .Distinct().OrderBy(p => p.Number).ToList();

            return (threadInfo.Title, postsList);
        }
        #endregion
    }
}
