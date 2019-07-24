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
    /// <summary>
    /// Class for handling reading forum posts from a quest's forum.
    /// </summary>
    class ForumReader : IDisposable
    {
        #region Constructor
        readonly IPageProvider pageProvider;
        readonly ForumAdapterFactory forumAdapterFactory;

        public ForumReader(IPageProvider provider, ForumAdapterFactory factory)
        {
            pageProvider = provider;
            forumAdapterFactory = factory;

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
        public async Task<(string threadTitle, List<Post> posts)> ReadQuestAsync(IQuest quest, CancellationToken token)
        {
            IForumAdapter2 adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, token).ConfigureAwait(false);

            SyncQuestWithForumAdapter(quest, adapter);

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
        /// Update the quest with information from the forum adapter.
        /// </summary>
        /// <param name="quest">The quest to sync up.</param>
        /// <param name="adapter">The forum adapter created for the quest.</param>
        private void SyncQuestWithForumAdapter(IQuest quest, IForumAdapter2 adapter)
        {
            if (quest.PostsPerPage == 0)
                quest.PostsPerPage = adapter.GetDefaultPostsPerPage(quest.ThreadUri!);

            if (adapter.GetHasRssThreadmarksFeed(quest.ThreadUri!) == BoolEx.True && quest.UseRSSThreadmarks == BoolEx.Unknown)
                quest.UseRSSThreadmarks = BoolEx.True;
        }

        /// <summary>
        /// Gets the thread range info (page and post numbers) based on the quest configuration.
        /// May load pages (such as for checking threadmarks), so will use the ViewModel's page provider.
        /// </summary>
        /// <param name="quest">The quest we're getting thread info for.</param>
        /// <param name="adapter">The quest's forum adapter.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns the quest's thread range info.</returns>
        private async Task<ThreadRangeInfo> GetStartInfoAsync(IQuest quest, IForumAdapter2 adapter, CancellationToken token)
        {
            ThreadRangeInfo rangeInfo = await adapter.GetQuestRangeInfo(quest, pageProvider, token).ConfigureAwait(false);

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
            IQuest quest, IForumAdapter2 adapter, ThreadRangeInfo threadRangeInfo, CancellationToken token)
        {
            var (firstPageNumber, lastPageNumber, pagesToScan) = await GetPagesToScanAsync(quest, adapter, threadRangeInfo, token).ConfigureAwait(false);

            // We will store the loaded pages in a new List.
            List<Task<HtmlDocument>> pages = new List<Task<HtmlDocument>>();

            // Initiate the async tasks to load the pages
            if (pagesToScan > 0)
            {
                // Initiate tasks for all pages other than the first page (which we already loaded)
                var results = from pageNum in Enumerable.Range(firstPageNumber, pagesToScan)
                              let pageUrl = adapter.GetUrlForPage(quest.ThreadUri!, pageNum)
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
            IQuest quest, IForumAdapter2 adapter, ThreadRangeInfo threadRangeInfo, CancellationToken token)
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

                string firstPageUrl = adapter.GetUrlForPage(quest.ThreadUri!, firstPageNumber);

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
        /// <returns>Returns a list of Post comprising the posts from the threads that fall within the specified range.</returns>
        private async Task<(string threadTitle, List<Post> posts)> GetPostsFromPagesAsync(
            IQuest quest, IForumAdapter2 adapter, ThreadRangeInfo rangeInfo, List<Task<HtmlDocument>> pages, CancellationToken token)
        {
            List<Post> postsList = new List<Post>();

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
                    Exception e = new Exception("Not all pages loaded.  Rerun tally.");
                    e.Data["Notify"] = true;
                    throw e;
                }

                postsList.AddRange(adapter.GetPosts(page, quest));
            }

            var firstPage = firstPageTask.Result;
            ThreadInfo threadInfo = adapter.GetThreadInfo(firstPage);

            postsList = FilteredPosts(postsList, quest, threadInfo, rangeInfo);

            return (threadInfo.Title, postsList);
        }

        private List<Post> FilteredPosts(List<Post> postsList,
            IQuest quest, ThreadInfo threadInfo, ThreadRangeInfo rangeInfo)
        {
            // Remove any posts that are not votes, that aren't in the valid post range, or that
            // hit any filters the quest has set up.  Then do a grouping to get distinct results.
            var filtered = from post in postsList
                           where post.HasVote
                                && (PostIsAfterStart(post, rangeInfo) && PostIsBeforeEnd(post, quest, rangeInfo))
                                && ((quest.UseCustomUsernameFilters && !quest.UsernameFilter.Match(post.Origin.Author))
                                    || (!quest.UseCustomUsernameFilters && post.Origin.Author != threadInfo.Author))
                                && (!quest.UseCustomPostFilters 
                                    || !(quest.PostsToFilter.Contains(post.Origin.ThreadPostNumber) 
                                    || quest.PostsToFilter.Contains(post.Origin.ID.Value)))
                           group post by post.Origin.ThreadPostNumber into postNumGroup // Group to deal with sticky posts that should only be processed once.
                           orderby postNumGroup.Key
                           select postNumGroup.First();

            return filtered.ToList();
        }

        private bool PostIsAfterStart(Post post, ThreadRangeInfo rangeInfo)
        {
            return (rangeInfo.ByNumber && post.Origin.ThreadPostNumber >= rangeInfo.Number) || (!rangeInfo.ByNumber && post.Origin.ID > rangeInfo.ID);
        }

        private bool PostIsBeforeEnd(Post post, IQuest quest, ThreadRangeInfo rangeInfo)
        {
            return (quest.ReadToEndOfThread || rangeInfo.IsThreadmarkSearchResult || post.Origin.ThreadPostNumber <= quest.EndPost);
        }
        #endregion
    }
}
