using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Input.Forums.ForumAdapters;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Threads;
using NetTally.Utility.Events;
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

    /// <summary>
    /// Reads quest data from the sources specified by the provided quest.
    /// </summary>
    /// <param name="quest">The quest to read.</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>A collection of found quest data.</returns>
    public async Task<QuestData> ReadQuestAsync(
        Quest quest,
        CancellationToken token)
    {
        logger.LogDebug("Reading Quest {quest}", quest.ThreadName);

        var questPosts = GetQuestSources(quest)
            .ToAsyncEnumerable()
            .Select(GetPostsFromQuest)
            .WithCancellation(token)
            .ConfigureAwait(false);

        var data = QuestData.Empty;

        await foreach (var questData in questPosts)
        {
            data = data.CombineWith(questData);
        }

        return data;
    }

    private ReadOnlyCollection<Quest> GetQuestSources(Quest quest)
    {
        return [quest, .. questsInfo.GetLinkedQuests(quest)];
    }

    private async ValueTask<QuestData> GetPostsFromQuest(
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

            QuestData questData = QuestData.Empty;

            await foreach (var page in GetPagesFromQuest(quest, adapter, threadInfo, token))
            {
                questData = questData.CombineWith(GetPostsFromPage(
                    quest,
                    page.HtmlDocument,
                    page.RequestInfo.PageNumber,
                    adapter,
                    threadInfo));
            }

            string title = FormatTitle(threadInfo, questData.Posts);

            questData = questData.CombineWith(title);

            return questData;
        }
        finally
        {
            pageProvider.StatusChanged -= PageProvider_StatusChanged;
        }
    }

    private async IAsyncEnumerable<PageRequestData> GetPagesFromQuest(
        Quest quest,
        IForumAdapter adapter,
        ThreadInfo threadInfo,
        [EnumeratorCancellation] CancellationToken token)
    {
        var pages = GetPagesToLoad(quest, adapter, threadInfo)
            .ToAsyncEnumerable()
            .Select(GetPage)
            .WithCancellation(token)
            .ConfigureAwait(false);

        await foreach (var pageData in pages)
        {
            if (pageData != null)
            {
                yield return pageData;
            }
        }
    }

    private async ValueTask<PageRequestData?> GetPage(
        PageRequestInfo pageRequestInfo,
        CancellationToken token)
    {
        var document = await pageProvider.GetHtmlDocumentAsync(
                    pageRequestInfo.Url,
                    $"Page {pageRequestInfo.PageNumber}",
                    pageRequestInfo.CacheMode,
                    SuppressNotifications.No,
                    token);

        if (document is null)
            return null;

        return new PageRequestData(pageRequestInfo, document);
    }

    private static IEnumerable<PageRequestInfo> GetPagesToLoad(
        Quest quest,
        IForumAdapter adapter,
        ThreadInfo threadInfo)
    {
        int firstPage = threadInfo.ThreadRange.StartPage;
        int lastPage = threadInfo.ThreadRange.EndPage;
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

        var posts = adapter.GetPosts(page, quest, threadInfo.ThreadRange.StartPage + index)
            .Where(p => KeepPost(p, quest, threadInfo))
            .DistinctBy(p => p.Origin) // remove sticky posts
            .OrderBy(p => p.Origin.PostNumber.Value);

        return posts;
    }

    /// <summary>
    /// Perform checks on all the different conditions that would cause
    /// us to want to discard the post in question from the tally.
    /// </summary>
    /// <param name="post">The post to check.</param>
    /// <param name="quest">The quest the post is for.</param>
    /// <param name="threadInfo">Information about the thread tally range.</param>
    /// <returns><c>true</c> if the post should be kept. Otherwise <c>false</c>.</returns>
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

    private static string FormatTitle(ThreadInfo threadInfo, IList<Post> posts)
    {
        long min = posts.Min(p => p.Origin.PostNumber.Value);
        long max = posts.Max(p => p.Origin.PostNumber.Value);

        string title = $"{threadInfo.Title} [{(posts.Count > 0 ? $"Posts: {min}-{max}" : "No Votes")}]";

        return title;
    }
}
