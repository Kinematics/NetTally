using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Adapters;

namespace NetTally.Forums
{
    public class ForumReader
    {
        #region Singleton
        static readonly Lazy<ForumReader> lazy = new Lazy<ForumReader>(() => new ForumReader());

        public static ForumReader Instance => lazy.Value;

        ForumReader()
        {
        }
        #endregion

        /// <summary>
        /// Collects the posts out of a quest based on the quest's configuration.
        /// </summary>
        /// <param name="quest">The quest to read.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Returns a list of posts extracted from the quest.</returns>
        public async Task<List<Task<HtmlDocument>>> ReadQuestAsync(IQuest quest, CancellationToken token)
        {
            IForumAdapter adapter = await GetForumAdapterAsync(quest, token);

            var startInfo = await GetStartInfoAsync(quest, adapter, token);

            var loadedPages = await LoadQuestPagesAsync(quest, adapter, startInfo, token).ConfigureAwait(false);

            return loadedPages;
            //return new List<PostComponents>();
        }

        #region Helper functions
        private async Task<IForumAdapter> GetForumAdapterAsync(IQuest quest, CancellationToken token)
        {
            if (!Uri.IsWellFormedUriString(quest.ThreadName, UriKind.Absolute))
                throw new ArgumentException(nameof(quest));

            Uri uri = new Uri(quest.ThreadName);

            if (quest.ForumType == ForumType.Unknown)
            {
                quest.ForumType = await ForumIdentifier.IdentifyForumTypeAsync(uri, token);
            }

            var adapter = ForumAdapterSelector.GetForumAdapter(quest.ForumType, uri);

            return adapter;
        }

        private async Task<ThreadRangeInfo> GetStartInfoAsync(IQuest quest, IForumAdapter adapter, CancellationToken token)
        {
            IPageProvider pageProvider = ViewModels.ViewModelLocator.MainViewModel.PageProvider;

            var rangeInfo = await adapter.GetStartingPostNumberAsync(quest, pageProvider, token);

            return rangeInfo;
        }

        private async Task<List<Task<HtmlDocument>>> LoadQuestPagesAsync(IQuest quest, IForumAdapter adapter, ThreadRangeInfo threadRangeInfo, CancellationToken token)
        {
            IPageProvider pageProvider = ViewModels.ViewModelLocator.MainViewModel.PageProvider;

            // We will store the loaded pages in a new List.
            List<Task<HtmlDocument>> pages = new List<Task<HtmlDocument>>();

            int firstPageNumber = threadRangeInfo.GetStartPage(quest);

            // Keep track of whether we used threadmarks to figure out the
            // first post.  If we did, we'll re-use this number when filtering
            // for valid posts.
            if (threadRangeInfo.IsThreadmarkSearchResult)
                if (threadRangeInfo.ID > 0)
                    quest.ThreadmarkPost = threadRangeInfo.ID;
                else
                    quest.ThreadmarkPost = -1;
            else
                quest.ThreadmarkPost = 0;

            // Var for what we determine the last page number will be
            int lastPageNumber = 0;
            int pagesToScan = 0;

            if (threadRangeInfo.Pages > 0)
            {
                // If the startInfo obtained the thread pages info, just use that.
                lastPageNumber = threadRangeInfo.Pages;
                pagesToScan = lastPageNumber - firstPageNumber + 1;
            }
            else if (quest.ReadToEndOfThread)
            {
                // If we're reading to the end of the thread (end post 0, or based on a threadmark),
                // then we need to load the first page to find out how many pages there are in the thread.
                // Make sure to bypass the cache, since it may have changed since the last load.

                HtmlDocument firstPage = await pageProvider.GetPage(adapter.GetUrlForPage(firstPageNumber, quest.PostsPerPage),
                    $"Page {firstPageNumber}", CachingMode.BypassCache, token, true).ConfigureAwait(false);

                if (firstPage == null)
                    throw new InvalidOperationException($"Unable to load web page: {adapter.GetUrlForPage(firstPageNumber, quest.PostsPerPage)}");

                pages.Add(Task.FromResult(firstPage));

                ThreadInfo threadInfo = adapter.GetThreadInfo(firstPage);
                lastPageNumber = threadInfo.Pages;

                // Get the number of pages remaining to load
                pagesToScan = lastPageNumber - firstPageNumber;
                // Increment the first page number to fix where we're starting.
                firstPageNumber++;
            }
            else
            {
                // If we're not reading to the end of the thread, just calculate
                // what the last page number will be.  Pages to scan will be the
                // difference in pages +1.
                lastPageNumber = quest.GetPageNumberOf(quest.EndPost);
                pagesToScan = lastPageNumber - firstPageNumber + 1;
            }

            // Initiate the async tasks to load the pages
            if (pagesToScan > 0)
            {
                // Initiate tasks for all pages other than the first page (which we already loaded)
                var results = from pageNum in Enumerable.Range(firstPageNumber, pagesToScan)
                              let pageUrl = adapter.GetUrlForPage(pageNum, quest.PostsPerPage)
                              let shouldCache = (pageNum != lastPageNumber)
                              select pageProvider.GetPage(pageUrl, $"Page {pageNum}", CachingMode.UseCache, token, shouldCache);

                pages.AddRange(results.ToList());
            }

            return pages;
        }
        #endregion
    }
}
