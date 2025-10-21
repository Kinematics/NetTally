using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Models.Creation;
using NetTally.Models.Posts;
using NetTally.Models.Threads;
using NetTally.Models.Behavior;
using NetTally.Utility.Async;
using NetTally.Utility.Filtering;
using NetTally.Utility.HtmlNodes;
using NetTally.Utility.Linq;
using NetTally.Web;
using NetTally.Models.Defaults;

namespace NetTally.Input.Forums.ForumAdapters;

public partial class XenForo1Adapter(
    IOptions<GlobalSettings> options,
    ILogger<XenForo1Adapter> logger) : IForumAdapter
{
    readonly GlobalSettings inputOptions = options.Value;
    readonly ILogger<XenForo1Adapter> logger = logger;

    #region Static data
    // May possibly end with /page-00#post-00
    [GeneratedRegex(@"threads/[^/]+/(page-(?<page>\d+))?(#?post-(?<post>\d+))?$")]
    private static partial Regex LongFragmentRegex { get; }

    // The short HREF version gives the post ID
    [GeneratedRegex(@"posts/(?<tmID>\d+)/?$")]
    private static partial Regex ShortFragmentRegex { get; }
    #endregion

    #region IForumAdapter interface
    /// <summary>
    /// String to use for a line break between tasks.
    /// </summary>
    /// <param name="uri">The uri of the site that we're querying information for.</param>
    /// <returns>Returns the string to use for a line break event when outputting the tally.</returns>
    public string GetDefaultLineBreak(Uri uri)
    {
        return uri.Host switch
        {
            "forums.spacebattles.com" => "———————————————————————————————————————————————————————",
            _ => "[hr]——————————————————————————————————————————————[/hr]"
        };
    }

    /// <summary>
    /// Get the default number of posts per page for the site used by the origin.
    /// </summary>
    /// <param name="uri">The uri of the site that we're querying information for.</param>
    /// <returns>Returns a default number of posts per page for the given site.</returns>
    public int GetDefaultPostsPerPage(Uri uri)
    {
        return uri.Host switch
        {
            "forum.questionablequesting.com" => 30,
            _ => 25
        };
    }

    /// <summary>
    /// Gets whether the provided URI host is known to use RSS feeds for its threadmarks.
    /// </summary>
    /// <param name="uri">The uri of the site that we're querying information for.</param>
    /// <returns>Returns whether the site is known to use or not use RSS threadmarks.</returns>
    public BoolEx HasRssThreadmarksFeed(Uri uri)
    {
        return uri.Host switch
        {
            "forums.sufficientvelocity.com" or "forums.spacebattles.com" => BoolEx.True,
            "forum.questionablequesting.com" => BoolEx.False,
            _ => BoolEx.Unknown,
        };
    }

    /// <summary>
    /// Get a proper URL for a specific page of a thread of the URI provided.
    /// </summary>
    /// <param name="uri">The URI of the site that we're constructing a URL for.</param>
    /// <param name="page">The page number to create a URL for.</param>
    /// <returns>Returns a URL for the page requested.</returns>
    public string GetUrlForPage(Quest quest, int page)
    {
        string append = page > 1 ? $"page-{page}" : "";

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
                    let post = GetPost(p, quest)
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
        HtmlDocument? page = await GetInfoPageAsync(quest, pageProvider, token);

        if (page != null)
        {
            string title = GetPageTitle(page);
            var author = GetPageAuthor(page) ?? Author.Unknown;
            int pages = GetMaxPageNumberOfThread(page);

            var range = await GetRangeInfoAsync(quest, pageProvider, pages, token);

            return ThreadInfo.Create(title, author, range);
        }

        return ThreadInfo.None;
    }

    #endregion IForumAdapter interface

    #region IForumAdapter support
    /// <summary>
    /// Gets the range of post numbers to tally, for the given quest.
    /// This may require loading information from the site.
    /// </summary>
    /// <param name="quest">The quest being tallied.</param>
    /// <param name="pageProvider">The page provider to use to load any needed pages.</param>
    /// <param name="token">The cancellation token to check for cancellation requests.</param>
    /// <returns>Returns a ThreadRangeInfo describing which pages to load for the tally.</returns>
    private async Task<ThreadRange> GetRangeInfoAsync(
            Quest quest,
            IPageProvider pageProvider,
            int numberOfPages,
            CancellationToken token)
    {
        if (quest.CheckForLastThreadmark)
        {
            if ((await TryGetRSSThreadmarksRange(quest, pageProvider, numberOfPages, token))
                .TryOut(out var threadRange))
            {
                return threadRange;
            }

            if ((await TryGetThreadmarksRange(quest, pageProvider, numberOfPages, token))
                .TryOut(out threadRange))
            {
                return threadRange;
            }
        }

        return ThreadRange.CreateByRange(quest.StartPost, quest.EndPost, quest.PostsPerPage, numberOfPages);
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

    private static Author? GetPageAuthor(HtmlDocument page)
    {
        // Find a common parent for other data
        HtmlNode? pageContent = GetPageContent(page, PageType.Thread)
            ?? throw new InvalidOperationException("Cannot find content on page.");

        // Non-thread pages (such as threadmark pages) won't have a title bar.
        _ = pageContent.GetDescendantWithClass("titleBar") ??
            throw new InvalidOperationException("Not a valid forum thread.");

        // Find the thread author
        HtmlNode? authorNode = page.GetElementbyId("pageDescription")?.GetChildWithClass("username");

        string authorName = ForumPostTextConverter.CleanupWebString(authorNode?.InnerText ?? "");
        return Author.Create(authorName);
    }

    private static int GetMaxPageNumberOfThread(HtmlDocument page)
    {
        // Find a common parent for other data
        HtmlNode? pageContent = GetPageContent(page, PageType.Thread)
            ?? throw new InvalidOperationException("Cannot find content on page.");

        // Find the number of pages in the thread
        var pageNavLinkGroup = pageContent.GetDescendantWithClass("div", "pageNavLinkGroup");
        var pageNav = pageNavLinkGroup?.GetChildWithClass("PageNav");
        string lastPage = pageNav?.GetAttributeValue("data-last", "") ?? "";

        return string.IsNullOrEmpty(lastPage) ? 1 : int.Parse(lastPage);
    }
    #endregion Get Page Information

    #region Get ThreadInfoRange information
    private async Task<(bool, ThreadRange)> TryGetThreadmarksRange(
        Quest quest, IPageProvider pageProvider, int numberOfPages, CancellationToken token)
    {
        if (quest == null)
            return (false, ThreadRange.None);

        // Load the threadmarks so that we can find the starting post page or number.
        HtmlDocument? threadmarksPage = await pageProvider.GetHtmlDocumentAsync(
            GetThreadmarksPageUrl(quest.ThreadUri), "Threadmarks",
            CachingMode.ReadWrite,
            SuppressNotifications.No, token);

        if (threadmarksPage == null)
            return (false, ThreadRange.None);

        var threadmarks = GetThreadmarksListFromPage(threadmarksPage, quest);

        // If there aren't any threadmarks, bail.
        if (!threadmarks.Any())
            return (false, ThreadRange.None);

        // Threadmarks have already been filtered, so just pick the last one,
        // and get the URL for the threadmark.
        string lastThreadmarkHref = threadmarks.Last().GetAttributeValue("href", "");

        // Make sure we found something.
        if (string.IsNullOrEmpty(lastThreadmarkHref))
            return (false, ThreadRange.None);

        // The threadmark list might use the long version of the URL (including thread info),
        // or the short version (which only shows the post number).

        // If we're given the short version of the URL, just do a HEAD query to get the long version.
        Match mShort = ShortFragmentRegex.Match(lastThreadmarkHref);
        if (mShort.Success)
        {
            // Get the post ID for the threadmark
            string tmID = mShort.Groups["tmID"].Value;
            var postId = PostId.Create(tmID);

            if (postId == null)
                return (false, ThreadRange.None);

            // The threadmark href might be a relative path, so make sure to
            // create a proper absolute path to load.
            Uri permalink = GetPermalinkForId(quest.ThreadUri, postId);

            // Attempt to load the threadmark page's headers.  Use cache if available, and cache the result as appropriate.
            string fullUrl = await pageProvider.GetRedirectUrlAsync(
                permalink.AbsoluteUri, "",
                SuppressNotifications.Yes, token);

            if (!string.IsNullOrEmpty(fullUrl))
                lastThreadmarkHref = fullUrl;
        }

        // If we have the long URL, we can extract the page number and post number from the URL itself.
        Match m1 = LongFragmentRegex.Match(lastThreadmarkHref);
        if (m1.Success)
        {
            int page = 0;
            int post = 0;

            if (m1.Groups["page"].Success)
                page = int.Parse(m1.Groups["page"].Value);
            if (m1.Groups["post"].Success)
                post = int.Parse(m1.Groups["post"].Value);

            // If neither matched, it's post 1/page 1
            if (page == 0 && post == 0)
                return (true, ThreadRange.CreateByRange(1, 0, quest.PostsPerPage, numberOfPages));

            var postId = PostId.Create(post);

            // Otherwise create a range based on the post ID.
            return (true, ThreadRange.CreateByPostId(postId, page, numberOfPages));
        }

        // Failed to find anything.
        return (false, ThreadRange.None);
    }

    private static async Task<(bool found, ThreadRange)> TryGetRSSThreadmarksRange(
        Quest quest, IPageProvider pageProvider, int numberOfPages, CancellationToken token)
    {
        if (quest == null || quest.ThreadUri == null)
            return (false, ThreadRange.None);

        if (quest.UseRSSThreadmarks == BoolEx.False)
            return (false, ThreadRange.None);

        XDocument? rss = await pageProvider.GetXmlDocumentAsync(
            GetRssThreadmarksUrl(quest.ThreadUri), "Threadmarks",
            CachingMode.ReadWrite,
            SuppressNotifications.No, token);

        if (rss == null)
        {
            if (quest.UseRSSThreadmarks == BoolEx.Unknown)
                quest.UseRSSThreadmarks = BoolEx.False;

            return (false, ThreadRange.None);
        }

        if (rss.Root?.Name != "rss")
            return (false, ThreadRange.None);

        var channel = rss.Root.Element(XName.Get("channel", ""));

        var items = channel?.Elements(XName.Get("item", "")) ?? [];

        XName titleName = XName.Get("title", "");
        XName pubDate = XName.Get("pubDate", "");

        // Use threadmark filters to filter out unwanted threadmark titles.
        var filteredItems = from item in items
                            let title = item.Element(titleName)?.Value
                            where (quest.UseCustomThreadmarkFilters && quest.ThreadmarkFilter.Allows(title)) ||
                                  (!quest.UseCustomThreadmarkFilters && RegexFilter.DefaultThreadmarkFilter.Allows(title))
                            let pub = item.Element(pubDate)?.Value
                            where string.IsNullOrEmpty(pub) == false
                            let pubStamp = DateTime.Parse(pub)
                            orderby pubStamp descending // Most recent is first
                            select item;

        // Take the first (most recent) item from the list.
        var recentItem = filteredItems.FirstOrDefault();

        if (recentItem != null)
        {
            string? href = recentItem.Element(XName.Get("link", ""))?.Value;

            if (!string.IsNullOrEmpty(href))
            {
                // If we have the long URL, we can extract the page number and post number from the URL itself.
                Match mr = LongFragmentRegex.Match(href);
                if (mr.Success)
                {
                    int page = 0;
                    int post = 0;

                    if (mr.Groups["page"].Success)
                        page = int.Parse(mr.Groups["page"].Value);
                    if (mr.Groups["post"].Success)
                        post = int.Parse(mr.Groups["post"].Value);

                    // If neither matched, it's post 1/page 1
                    // Return a By Post range
                    if (page == 0 || post == 0)
                        return (true, ThreadRange.CreateByRange(1, 0, quest.PostsPerPage, numberOfPages));

                    var postId = PostId.Create(post);

                    // Otherwise create a range based on the post ID.
                    return (true, ThreadRange.CreateByPostId(postId, page, numberOfPages));
                }
            }
        }

        return (false, ThreadRange.None);
    }

    private IEnumerable<HtmlNode> GetThreadmarksListFromPage(HtmlDocument threadmarksPage, Quest quest)
    {
        try
        {
            HtmlNode? content = GetPageContent(threadmarksPage, PageType.Threadmarks);

            HtmlNode? threadmarksDiv = content?.GetDescendantWithClass("div", "threadmarks");

            HtmlNode? listOfThreadmarks = null;

            HtmlNode? threadmarkList = threadmarksDiv?.GetDescendantWithClass("threadmarkList");

            if (threadmarkList != null)
            {
                // We have a .threadmarkList node.  This is either an ol itself, or it will contain a ThreadmarkCategory_# ol node.  We want category 1.

                if (threadmarkList.Name == "ol")
                {
                    if (threadmarkList.GetAttributeValue("class", "").Contains("ThreadmarkCategory"))
                    {
                        if (!threadmarkList.HasClass("ThreadmarkCategory_1"))
                            return [];
                    }

                    listOfThreadmarks = threadmarkList;
                }
                else
                {
                    listOfThreadmarks = threadmarkList.GetDescendantWithClass("ol", "ThreadmarkCategory_1");
                }
            }
            else
            {
                // threadmarkList was null.  There is no .threadmarkList node, so check for undecorated ul that contains .threadmarkItem list items.
                listOfThreadmarks = threadmarksDiv?.Descendants("ul")
                    .FirstOrDefault(e => e.Elements("li").Any(a => a.HasClass("threadmarkItem")));
            }

            return listOfThreadmarks
                ?.Elements("li")
                .TraverseList(childSelector, nodeSelector, filterLambda)
                .Where(n => n != null)
                .Select(n => n!)
                ?? [];
        }
        catch (ArgumentNullException e)
        {
            logger.LogError(e, "Failure when attempting to get the list of threadmarks from the index page. Null list somewhere?");
        }

        return [];

        // Local functions
        bool filterLambda(HtmlNode? n) => n != null &&
            ((quest.UseCustomThreadmarkFilters && quest.ThreadmarkFilter.Allows(n.InnerText)) ||
            (!quest.UseCustomThreadmarkFilters && RegexFilter.DefaultThreadmarkFilter.Allows(n.InnerText)));

        static IEnumerable<HtmlNode> childSelector(HtmlNode i) => i.Element("ul")?.Elements("li") ?? [];

        static HtmlNode? nodeSelector(HtmlNode n) => n.Element("a");
    }
    #endregion Get ThreadInfoRange information

    #region Get Posts
    private static IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
    {
        // The ordered list containing all messages.
        var messageList = page?.GetElementbyId("messageList");

        // Return all found list items in the message list, or an empty list.
        return messageList?.Elements("li") ?? [];
    }

    private Post? GetPost(HtmlNode li, Quest quest)
    {
        if (li == null)
            return null;

        var id = GetPostId(li);
        var author = GetPostAuthor(li);
        string text = GetPostText(li, quest);
        var number = PostNumber.Create(GetPostNumber(li));

        if (author is null)
            return null;

        if (inputOptions.TrackPostAuthorsUniquely)
        {
            author = author.Rename($"{author.DisplayName}_{id.Value}");
        }

        var origin = Origin.CreateUser(author, quest.ThreadUri, GetPermalinkForId(quest.ThreadUri, id), id, number);
        var post = Post.Create(origin, text);

        return post;
    }

    private static Author? GetPostAuthor(HtmlNode li)
    {
        string authorName = li.GetAttributeValue("data-author", "");
        authorName = ForumPostTextConverter.CleanupWebString(authorName);
        return Author.Create(authorName);
    }

    private static PostId GetPostId(HtmlNode li)
    {
        string id = li.Id["post-".Length..];
        return PostId.Create(id) ?? PostId.Zero;
    }

    private static string GetPostText(HtmlNode li, Quest quest)
    {
        // Get the primary content of the list item
        HtmlNode? primaryContent = li.GetChildWithClass("primaryContent");

        // On one branch, we can get the post text
        HtmlNode? messageContent = primaryContent?.GetChildWithClass("messageContent");
        HtmlNode? postBlock = messageContent?.Element("article")?.Element("blockquote");

        List<string> excludedClasses = ["bbCodeQuote", "messageTextEndMarker","advbbcodebar_encadre",
            "advbbcodebar_article", "adv_tabs_wrapper", "adv_slider_wrapper"];

        if (quest.IgnoreSpoilers)
            excludedClasses.Add("bbCodeSpoilerContainer");

        // Predicate for filtering out elements that we don't want to include
        var exclusions = ForumPostTextConverter.GetClassesExclusionPredicate(excludedClasses);

        Uri host = new(quest.ThreadUri.GetLeftPart(UriPartial.Authority) + "/"); ;

        // Get the full post text.
        return ForumPostTextConverter.ExtractPostText(postBlock, exclusions, host);
    }

    private static int GetPostNumber(HtmlNode li)
    {
        // Get the primary content of the list item
        HtmlNode? primaryContent = li.GetChildWithClass("primaryContent");

        // On another branch of the primary content, we can get the post number.
        HtmlNode? messageMeta = primaryContent?.GetChildWithClass("messageMeta");
        // HTML parsing of the post was corrupted somehow.
        if (messageMeta == null)
        {
            return 0;
        }
        HtmlNode? publicControls = messageMeta.GetChildWithClass("publicControls");
        HtmlNode? postNumber = publicControls?.GetChildWithClass("postNumber");

        if (postNumber == null)
            return 0;

        string postNumberText = postNumber.InnerText;
        // Skip the leading # character.
        if (postNumberText.StartsWith('#'))
            postNumberText = postNumberText[1..];

        return int.Parse(postNumberText);
    }
    #endregion Get Posts

    #region URL Manipulation
    /// <summary>
    /// Get the URL string up to the end of ".../threads/thread.name.12345/"
    /// </summary>
    /// <param name="uri">The URI to derive the URL from.</param>
    /// <returns>Returns a string containing the URL up to the thread name.</returns>
    private static string GetBaseThreadUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        StringBuilder sb = new();

        sb.Append(uri.GetLeftPart(UriPartial.Authority));

        bool foundThreads = false;

        // Add segments up to the thread name.
        for (int i = 0; i < uri.Segments.Length; i++)
        {
            sb.Append(uri.Segments[i]);

            if (foundThreads)
                break;

            if (uri.Segments[i] == "threads/")
                foundThreads = true;
        }

        if (sb[^1] != '/')
            sb.Append('/');

        return sb.ToString();
    }

    /// <summary>
    /// Gets the URL string up to the end of ".../posts/"
    /// It's generated at the same level as the ".../threads/" directory.
    /// </summary>
    /// <param name="uri">The URI to derive the URL from.</param>
    /// <returns>Returns a string containing the URL up to the posts directory.</returns>
    private static string GetHostBasePostsUrl(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        StringBuilder sb = new();

        sb.Append(uri.GetLeftPart(UriPartial.Authority));

        // Add segments up to the thread name.
        for (int i = 0; i < uri.Segments.Length; i++)
        {
            if (uri.Segments[i] == "threads/")
                break;

            sb.Append(uri.Segments[i]);
        }

        sb.Append("posts/");

        return sb.ToString();
    }

    private static string GetThreadmarksPageUrl(Uri uri)
    {
        return $"{GetBaseThreadUrl(uri)}threadmarks?category_id=1";
    }

    private static string GetRssThreadmarksUrl(Uri uri)
    {
        return $"{GetBaseThreadUrl(uri)}threadmarks.rss?category_id=1";
    }

    private static Uri GetPermalinkForId(Uri uri, PostId postId)
    {
        string url = $"{GetHostBasePostsUrl(uri)}{postId.Value}/";
        return new Uri(url);
    }
    #endregion URL Manipulation

    #region Misc Helper Functions
    private static HtmlNode? GetPageContent(HtmlDocument page, PageType pageType)
    {
        ArgumentNullException.ThrowIfNull(page);

        var contentNode = page.GetElementbyId("content")
            ?? throw new InvalidOperationException("Page does not have a content section.");

        switch (pageType)
        {
            case PageType.Thread:
                if (!contentNode.HasClass("thread_view"))
                    throw new InvalidOperationException("This page does not contain a forum thread.");
                break;
            case PageType.Threadmarks:
                if (!contentNode.HasClass("threadmarks"))
                    throw new InvalidOperationException("This page does not contain threadmarks.");
                break;
        }

        return contentNode;
    }
    #endregion Misc Helper Functions
}
