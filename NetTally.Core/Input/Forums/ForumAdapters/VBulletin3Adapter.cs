using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Models.Creation;
using NetTally.Models.Posts;
using NetTally.Models.Threads;
using NetTally.Models.Behavior;
using NetTally.Utility.HtmlNodes;
using NetTally.Web;

namespace NetTally.Input.Forums.ForumAdapters;

public partial class VBulletin3Adapter(
    IOptions<GlobalSettings> options,
    ILogger<VBulletin3Adapter> logger) : IForumAdapter
{
    readonly GlobalSettings inputOptions = options.Value;
    readonly ILogger<VBulletin3Adapter> logger = logger;

    #region IForumAdapter interface
    /// <summary>
    /// String to use for a line break between tasks.
    /// </summary>
    /// <param name="uri">The uri of the site that we're querying information for.</param>
    /// <returns>Returns the string to use for a line break event when outputting the tally.</returns>
    public string GetDefaultLineBreak(Uri uri)
    {
        return "———————————————————————————————————————————————————————";
    }

    /// <summary>
    /// Get the default number of posts per page for the site used by the origin.
    /// </summary>
    /// <param name="uri">The uri of the site that we're querying information for.</param>
    /// <returns>Returns a default number of posts per page for the given site.</returns>
    public int GetDefaultPostsPerPage(Uri uri)
    {
        return 20;
    }

    /// <summary>
    /// Gets whether the provided URI host is known to use RSS feeds for its threadmarks.
    /// </summary>
    /// <param name="uri">The uri of the site that we're querying information for.</param>
    /// <returns>Returns whether the site is known to use or not use RSS threadmarks.</returns>
    public BoolEx HasRssThreadmarksFeed(Uri uri)
    {
        return BoolEx.False;
    }

    /// <summary>
    /// Get a proper URL for a specific page of a thread of the URI provided.
    /// </summary>
    /// <param name="uri">The URI of the site that we're constructing a URL for.</param>
    /// <param name="page">The page number to create a URL for.</param>
    /// <returns>Returns a URL for the page requested.</returns>
    public string GetUrlForPage(Quest quest, int page)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        string append = page > 1 ? $"&page={page}" : "";

        return $"{GetBaseThreadUrl(quest.ThreadUri)}{append}";
    }


    /// <summary>
    /// Get a list of posts from the provided page.
    /// </summary>
    /// <param name="page">A web page from a forum that this adapter can handle.</param>
    /// <param name="quest">The quest being tallied, which may have options that we need to consider.</param>
    /// <returns>Returns a list of constructed posts from this page.</returns>
    public IEnumerable<Post> GetPosts(HtmlDocument page, Quest quest, int pageNumber)
    {
        if (quest == null || quest.ThreadUri == null || quest.ThreadUri == Quest.InvalidThreadUri)
            return [];

        var posts = from p in GetPostList(page)
                    where p != null
                    let post = GetPost(page, p, quest)
                    where post != null
                    select post;

        return posts;
    }

    /// <summary>
    /// Get information about the thread.
    /// This includes title, author, and starting range.
    /// </summary>
    /// <param name="quest">The quest being queried.</param>
    /// <param name="pageProvider">A page provider for loading pages.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns><see cref="ThreadInfo"/> containing thread information.</returns>
    public async Task<ThreadInfo> GetThreadInfoAsync(
        Quest quest,
        IPageProvider pageProvider,
        CancellationToken token)
    {
        var infoPage = await GetInfoPageAsync(quest, pageProvider, token);

        if (infoPage == null) return ThreadInfo.None;

        return GetThreadInfo(infoPage, quest);
    }

    #endregion IForumAdapter interface

    #region IForumAdapter support
    /// <summary>
    /// Get thread info from the provided page.
    /// </summary>
    /// <param name="page">A web page from a forum that this adapter can handle.</param>
    /// <returns>Returns thread information that can be gleaned from that page.</returns>
    private static ThreadInfo GetThreadInfo(HtmlDocument page, Quest quest)
    {
        string title = GetPageTitle(page);
        var author = Author.Unknown; // vBulletin doesn't show thread authors
        int pages = GetMaxPageNumberOfThread(page);

        var range = ThreadRange.CreateByRange(quest.StartPost, quest.EndPost, quest.PostsPerPage, pages);
        var info = ThreadInfo.Create(title, author, range);

        return info;
    }

    private async Task<HtmlDocument?> GetInfoPageAsync(
        Quest quest,
        IPageProvider pageProvider,
        CancellationToken token)
    {
        string infoPageUrl = GetUrlForPage(quest, 1);

        // Make sure to bypass the cache, since it may have changed since the last load.
        HtmlDocument? page = await pageProvider.GetHtmlDocumentAsync(
            infoPageUrl, "Info Page",
            CachingMode.WriteOnly,
            SuppressNotifications.Yes, token);

        return page;
    }
    #endregion IForumAdapter support


    #region Get Page Information
    private static string GetPageTitle(HtmlDocument page)
    {
        return ForumPostTextConverter.CleanupWebString(
            page.DocumentNode
                .Element("html")
                ?.Element("head")
                ?.Element("title")
                ?.InnerText);
    }

    private static int GetMaxPageNumberOfThread(HtmlDocument page)
    {
        // If there's no pagenav div, that means there's no navigation to alternate pages,
        // which means there's only one page in the thread.
        var pageNavDiv = page
            .DocumentNode
            .Element("html")
            ?.Element("body")
            ?.GetDescendantWithClass("div", "pagenav");

        if (pageNavDiv != null)
        {
            var vbMenuControl = pageNavDiv.GetDescendantWithClass("td", "vbmenu_control");

            if (vbMenuControl != null)
            {
                Regex pageNumsRegex = PageNumsRegex;

                Match m = pageNumsRegex.Match(vbMenuControl.InnerText);
                if (m.Success)
                {
                    return int.Parse(m.Groups["pages"].Value);
                }
            }
        }

        return 1;
    }
    #endregion Get Page Information

    #region Get Posts
    private static IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
    {
        var postList = page?.GetElementbyId("posts");

        if (postList == null)
            return [];

        return postList.Elements("div");
    }

    private Post? GetPost(HtmlDocument page, HtmlNode div, Quest quest)
    {
        if (div == null)
            return null;

        var table = div.Descendants("table").FirstOrDefault(a => a.Id.StartsWith("post", StringComparison.Ordinal));

        if (table == null)
            return null;

        var id = GetPostId(table);
        var author = GetPostAuthor(page, id);
        var number = PostId.Create(GetPostNumber(page, id));
        string text = GetPostText(page, id, quest);

        if (inputOptions.TrackPostAuthorsUniquely)
        {
            author = author.Rename($"{author.DisplayName}_{id.Value}");
        }

        var origin = Origin.CreateUser(author, quest.ThreadUri, GetPermalinkForId(quest.ThreadUri, id), id, number);
        var post = Post.Create(origin, text);

        return post;
    }

    private static PostId GetPostId(HtmlNode table)
    {
        var idString = table.Id["post".Length..];
        var id = PostId.Create(idString);

        return id ?? PostId.Zero;
    }

    private static Author GetPostAuthor(HtmlDocument page, PostId id)
    {
        string? authorName = null;
        string postAuthorDivID = $"postmenu_{id.Value}";

        var authorAnchor = page.GetElementbyId(postAuthorDivID).Element("a");

        if (authorAnchor != null)
        {
            // ??
            if (authorAnchor.Element("span") != null)
            {
                authorName = authorAnchor.Element("span")?.InnerText;
            }
            else
            {
                authorName = authorAnchor.InnerText;
            }
        }

        authorName = ForumPostTextConverter.CleanupWebString(authorName);

        return Author.Create(authorName);
    }

    private static string GetPostText(HtmlDocument page, PostId id, Quest quest)
    {
        string postMessageId = $"post_message_{id.Value}";

        var postContents = page.GetElementbyId(postMessageId);

        // Predicate filtering out elements that we don't want to include
        var exclusion = ForumPostTextConverter.GetClassExclusionPredicate("bbcode_quote");

        Uri host = new(quest.ThreadUri.GetLeftPart(UriPartial.Authority) + "/"); ;

        // Get the full post text.
        return ForumPostTextConverter.ExtractPostText(postContents, exclusion, host);
    }

    private static int GetPostNumber(HtmlDocument page, PostId id)
    {
        string postNumberAnchorID = $"postcount{id.Value}";

        var anchor = page.GetElementbyId(postNumberAnchorID);

        if (anchor != null)
        {
            string postNumText = anchor.GetAttributeValue("name", "");
            return int.Parse(postNumText);
        }

        return 0;
    }
    #endregion Get Posts

    #region URL Manipulation
    /// <summary>
    /// Get the URL string up to the end of any directory paths.
    /// </summary>
    /// <param name="uri">The URI to derive the URL from.</param>
    /// <returns>Returns a string containing the URL up to the last path.</returns>
    private static string GetBaseThreadUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        // https://forums.animesuki.com/showthread.php?t=152155

        string auth = uri.GetLeftPart(UriPartial.Authority);
        string page = uri.AbsolutePath;

        Match m = ThreadNumberRegex.Match(uri.Query);
        if (m.Success)
        {
            return $"{auth}{page}?t={m.Groups["thread"].Value}";
        }

        throw new ArgumentException("URI has no thread number.", nameof(uri));
    }

    /// <summary>
    /// Gets the URL string up to start of the query for posts.
    /// </summary>
    /// <param name="uri">The URI to derive the URL from.</param>
    /// <returns>Returns a string containing the URL up to the posts query.</returns>
    private static string GetHostBasePostsUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        // https://forums.animesuki.com/showthread.php?p=6355458

        string auth = uri.GetLeftPart(UriPartial.Authority);
        string page = uri.AbsolutePath;

        return $"{auth}{page}?p=";
    }

    private static Uri GetPermalinkForId(Uri uri, PostId postId)
    {
        string url = $"{GetHostBasePostsUrl(uri)}{postId.Value}";
        return new Uri(url);
    }

    [GeneratedRegex(@"\?t=(?<thread>\d+)")]
    private static partial Regex ThreadNumberRegex { get; }
    [GeneratedRegex(@"Page \d+ of (?<pages>\d+)")]
    private static partial Regex PageNumsRegex { get; }
    #endregion URL Manipulation
}
