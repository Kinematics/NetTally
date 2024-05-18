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
using NetTally.Enums;
using NetTally.Input.Forums.ForumAdaptersF;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Threads;
using NetTally.Utility;
using NetTally.Web;

namespace NetTally.Input.Forums.ReadingF;
public class ForumReader(
    IServiceProvider serviceProvider,
    ForumAdapterFactory forumAdapterFactory,
    IQuestsInfo questsInfo,
    ILogger<ForumReader> logger) : IForumReader
{
    private readonly IServiceProvider serviceProvider = serviceProvider;
    private readonly ILogger<ForumReader> logger = logger;
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
    public async Task<(IEnumerable<string> Titles, IEnumerable<PostType> Posts)>
        ReadQuestAsync(Quest quest, CancellationToken cancellationToken)
    {
        List<Quest> questsToRead = [quest, .. questsInfo.GetLinkedQuests(quest)];

        logger.LogDebug("Reading quest {questName} with any linked quests. Total quests: {total}",
            quest.DisplayName, questsToRead.Count);

        // Load all posts from the base quest and all linked quests.
        var results = await Task.WhenAll(
                questsToRead.Select(q => GetPostsFromQuestAsync(q, cancellationToken)))
            .ConfigureAwait(false);

        var allTitles = results.Select(q => q.Title);
        var allPosts = results.SelectMany(q => q.Posts);

        if (allTitles.Any(t => t == Strings.Error))
        {
            throw new Exception("Unable to load all pages.");
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
            return (Strings.Error, []);
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

        if (threadData != null)
        {
            var pages = await ReadPagesFromQuestAsync(quest, threadData, pageProvider, adapter, token)
                                .ConfigureAwait(false);

            if (pages.All(p => p != null))
            {
                var posts = GetPostsFromPages(quest, pages, threadData, adapter);

                return (threadData.Title, posts);
            }
        }

        return (Strings.Error, []);
    }

    /// <summary>
    /// Processes the provided pages to extract posts.
    /// </summary>
    /// <param name="pages">All pages loaded for the quest.</param>
    /// <param name="threadInfo">Information about the thread.</param>
    /// <param name="adapter">The forum adapter that can transform this forum's
    /// HTML into posts we can understand.</param>
    /// <param name="quest">The quest being read.</param>
    /// <returns>A list of valid posts found.</returns>
    private List<PostType> GetPostsFromPages(
        Quest quest,
        IEnumerable<HtmlDocument?> pages,
        ThreadInformationType threadInfo,
        IForumAdapter adapter)
    {
        int startPage = ThreadInformation.GetStartPage(threadInfo, quest);

        var posts = pages
            .Where(p => p != null)
            .SelectMany((p, i) => adapter.GetPosts(p, quest, startPage + i))
            .Where(p => KeepPost(p, quest, threadInfo))
            .DistinctBy(p => p.Origin, OriginComparer.Instance) // remove sticky posts
            .OrderBy(p => p.Origin.ThreadPostNumber)
            .ToList();

        logger.LogDebug("Got {Count} posts for quest {questDisplayName}.", posts.Count, quest.DisplayName);

        return posts;
    }
    #endregion Basic Flow

    #region Keep Post Filtering
    /// <summary>
    /// A filtering function to determine if a post is to be kept by the quest.
    /// </summary>
    /// <param name="post">The post to check.</param>
    /// <param name="quest">The quest being tallied.</param>
    /// <param name="threadInfo">Thread information to identify author posts.</param>
    /// <returns><c>True</c> if the post should be kept, or <c>false</c> if the post should be skipped.</returns>
    private bool KeepPost(
        PostType post,
        Quest quest,
        ThreadInformationType threadInfo)
    {
        if (!post.HasVote)
            return false;

        if (PostIsBeforeStart(post, threadInfo) || PostIsAfterEnd(post, quest, threadInfo))
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

    /// <summary>
    /// Determine if a post falls before the starting point of the tallied range.
    /// </summary>
    /// <param name="post">The post to check</param>
    /// <param name="threadInfo">The tally range</param>
    /// <returns><c>True</c> if the post falls before the tally starting point.</returns>
    private static bool PostIsBeforeStart(PostType post, ThreadInformationType threadInfo)
    {
        if (threadInfo.RangeType == ThreadRangeRangeType.ByPostNumber)
            return post.Origin.ThreadPostNumber < threadInfo.StartPostNumber;

        return post.Origin.PostId.Id < threadInfo.StartPostId.Id;
    }

    /// <summary>
    /// Determine if a post falls after the ending point of the tallied range.
    /// </summary>
    /// <param name="post">The post to check</param>
    /// <param name="quest">The quest being tallied</param>
    /// <param name="threadInfo">The tally range</param>
    /// <returns><c>True</c> if the post falls after the tally ending point.</returns>
    private static bool PostIsAfterEnd(PostType post, Quest quest, ThreadInformationType threadInfo)
    {
        if (quest.ReadToEndOfThread || threadInfo.RangeType == ThreadRangeRangeType.ByPostId)
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
    /// <summary>
    /// Helper method to get a forum adapter and sync it up with the quest.
    /// </summary>
    /// <param name="quest">The quest being processed.</param>
    /// <param name="token">A cancellation token</param>
    /// <returns>An <see cref="IForumAdapter"/> for the quest.</returns>
    private async Task<IForumAdapter> GetForumAdapter(Quest quest, CancellationToken token)
    {
        IForumAdapter adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, token)
            .ConfigureAwait(false);

        SyncQuestWithForumAdapter(quest, adapter);

        return adapter;
    }

    /// <summary>
    /// Update the quest with information from the forum adapter.
    /// </summary>
    /// <param name="quest">The quest to sync up.</param>
    /// <param name="adapter">The forum adapter created for the quest.</param>
    private void SyncQuestWithForumAdapter(Quest quest, IForumAdapter adapter)
    {
        if (quest.PostsPerPage == 0)
            quest.PostsPerPage = adapter.GetDefaultPostsPerPage(quest.ThreadUri);

        if (adapter.HasRssThreadmarksFeed(quest.ThreadUri) == BoolEx.True &&
            quest.UseRSSThreadmarks == BoolEx.Unknown)
            quest.UseRSSThreadmarks = BoolEx.True;

        logger.LogDebug("Quest {questDisplayName} synced with forum adapter.", quest.DisplayName);
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
    private async Task<ThreadInformationType?> GetThreadInfoAsync(
        Quest quest,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        var infos = await adapter.GetThreadInformationAsync(quest, pageProvider, token)
            .ConfigureAwait(false);

        logger.LogDebug("Thread information acquired for {questDisplayName}.\n({threadData})",
            quest.DisplayName, infos);

        return infos;
    }

    /// <summary>
    /// Read all the relevant pages from the quest, based on the provided
    /// thread range information.
    /// </summary>
    /// <param name="quest">The quest</param>
    /// <param name="threadInfo">Thread range data</param>
    /// <param name="pageProvider">The page provider to read web pages.</param>
    /// <param name="adapter">The forum adapter for the quest.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A list of all loaded documents.</returns>
    private async Task<IEnumerable<HtmlDocument?>> ReadPagesFromQuestAsync(
        Quest quest,
        ThreadInformationType threadInfo,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        int firstPage = ThreadInformation.GetStartPage(threadInfo, quest);
        int lastPage = ThreadInformation.GetEndPage(threadInfo, quest);
        int pageCount = lastPage - firstPage + 1;

        if (pageCount < 1)
            return [];

        List<Task<HtmlDocument?>> tasks = [];

        for (int pageNum = firstPage; pageNum <= lastPage; pageNum++)
        {
            var pageUrl = adapter.GetUrlForPage(quest, pageNum);
            var shouldCache = pageNum == lastPage ? ShouldCache.No : ShouldCache.Yes;

            tasks.Add(pageProvider.GetHtmlDocumentAsync(
                              pageUrl, $"Page {pageNum}",
                              CachingMode.UseCache, shouldCache,
                              SuppressNotifications.No, token));
        }

        var finished = await Task.WhenAll(tasks)
            .ConfigureAwait(false);

        logger.LogDebug("Got {Count} pages loading {questDisplayName}.", finished.Length, quest.DisplayName);

        return finished;
    }
    #endregion Page Loading
}
