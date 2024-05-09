using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Configure;
using NetTally.CustomEventArgs;
using NetTally.Data;
using NetTally.Enums;
using NetTally.Forums;
using NetTally.Forums.ForumAdapters;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Threads;
using NetTally.Web;

namespace NetTally.Input.Forums;
public class ForumReaderF(
    IServiceProvider serviceProvider,
    ForumAdapterFactory forumAdapterFactory,
    IQuestsInfo questsInfo,
    ILogger<ForumReaderF> logger)
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<ForumReaderF> logger = logger;
    private readonly IQuestsInfo questsInfo = questsInfo;
    private readonly ForumAdapterFactory forumAdapterFactory = forumAdapterFactory;

    #region Event passing
    private void PageProvider_StatusChanged(object? sender, MessageEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Message))
        {
            StatusChanged?.Invoke(sender, e);
        }
    }

    private void SendNotificationMessage(string message)
    {
        var e = new MessageEventArgs(message);
        StatusChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Event handler hook for status messages.
    /// </summary>
    public event EventHandler<MessageEventArgs>? StatusChanged;
    #endregion

    #region Basic Flow
    /// <summary>
    /// Read posts from a provided quest. Include all associated linked quests.
    /// </summary>
    /// <param name="quest">The quest to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of titles (one per quest) and the list of posts to be processed.</returns>
    public async Task<(List<string> Titles, List<PostType> Posts)>
        ReadQuestAsync(Quest quest, CancellationToken cancellationToken)
    {
        List<Quest> questsToRead = [quest, .. questsInfo.GetLinkedQuests(quest)];

        logger.LogDebug("Reading quest {questName} with any linked quests. Total quests: {total}",
            quest.DisplayName, questsToRead.Count);

        // Load all posts from the base quest and all linked quests.
        var results = await Task.WhenAll(
            questsToRead.Select(q => GetPostsFromQuestAsync(q, cancellationToken)))
            .ConfigureAwait(false);

        var allTitles = results.Select(q => q.Title).ToList();
        var allPosts = results.SelectMany(q => q.Posts).ToList();

        if (allTitles.Any(t => t == StringData.Error))
        {
            return ([StringData.Error], []);
        }

        return (allTitles, allPosts);
    }

    /// <summary>
    /// Read posts from a specific quest.
    /// </summary>
    /// <param name="quest">The quest to read.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A title describing the quest and posts, plus all the valid posts found.</returns>
    private async Task<(string Title, List<PostType> Posts)>
        GetPostsFromQuestAsync(Quest quest, CancellationToken token)
    {
        logger.LogDebug("Reading posts from quest {questDisplayName} with ForumReader.",
            quest.DisplayName);

        using var pageProvider = serviceProvider.GetRequiredService<IPageProvider>();

        try
        {
            pageProvider.StatusChanged += PageProvider_StatusChanged;

            IForumAdapter adapter = await GetForumAdapter(quest, token)
                .ConfigureAwait(false);

            return await GetPostsAsync(quest, pageProvider, adapter, token)
                .ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error loading quest from page provider for quest {questName}.",
                quest.DisplayName);
            return (StringData.Error, []);
        }
    }

    /// <summary>
    /// Get the posts from a specific quest.
    /// </summary>
    /// <param name="quest">The quest to read.</param>
    /// <param name="pageProvider">The page provider for loading pages.</param>
    /// <param name="adapter">The forum adapter for processing pages according to
    /// that forum's needs.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A title describing the quest and posts, plus all the valid posts found.</returns>
    private async Task<(string Title, List<PostType> Posts)> GetPostsAsync(
        Quest quest,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        // Get information about the thread, and which pages need to be loaded.
        //(ThreadRangeType rangeInfo, ThreadInfoType threadInfo)
        var threadData = await GetThreadInfoAsync(quest, pageProvider, adapter, token)
            .ConfigureAwait(false);

        if (threadData == null)
        {
            return (StringData.Error, []);
        }

        var (rangeInfo, threadInfo) = threadData.Value;

        logger.LogDebug("Range info acquired for {questDisplayName}. ({rangeInfo})",
            quest.DisplayName, rangeInfo);

        List<HtmlDocument?> pagesN =
            await ReadPagesFromQuestAsync(quest, rangeInfo, pageProvider, adapter, token)
            .ConfigureAwait(false);

        logger.LogDebug("Got {Count} pages loading {questDisplayName}.", pagesN.Count, quest.DisplayName);

        if (pagesN.Any(p => p == null))
        {
            SendNotificationMessage("Unable to load all pages.");
            return (StringData.Error, []);
        }

        var pages = pagesN.Select(p => p!).ToList();

        var posts = GetPostsFromPages(quest, pages, rangeInfo, threadInfo, adapter);

        logger.LogDebug("Got {Count} posts for quest {questDisplayName}.", posts.Count, quest.DisplayName);

        return (threadInfo.Title, posts);
    }

    /// <summary>
    /// Processes the provided pages to extract posts.
    /// </summary>
    /// <param name="pages">All pages loaded for the quest.</param>
    /// <param name="rangeInfo">The range of posts we want to extract.</param>
    /// <param name="threadInfo">Information about the thread.</param>
    /// <param name="adapter">The forum adapter that can transform this forum's
    /// HTML into posts we can understand.</param>
    /// <param name="quest">The quest being read.</param>
    /// <returns>A list of valid posts found.</returns>
    private List<PostType> GetPostsFromPages(
        Quest quest,
        List<HtmlDocument> pages,
        ThreadRangeType rangeInfo,
        ThreadInfoType threadInfo,
        IForumAdapter adapter)
    {
        int startPage = ThreadRange.GetStartPage(rangeInfo, quest);

        var posts = pages
            .SelectMany((p, i) => adapter.GetPostsF(p, quest, startPage + i))
            .Where(p => KeepPost(p, quest, rangeInfo, threadInfo))
            .DistinctBy(p => p.Origin, OriginComparer.Instance) // remove sticky posts
            .OrderBy(p => p.Origin.ThreadPostNumber)
            .ToList();

        return posts;
    }
    #endregion Basic Flow

    #region Keep Post Filtering
    private bool KeepPost(
        PostType post,
        Quest quest,
        ThreadRangeType rangeInfo,
        ThreadInfoType threadInfo)
    {
        if (!post.HasVote)
            return false;

        if (PostIsBeforeStart(post, rangeInfo) || PostIsAfterEnd(post, quest, rangeInfo))
            return false;

        if (post.Origin.Author == threadInfo.Author)
            return false;

        if (AuthorComparer.Instance.Equals(post.Origin.Author, threadInfo.Author))
        {
            logger.LogWarning("Author compare failed but AuthorComparer passed for [{postAuthor}] vs [{threadAuthor}]",
                post.Origin.Author, threadInfo.Author);
            return false;
        }

        if (PostMatchesUsernameFilter(post, quest))
            return false;

        if (PostMatchesPostNumberFilter(post, quest))
            return false;

        return true;
    }

    private static bool PostIsBeforeStart(PostType post, ThreadRangeType rangeInfo)
    {
        if (rangeInfo.RangeType == ThreadRangeRangeType.ByPostNumber)
            return post.Origin.ThreadPostNumber < rangeInfo.StartPostNumber;

        return post.Origin.PostId.Id < rangeInfo.PostId.Id;
    }

    private static bool PostIsAfterEnd(PostType post, Quest quest, ThreadRangeType rangeInfo)
    {
        if (quest.ReadToEndOfThread || rangeInfo.RangeType == ThreadRangeRangeType.ByPostId)
            return false;

        return post.Origin.ThreadPostNumber > quest.EndPost;
    }

    private static bool PostMatchesUsernameFilter(PostType post, Quest quest)
    {
        return quest.UseCustomUsernameFilters && quest.UsernameFilter.Match(post.Origin.Author.Name);
    }

    private static bool PostMatchesPostNumberFilter(PostType post, Quest quest)
    {
        return quest.UseCustomPostFilters &&
            (quest.PostsToFilter.Contains(post.Origin.ThreadPostNumber) ||
            quest.PostsToFilter.Contains(post.Origin.PostId.Id));
    }
    #endregion Keep Post Filtering

    #region Forum Adapter Setup
    private async Task<IForumAdapter> GetForumAdapter(Quest quest, CancellationToken token)
    {
        IForumAdapter adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, token)
            .ConfigureAwait(false);

        SyncQuestWithForumAdapter(quest, adapter);

        logger.LogDebug("Quest {questDisplayName} synced with forum adapter.", quest.DisplayName);

        return adapter;
    }

    /// <summary>
    /// Update the quest with information from the forum adapter.
    /// </summary>
    /// <param name="quest">The quest to sync up.</param>
    /// <param name="adapter">The forum adapter created for the quest.</param>
    private static void SyncQuestWithForumAdapter(Quest quest, IForumAdapter adapter)
    {
        if (quest.PostsPerPage == 0)
            quest.PostsPerPage = adapter.GetDefaultPostsPerPage(quest.ThreadUri);

        if (adapter.HasRssThreadmarksFeed(quest.ThreadUri) == BoolEx.True &&
            quest.UseRSSThreadmarks == BoolEx.Unknown)
            quest.UseRSSThreadmarks = BoolEx.True;
    }
    #endregion Forum Adapter Setup

    #region Page Loading
    /// <summary>
    /// Get information about the thread being read.
    /// </summary>
    /// <param name="quest">The quest</param>
    /// <param name="pageProvider">The page provider to read web pages.</param>
    /// <param name="adapter">The forum adapter for the quest.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A tuple of range information about the thread, and
    /// title and author information about the thread.</returns>
    private static async Task<(ThreadRangeType, ThreadInfoType)?> GetThreadInfoAsync(
        Quest quest,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        string infoPageUrl = adapter.GetUrlForPage(quest, 1);
        var page = await GetQuestInfoPageAsync(infoPageUrl, pageProvider, token);

        if (page == null)
            return null;

        ThreadRangeType range = adapter.GetQuestRangeInfoF(quest, page);
        ThreadInfoType thread = adapter.GetThreadInfoF(quest, page);

        return (range, thread);
    }

    /// <summary>
    /// Loads a specific page that will be used to gather information about the thread.
    /// </summary>
    /// <param name="pageUrl">The URL to load.</param>
    /// <param name="pageProvider">The page provider to read web pages.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The requested page, or null if reading failed.</returns>
    private static async Task<HtmlDocument?> GetQuestInfoPageAsync(
        string pageUrl,
        IPageProvider pageProvider,
        CancellationToken token)
    {
        // Make sure to bypass the cache, since it may have changed since the last load.
        HtmlDocument? page = await pageProvider.GetHtmlDocumentAsync(
            pageUrl, "Info Page",
            CachingMode.BypassCache, ShouldCache.Yes,
            SuppressNotifications.Yes, token)
            .ConfigureAwait(false);

        return page;
    }

    /// <summary>
    /// Read all the relevant pages from the quest, based on the provided
    /// thread range information.
    /// </summary>
    /// <param name="quest">The quest</param>
    /// <param name="threadRange">Thread range data</param>
    /// <param name="pageProvider">The page provider to read web pages.</param>
    /// <param name="adapter">The forum adapter for the quest.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A list of all loaded documents.</returns>
    private static async Task<List<HtmlDocument?>> ReadPagesFromQuestAsync(
        Quest quest,
        ThreadRangeType threadRange,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        int firstPage = ThreadRange.GetStartPage(threadRange, quest);
        int lastPage = ThreadRange.GetEndPage(threadRange, quest);
        int pageCount = lastPage - firstPage + 1;

        if (pageCount < 1)
            return [];

        List<Task<HtmlDocument?>> tasks = [];

        for (int pageNum = firstPage; pageNum <= lastPage; pageNum++)
        {
            var pageUrl = adapter.GetUrlForPage(quest, pageNum);
            var shouldCache = (pageNum == lastPage) ? ShouldCache.No : ShouldCache.Yes;

            tasks.Add(pageProvider.GetHtmlDocumentAsync(
                              pageUrl, $"Page {pageNum}",
                              CachingMode.UseCache, shouldCache,
                              SuppressNotifications.No, token));
        }

        var finished = await Task.WhenAll(tasks).ConfigureAwait(false);

        return finished.Where(d => d != null)
            .ToList();
    }
    #endregion Page Loading
}
