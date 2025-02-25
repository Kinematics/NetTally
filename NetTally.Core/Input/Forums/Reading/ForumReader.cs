using System.Text;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Configure;
using NetTally.CustomEventArgs;
using NetTally.Enums;
using NetTally.Input.Forums.ForumAdapters;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Threads;
using NetTally.Utility;
using NetTally.Web;

namespace NetTally.Input.Forums.Reading;
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
    public async Task<(IEnumerable<string> Titles, IEnumerable<Post> Posts)>
        ReadQuestAsync(Quest quest, CancellationToken cancellationToken)
    {
        List<Quest> questsToRead = [quest, .. questsInfo.GetLinkedQuests(quest)];

        logger.LogDebug("Reading quest {questName} with any linked quests. Total quests: {total}",
            quest.DisplayName, questsToRead.Count);

        // Load all posts from the base quest and all linked quests.
        var results = await Task.WhenAll(
                questsToRead.Select(q => GetPostsWithVotesFromQuestAsync(q, cancellationToken)))
            .ConfigureAwait(ConfigureAwaitOptions.None);

        var allTitles = results.Select(q => AddPostCountToTitle(q.Title, q.Posts));
        var allPosts = results.SelectMany(q => q.Posts);

        if (allTitles.Any(t => t == Strings.Error))
        {
            throw new Exception("Unable to load all pages.");
        }

        return (allTitles, allPosts);
    }

    private static string AddPostCountToTitle(string title, List<Post> posts)
    {
        StringBuilder sb = new();

        sb.Append(title)
          .Append(' ')
          .Append(posts.Count > 0
                ? $"[Posts: {posts.Min(p => p.Origin.PostNumber.Id)}-{posts.Max(p => p.Origin.PostNumber.Id)}]"
                : "[No votes]");

        return sb.ToString();
    }

    /// <summary>
    /// Read posts from a specific quest.
    /// </summary>
    /// <param name="quest">The quest to read.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A title describing the quest and posts, plus all the valid posts found.</returns>
    private async Task<(string Title, List<Post> Posts)>
        GetPostsWithVotesFromQuestAsync(Quest quest, CancellationToken token)
    {
        logger.LogDebug("Reading posts from quest {questDisplayName} with ForumReader.",
            quest.DisplayName);

        using var pageProvider = serviceProvider.GetRequiredService<IPageProvider>();

        try
        {
            pageProvider.StatusChanged += PageProvider_StatusChanged;

            IForumAdapter adapter = await GetForumAdapter(quest, token)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            return await GetPostsWithVotesAsync(quest, pageProvider, adapter, token)
                .ConfigureAwait(ConfigureAwaitOptions.None);
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
    private async Task<(string Title, List<Post> Posts)> GetPostsWithVotesAsync(
        Quest quest,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        // Get information about the thread, and which pages need to be loaded.
        //(ThreadRangeType rangeInfo, ThreadInfoType threadInfo)
        var threadData = await GetThreadInfoAsync(quest, pageProvider, adapter, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        if (threadData != ThreadInfos.None)
        {
            var pages = await ReadPagesFromQuestAsync(quest, threadData, pageProvider, adapter, token)
                                .ConfigureAwait(ConfigureAwaitOptions.None);

            if (pages.All(p => p != null))
            {
                var posts = GetPostsWithVotesFromPages(quest, pages, threadData, adapter);

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
    private List<Post> GetPostsWithVotesFromPages(
        Quest quest,
        IEnumerable<HtmlDocument?> pages,
        ThreadInfo threadInfo,
        IForumAdapter adapter)
    {
        int startPage = threadInfo.ThreadRange.GetStartPage();

        var posts = pages
            .Where(p => p != null)
            .SelectMany((p, i) => adapter.GetPosts(p!, quest, startPage + i))
            .Where(p => KeepPost(p, quest, threadInfo))
            .DistinctBy(p => p.Origin) // remove sticky posts
            .OrderBy(p => p.Origin.PostNumber.Id)
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
    private static bool KeepPost(
        Post post,
        Quest quest,
        ThreadInfo threadInfo)
    {
        if (!post.HasVote)
            return false;

        if (post.IsBeforeStart(threadInfo.ThreadRange) || post.IsAfterEnd(threadInfo.ThreadRange))
            return false;

        if (post.Origin.Author == threadInfo.Author)
            return false;

        if (post.MatchesUsernameFilter(quest))
            return false;

        if (post.MatchesPostNumberFilter(quest))
            return false;

        return true;
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
            .ConfigureAwait(ConfigureAwaitOptions.None);

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
    private async Task<ThreadInfo> GetThreadInfoAsync(
        Quest quest,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        var infos = await adapter.GetThreadInfoAsync(quest, pageProvider, token)
            .ConfigureAwait(ConfigureAwaitOptions.None);

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
        ThreadInfo threadInfo,
        IPageProvider pageProvider,
        IForumAdapter adapter,
        CancellationToken token)
    {
        int firstPage = threadInfo.ThreadRange.GetStartPage();
        int lastPage = threadInfo.ThreadRange.GetEndPage();
        int pageCount = lastPage - firstPage + 1;

        if (pageCount < 1)
            return [];

        List<Task<HtmlDocument?>> tasks = [];

        for (int pageNum = firstPage; pageNum <= lastPage; pageNum++)
        {
            var pageUrl = adapter.GetUrlForPage(quest, pageNum);
            var caching = pageNum == lastPage ? CachingMode.ReadOnly : CachingMode.ReadWrite;

            tasks.Add(pageProvider.GetHtmlDocumentAsync(
                              pageUrl, $"Page {pageNum}",
                              caching,
                              SuppressNotifications.No, token));
        }

        var finished = await Task.WhenAll(tasks)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        logger.LogDebug("Got {Count} pages loading {questDisplayName}.", finished.Length, quest.DisplayName);

        return finished;
    }
    #endregion Page Loading
}
