using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Input.Forums.ForumAdapters;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Threads;
using NetTally.Utility.Events;
using NetTally.Utility.Linq;
using NetTally.Web;

namespace NetTally.Input.Forums.Reading;

public class ForumReader(
    ILogger<ForumReader> logger,
    ForumAdapterFactory forumAdapterFactory,
    IQuestsInfo questsInfo,
    IPageProvider pageProvider) : IForumReader
{
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

    public async Task<QuestData> ReadQuestAsync(
        Quest quest,
        CancellationToken token)
    {
        logger.LogDebug("Reading Quest {quest}", quest.ThreadName);

        var questPosts = GetQuestSources(quest)
            .SelectAsync(q => GetPostsFromQuest(q, token), token)
            .ConfigureAwait(false);

        var data = QuestData.Empty;

        await foreach (var (Title, Posts) in questPosts)
        {
            data = data.CombineWith(Title, Posts);
            token.ThrowIfCancellationRequested();
        }

        return data;
    }

    /// <summary>
    /// Gets a list of all quest sources associated with the specified quest.
    /// </summary>
    /// <param name="quest">The quest with potential alternate sources.</param>
    /// <returns>All quest sources to be tallied.</returns>
    public IEnumerable<Quest> GetQuestSources(Quest quest)
    {
        return [quest, .. questsInfo.GetLinkedQuests(quest)];
    }

    private async Task<QuestData> GetPostsFromQuest(
        Quest quest,
        CancellationToken token)
    {
        try
        {
            pageProvider.StatusChanged += PageProvider_StatusChanged;

            IForumAdapter adapter = await quest.GetForumAdapter(forumAdapterFactory, token)
               .ConfigureAwait(ConfigureAwaitOptions.None);

            var threadInfo = await adapter.GetThreadInfoAsync(quest, pageProvider, token)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            logger.LogDebug("Thread information acquired for {questDisplayName}.\n({threadData})",
                quest.DisplayName, threadInfo);

            var pages = await GetPagesFromQuest(quest, adapter, threadInfo, token)
                .ConfigureAwait(ConfigureAwaitOptions.None);

            var posts = pages
                .SelectMany((p, i) => GetPostsFromPage(quest, p, i, adapter, threadInfo))
                .ToList();

            string title = CraftTitle(threadInfo, posts);

            return QuestData.Create(title, posts);
        }
        finally
        {
            pageProvider.StatusChanged -= PageProvider_StatusChanged;
        }
    }

    private async Task<IEnumerable<HtmlDocument?>> GetPagesFromQuest(
        Quest quest,
        IForumAdapter adapter,
        ThreadInfo threadInfo,
        CancellationToken token)
    {
        var requestedPages = GetPagesToLoad(quest, adapter, threadInfo);

        var pageLoads = requestedPages.Select(p =>
            pageProvider.GetHtmlDocumentAsync(
                p.Url,
                $"Page {p.PageNumber}",
                p.CacheMode,
                SuppressNotifications.No,
                token));

        var finished = await Task.WhenAll(pageLoads)
            .ConfigureAwait(ConfigureAwaitOptions.None);

        logger.LogDebug("Got {Count} pages loading {questDisplayName}.", finished.Length, quest.DisplayName);

        return finished;
    }

    private static IEnumerable<PageRequestInfo> GetPagesToLoad(
        Quest quest,
        IForumAdapter adapter,
        ThreadInfo threadInfo)
    {
        int firstPage = threadInfo.ThreadRange.GetStartPage();
        int lastPage = threadInfo.ThreadRange.GetEndPage();
        int pageCount = lastPage - firstPage + 1;

        if (pageCount < 1)
            return [];

        var urls = Enumerable.Range(firstPage, pageCount)
            .Select(pageNum =>
                PageRequestInfo.Create(
                    adapter.GetUrlForPage(quest, pageNum),
                    pageNum,
                    pageNum == lastPage ? CachingMode.NoCache : CachingMode.ReadWrite));

        return urls;
    }


    private static IEnumerable<Post> GetPostsFromPage(
        Quest quest,
        HtmlDocument? page,
        int index,
        IForumAdapter adapter,
        ThreadInfo threadInfo)
    {
        if (page is null)
            return [];

        var posts = adapter.GetPosts(page, quest, threadInfo.ThreadRange.GetStartPage() + index)
            .Where(p => KeepPost(p, quest, threadInfo))
            .DistinctBy(p => p.Origin) // remove sticky posts
            .OrderBy(p => p.Origin.PostNumber.Value);

        return posts;
    }

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

    private static string CraftTitle(ThreadInfo threadInfo, List<Post> posts)
    {
        long min = posts.Min(p => p.Origin.PostNumber.Value);
        long max = posts.Max(p => p.Origin.PostNumber.Value);

        string title = $"{threadInfo.Title} [{(posts.Count > 0 ? $"Posts: {min}-{max}" : "No Votes")}]";

        return title;
    }
}
