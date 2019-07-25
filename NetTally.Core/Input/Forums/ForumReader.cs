using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ForumReader> logger;

        public ForumReader(IPageProvider provider, ForumAdapterFactory factory, ILoggerFactory loggerFactory)
        {
            pageProvider = provider;
            forumAdapterFactory = factory;
            logger = loggerFactory.CreateLogger<ForumReader>();

            pageProvider.StatusChanged += PageProvider_StatusChanged;
        }

        public void Dispose()
        {
            if (pageProvider != null)
            {
                pageProvider.StatusChanged -= PageProvider_StatusChanged;
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
            logger.LogDebug("Reading quest with ForumReader.");

            IForumAdapter2 adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, token).ConfigureAwait(false);

            logger.LogDebug("Forum adapter created.");

            SyncQuestWithForumAdapter(quest, adapter);

            logger.LogDebug("Quest synced with forum adapter.");

            ThreadRangeInfo rangeInfo = await GetStartInfoAsync(quest, adapter, token).ConfigureAwait(false);

            logger.LogDebug($"Range info acquired. ({rangeInfo.ToString()})");

            List<Task<HtmlDocument?>> loadingPages = await LoadQuestPagesAsync2(quest, adapter, rangeInfo, token).ConfigureAwait(false);

            logger.LogDebug($"Got {loadingPages.Count} pages loading. (v2)");

            var (threadInfo, posts2) = await GetPostsFromPagesAsync2(loadingPages, quest, adapter).ConfigureAwait(false);

            logger.LogDebug($"Got {posts2.Count} posts for quest {threadInfo.Title}. (v2)");

            List<Post> filteredPosts = FilterPosts(posts2, quest, threadInfo, rangeInfo);

            logger.LogDebug($"Filtered to {filteredPosts.Count} posts for quest {threadInfo.Title}. (v2)");

            return (threadInfo.Title, filteredPosts);
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
                quest.PostsPerPage = adapter.GetDefaultPostsPerPage(quest.ThreadUri);

            if (adapter.GetHasRssThreadmarksFeed(quest.ThreadUri) == BoolEx.True && quest.UseRSSThreadmarks == BoolEx.Unknown)
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
            ThreadRangeInfo rangeInfo = await adapter.GetQuestRangeInfoAsync(quest, pageProvider, token).ConfigureAwait(false);

            return rangeInfo;
        }

        /// <summary>
        /// Acquire a list of page loading tasks for the pages that are intended
        /// to be tallied.
        /// </summary>
        /// <param name="quest">The quest for which the tally is being run.</param>
        /// <param name="adapter">The forum adapter that handles the quest's thread.</param>
        /// <param name="threadRangeInfo">The range of posts that are wanted in the tally.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>Returns a list of page loading tasks.</returns>
        private async Task<List<Task<HtmlDocument?>>> LoadQuestPagesAsync2(
            IQuest quest, IForumAdapter2 adapter, ThreadRangeInfo threadRangeInfo, CancellationToken token)
        {
            int firstPageNumber = threadRangeInfo.GetStartPage(quest);

            // Get the first page in order to find out how many pages are in the thread
            // Keep it as a task.
            Task<HtmlDocument?> firstPage = GetFirstPage(firstPageNumber, quest, adapter, token);

            // Get the last page number.
            int lastPageNumber = await GetLastPageNumber(quest, adapter, threadRangeInfo, firstPage).ConfigureAwait(false);

            // Initiate tasks for any remaining pages
            IEnumerable<Task<HtmlDocument?>> remainingPages =
                GetRemainingPages(firstPageNumber, lastPageNumber, quest, adapter, token);

            // Collect all the page load tasks (including the finished first page) to return to caller.
            List<Task<HtmlDocument?>> pagesToLoad = new List<Task<HtmlDocument?>>() { firstPage };
            pagesToLoad.AddRange(remainingPages);

            return pagesToLoad;
        }

        /// <summary>
        /// Gets the first page of the desired tally.
        /// </summary>
        /// <param name="firstPageNumber">The page number of the first page.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="adapter">The forum adapter that handles the quest's thread.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>Returns the thread page that starts the tally.</returns>
        private async Task<HtmlDocument?> GetFirstPage(
            int firstPageNumber, IQuest quest, IForumAdapter2 adapter, CancellationToken token)
        {
            string firstPageUrl = adapter.GetUrlForPage(quest, firstPageNumber);

            // Make sure to bypass the cache, since it may have changed since the last load.
            HtmlDocument? page = await pageProvider.GetHtmlDocumentAsync(
                firstPageUrl, $"Page {firstPageNumber}",
                CachingMode.BypassCache, ShouldCache.Yes, 
                SuppressNotifications.No, token)
                .ConfigureAwait(false);

            return page;
        }

        /// <summary>
        /// Get the last page number of the tally. This may be determined solely
        /// from the thread range info, or might require information from the
        /// provided first page, where we can extract how many pages are in the thread.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="adapter">The forum adapter that handles the quest's thread.</param>
        /// <param name="threadRangeInfo">The range of posts that are wanted in the tally.</param>
        /// <param name="firstPage">The first page of the tally, from which we can get the page range of the thread.</param>
        /// <returns>Returns the last page number of the tally.</returns>
        private async Task<int> GetLastPageNumber(IQuest quest, IForumAdapter2 adapter,
            ThreadRangeInfo threadRangeInfo, Task<HtmlDocument?> firstPage)
        {
            // Check for quick results first.
            if (threadRangeInfo.Pages > 0)
            {
                // If the page range has already been determined, use that.
                return threadRangeInfo.Pages;
            }
            else if (!quest.ReadToEndOfThread && !threadRangeInfo.IsThreadmarkSearchResult)
            {
                // If we're not reading to the end of the thread, just calculate
                // what the last page number will be.  Pages to scan will be the
                // difference in pages +1.
                return ThreadInfo.GetPageNumberOfPost(quest.EndPost, quest);
            }

            // If we're reading to the end of the thread (end post 0, or based on a threadmark),
            // then we need to load the first page to find out how many pages there are in the thread.
            var page = await firstPage.ConfigureAwait(false);

            if (page == null)
                throw new InvalidOperationException($"Unable to load first page of {quest.ThreadName}");

            return adapter.GetThreadInfo(page).Pages;
        }

        /// <summary>
        /// Gets a collection of all pages that need to be loaded for the tally,
        /// other than the first page (which was already loaded).
        /// </summary>
        /// <param name="firstPageNumber">The first page number of the tally.</param>
        /// <param name="lastPageNumber">The last page number of the tally.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="adapter">The forum adapter that handles the quest's thread.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a collection of pages being loaded.</returns>
        private IEnumerable<Task<HtmlDocument?>> GetRemainingPages(
            int firstPageNumber, int lastPageNumber,
            IQuest quest, IForumAdapter2 adapter,
            CancellationToken token)
        {
            if (lastPageNumber <= firstPageNumber)
                yield break;

            for (int pageNum = firstPageNumber + 1; pageNum <= lastPageNumber; pageNum++)
            {
                var pageUrl = adapter.GetUrlForPage(quest, pageNum);
                var shouldCache = (pageNum == lastPageNumber) ? ShouldCache.No : ShouldCache.Yes;

                yield return pageProvider.GetHtmlDocumentAsync(
                                  pageUrl, $"Page {pageNum}",
                                  CachingMode.UseCache, shouldCache,
                                  SuppressNotifications.No, token);
            }
        }

        /// <summary>
        /// Gets all posts from the provided pages list.
        /// </summary>
        /// <param name="loadingPages">The pages that are being loaded for the tally.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="adapter">The forum adapter that handles the quest's thread.</param>
        /// <returns>Returns all posts extracted from all pages provided,
        /// and the thread title.</returns>
        private async Task<(ThreadInfo threadInfo, List<Post> posts)> GetPostsFromPagesAsync2(
            List<Task<HtmlDocument?>> loadingPages,
            IQuest quest, IForumAdapter2 adapter)
        {
            ThreadInfo? threadInfo = null;
            List<Post> postsList = new List<Post>();

            foreach (var loadingPage in loadingPages)
            {
                var page = await loadingPage.ConfigureAwait(false);

                if (page == null)
                    continue;

                if (threadInfo == null)
                {
                    threadInfo = adapter.GetThreadInfo(page);
                }

                postsList.AddRange(adapter.GetPosts(page, quest));
            }

            if (threadInfo == null)
                threadInfo = new ThreadInfo("Unknown", "Unknown", 0);

            return (threadInfo, postsList);
        }

        /// <summary>
        /// Run the provided post list through the various filters, as set
        /// by quest options and post numbers, etc.
        /// </summary>
        /// <param name="postsList">The list of posts to filter.</param>
        /// <param name="quest">The quest with relevant options.</param>
        /// <param name="threadInfo">Thread info provides the thread author.</param>
        /// <param name="rangeInfo">Range info provides information on the range of valid posts.</param>
        /// <returns>Returns a list of posts that satisfy the filtering criteria.</returns>
        private List<Post> FilterPosts(List<Post> postsList,
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

        /// <summary>
        /// Check whether the given post is after the startpoint of the tally.
        /// </summary>
        /// <param name="post">The post to check.</param>
        /// <param name="rangeInfo">The range which shows where the tally starts.</param>
        /// <returns>Returns true if the post comes after the start of the tally.</returns>
        private bool PostIsAfterStart(Post post, ThreadRangeInfo rangeInfo)
        {
            return (rangeInfo.ByNumber && post.Origin.ThreadPostNumber >= rangeInfo.Number) || (!rangeInfo.ByNumber && post.Origin.ID > rangeInfo.ID);
        }

        /// <summary>
        /// Check whether the given post is before the endpoint of the tally.
        /// </summary>
        /// <param name="post">The post to check.</param>
        /// <param name="quest">Quest options.</param>
        /// <param name="rangeInfo">Specific range information.</param>
        /// <returns>Returns true if the post comes before the end of the tally.</returns>
        private bool PostIsBeforeEnd(Post post, IQuest quest, ThreadRangeInfo rangeInfo)
        {
            return (quest.ReadToEndOfThread || rangeInfo.IsThreadmarkSearchResult || post.Origin.ThreadPostNumber <= quest.EndPost);
        }
        #endregion
    }
}
