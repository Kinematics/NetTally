using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Extensions;
using NetTally.Configure;
using NetTally.Input.Utility;
using NetTally.Web;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Threads;
using NetTally.Tally.ComponentsF.Posts;

namespace NetTally.Input.Forums.ForumAdaptersF
{
    public partial class XenForo2Adapter(
        IOptions<GlobalSettings> options,
        ILogger<XenForo2Adapter> logger) : IForumAdapter
    {
        readonly GlobalSettings inputOptions = options.Value;
        readonly ILogger<XenForo2Adapter> logger = logger;

        #region Regex data
        // May possibly end with /page-00#post-00
        [GeneratedRegex(@"threads/[^/]+/(page-(?<page>\d+))?(?:\?[^#]+)?(#?post-(?<post>\d+))?$")]
        private static partial Regex LongFragment();

        // The short HREF version gives the post ID
        [GeneratedRegex(@"posts/(?<tmID>\d+)/?$")]
        private static partial Regex ShortFragment();

        // RSS permalink does not include the page number.
        [GeneratedRegex(@"threads/[^/]+/(post-(?<post>\d+))?$")]
        private static partial Regex PermalinkFragment();
        #endregion

        #region IForumAdapter interface
        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns the string to use for a line break event when outputting the tally.</returns>
        public string GetDefaultLineBreak(Uri uri)
        {
            return "[hr][/hr]";
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
            ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

            string append = page > 1 ? $"page-{page}" : "";

            return $"{GetBaseThreadUrl(quest.ThreadUri)}{append}";
        }

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <param name="quest">The quest being tallied, which may have options that we need to consider.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        public IEnumerable<PostType> GetPosts(HtmlDocument page, Quest quest, int pageNumber)
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
        /// <returns>A tuple of <see cref="ThreadInfoType"/> and <see cref="ThreadRangeType"/></returns>
        public async Task<(ThreadInfoType, ThreadRangeType)?>
            GetThreadInformationAsync(Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            var threadInfo = await GetThreadInfoAsync(quest, pageProvider, token);
            var rangeInfo = await GetRangeInfoAsync(quest, pageProvider, token);

            return (threadInfo, rangeInfo);
        }
        #endregion IForumAdapter interface

        #region IForumAdapter support

        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        private async Task<ThreadInfoType> GetThreadInfoAsync(
            Quest quest,
            IPageProvider pageProvider,
            CancellationToken token)
        {
            HtmlDocument? page = await GetInfoPageAsync(quest, pageProvider, token);
            ThreadInfoType? info = null;

            if (page != null)
            {
                var (headerNode, bodyNode) = GetPageInfoNodes(page);
                string title = GetPageTitle(page);
                var author = GetPageAuthor(headerNode);
                int pages = GetMaxPageNumberOfThread(bodyNode);

                info = ThreadInfo.Create(title, author, pages);
            }

            return info ?? ThreadInfo.None;
        }

        /// <summary>
        /// Gets the range of post numbers to tally, for the given quest.
        /// This may require loading information from the site.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="pageProvider">The page provider to use to load any needed pages.</param>
        /// <param name="token">The cancellation token to check for cancellation requests.</param>
        /// <returns>Returns a ThreadRangeInfo describing which pages to load for the tally.</returns>
        private async Task<ThreadRangeType> GetRangeInfoAsync(
            Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            ThreadRangeType? rangeInfo = null;

            if (quest.CheckForLastThreadmark)
            {
                rangeInfo = await TryGetRSSThreadmarksRange(quest, pageProvider, token) ??
                            await TryGetThreadmarksRange(quest, pageProvider, token);
            }

            return rangeInfo ?? ThreadRange.CreateRangeByPost(quest.StartPost);
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
                CachingMode.BypassCache, ShouldCache.Yes,
                SuppressNotifications.Yes, token)
                .ConfigureAwait(false);

            return page;
        }

        #endregion IForumAdapter support

        #region Get Page Information
        private static (HtmlNode headerNode, HtmlNode bodyNode) GetPageInfoNodes(HtmlDocument page)
        {
            var topNode = page.GetElementbyId("top");

            var bodyNode = topNode.GetChildWithClass("div", "p-body") ??
                topNode.GetDescendantWithClass("div", "p-body") ??
                throw new InvalidOperationException("Unable to find p-body.");

            if (bodyNode.Elements("div").Any(n => n.HasClass("p-body-inner")))
            {
                bodyNode = bodyNode.GetChildWithClass("p-body-inner")!;
            }

            var headerNode = bodyNode.GetChildWithClass("div", "p-body-header") ??
                throw new InvalidOperationException("Unable to find p-body-header.");

            return (headerNode, bodyNode);
        }

        private static string GetPageTitle(HtmlDocument page)
        {
            return ForumPostTextConverter.CleanupWebString(
                page.DocumentNode
                    .Element("html")
                    .Element("head")
                    ?.Element("title")
                    ?.InnerText);
        }

        private static AuthorType GetPageAuthor(HtmlNode headerNode)
        {
            var descripNode = headerNode.GetChildWithClass("div", "p-description");
            var authorNode = descripNode?.GetDescendantWithClass("a", "username");
            string authorName = ForumPostTextConverter.CleanupWebString(authorNode?.InnerText.Trim() ?? "");
            return Author.Create(authorName);
        }

        private static int GetMaxPageNumberOfThread(HtmlNode bodyNode)
        {
            var mainNode = bodyNode.GetChildWithClass("div", "p-body-main") ??
                throw new InvalidOperationException("Unable to find p-body-main.");

            var navNode = mainNode.GetDescendantWithClass("nav", "pageNavWrapper");

            if (navNode != null)
            {
                var navItems = navNode.GetDescendantWithClass("ul", "pageNav-main")?.Elements("li").Where(n => n.HasClass("pageNav-page"));

                if (navItems != null && navItems.Any())
                {
                    var lastItem = ForumPostTextConverter.CleanupWebString(navItems.Last().Element("a").InnerText.Trim());

                    if (int.TryParse(lastItem, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int pages))
                    {
                        if (pages == 0)
                            pages = 1;

                        return pages;
                    }
                }
            }

            return 1;
        }
        #endregion Get Page Information

        #region Get ThreadInfoRange information
        private async Task<ThreadRangeType?> TryGetThreadmarksRange(
            Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null || quest.ThreadUri == null)
                return null;

            // Load the threadmarks so that we can find the starting post page or number.
            HtmlDocument? threadmarksPage = await pageProvider.GetHtmlDocumentAsync(
                GetThreadmarksPageUrl(quest.ThreadUri), "Threadmarks",
                CachingMode.UseCache, ShouldCache.Yes,
                SuppressNotifications.No, token).ConfigureAwait(false);

            if (threadmarksPage == null)
                return null;

            var threadmarks = GetThreadmarksListFromPage(threadmarksPage, quest);

            // If there aren't any threadmarks, bail.
            if (!threadmarks.Any())
                return null;

            // Threadmarks have already been filtered, so just pick the last one,
            // and get the URL for the threadmark.
            string lastThreadmarkHref = threadmarks.Last().GetAttributeValue("href", "");

            // Make sure we found something.
            if (string.IsNullOrEmpty(lastThreadmarkHref))
                return null;

            // The threadmark list might use the long version of the URL (including thread info),
            // or the short version (which only shows the post number).

            // If we're given the short version of the URL, just do a HEAD query to get the long version.
            Match mShort = ShortFragment().Match(lastThreadmarkHref);
            if (mShort.Success)
            {
                // Get the post ID for the threadmark
                string tmID = mShort.Groups["tmID"].Value;
                var postId = PostId.Create(tmID);

                if (postId == null)
                    return null;

                // The threadmark href might be a relative path, so make sure to
                // create a proper absolute path to load.
                Uri permalink = GetPermalinkForId(quest.ThreadUri, postId);

                // Attempt to load the threadmark page's headers.  Use cache if available, and cache the result as appropriate.
                string fullUrl = await pageProvider.GetRedirectUrlAsync(
                    permalink.AbsoluteUri, null,
                    CachingMode.BypassCache, ShouldCache.No,
                    SuppressNotifications.Yes, token).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(fullUrl))
                    lastThreadmarkHref = fullUrl;
            }

            // If we have the long URL, we can extract the page number and post number from the URL itself.
            Match m1 = LongFragment().Match(lastThreadmarkHref);
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
                    return ThreadRange.CreateRangeByPost(1);

                var postId = PostId.Create(post);

                // If no page number was found, it's page 1
                if (page == 0)
                    page = 1;

                return ThreadRange.CreateRangeFromPostId(postId, page);
            }

            // Failed to find anything.
            return null;
        }

        private static async Task<ThreadRangeType?> TryGetRSSThreadmarksRange(
            Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null || quest.ThreadUri == null)
                return null;

            if (quest.UseRSSThreadmarks == BoolEx.False)
                return null;

            XDocument? rss = await pageProvider.GetXmlDocumentAsync(
                GetRssThreadmarksUrl(quest.ThreadUri), "Threadmarks",
                CachingMode.UseCache, ShouldCache.Yes,
                SuppressNotifications.No, token).ConfigureAwait(false);

            if (rss == null)
            {
                if (quest.UseRSSThreadmarks == BoolEx.Unknown)
                    quest.UseRSSThreadmarks = BoolEx.False;

                return null;
            }

            if (rss.Root?.Name != "rss")
                return null;

            XElement? channel = rss.Root.Element(XName.Get("channel", ""));

            IEnumerable<XElement> items = channel?.Elements(XName.Get("item", "")) ?? []; ;

            XName titleName = XName.Get("title", "");
            XName pubDate = XName.Get("pubDate", "");

            // Use threadmark filters to filter out unwanted threadmark titles.
            var filteredItems = from item in items
                                let title1 = item.Element(titleName)?.Value
                                let title = title1.StartsWith("Threadmark:") ? title1["Threadmark:".Length..].Trim() : title1
                                where !((quest.UseCustomThreadmarkFilters && (quest.ThreadmarkFilter?.Match(title) ?? false)) ||
                                        (!quest.UseCustomThreadmarkFilters && Filter.DefaultThreadmarkFilter.Match(title)))
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
                    // If we have a permalink fragment, we have no page number, but we can
                    // request a redirect to get the actual href.
                    Match mr = PermalinkFragment().Match(href);
                    if (mr.Success)
                    {
                        string redirect = await pageProvider.GetRedirectUrlAsync(
                            href, "RSS Link", CachingMode.BypassCache, ShouldCache.Yes,
                            SuppressNotifications.Yes, token).ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(redirect) && redirect != href)
                        {
                            href = redirect;
                        }
                    }

                    // If we have the long URL, we can extract the page number and post number from the URL itself.
                    mr = LongFragment().Match(href);
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
                            return ThreadRange.CreateRangeByPost(1);

                        var postId = PostId.Create(post);

                        // If no page number was found, it's page 1
                        if (page == 0)
                            page = 1;

                        return ThreadRange.CreateRangeFromPostId(postId, page);
                    }
                }
            }

            return null;
        }

        private IEnumerable<HtmlNode> GetThreadmarksListFromPage(HtmlDocument threadmarksPage, Quest quest)
        {
            try
            {
                HtmlNode? topNode = GetPageContent(threadmarksPage, PageType.Threadmarks);

                if (topNode == null)
                    return [];

                var threadmarkCat1List = threadmarksPage.GetElementbyId("threadmark-category-1");

                if (threadmarkCat1List != null)
                {
                    var threadmarkDivs = threadmarkCat1List.GetDescendantsWithClass("div", "structItem--threadmark");

                    if (threadmarkDivs != null)
                    {
                        return threadmarkDivs
                            .Select(n => n.GetDescendantWithClass("a", ""))
                            .Where(n => !filterLambda(n))!; // Keep anything the filter returns false for. Guarantee there are no nulls.
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                logger.LogError(e, "Failure when attempting to get the list of threadmarks from the index page. Null list somewhere?");
            }

            return [];

            // Local functions

            // Filter returns true if the item should be removed from consideration.
            bool filterLambda(HtmlNode? n)
            {
                if (n == null)
                    return true;

                if (quest.UseCustomThreadmarkFilters)
                {
                    if (quest.ThreadmarkFilter != null)
                    {
                        return quest.ThreadmarkFilter.Match(n.InnerText);
                    }
                }
                else
                {
                    return Filter.DefaultThreadmarkFilter.Match(n.InnerText);
                }

                return true;
            }
        }
        #endregion Get ThreadInfoRange information

        #region Get Posts
        private static IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
        {
            var top = page.GetElementbyId("top");

            var articles = top.GetDescendantsWithClass("article", "message");

            return articles;
        }

        private PostType? GetPost(HtmlNode article, Quest quest)
        {
            if (article == null)
                return null;

            var id = GetPostId(article);
            var author = GetPostAuthor(article);
            string text = GetPostText(article, quest);
            int number = GetPostNumber(article);

            if (inputOptions.TrackPostAuthorsUniquely)
                author = author with { Name = $"{author.Name}_{id.Id}" };

            var origin = Origin.CreateUser(author, quest.ThreadUri, GetPermalinkForId(quest.ThreadUri, id), id, number);
            var post = Post.Create(origin, text);

            return post;
        }

        private static AuthorType GetPostAuthor(HtmlNode article)
        {
            string authorName = article.GetAttributeValue("data-author", "");
            authorName = ForumPostTextConverter.CleanupWebString(authorName);
            return Author.Create(authorName);
        }

        private static PostIdType GetPostId(HtmlNode article)
        {
            var attribute = article.GetAttributeValue("data-content", "post-");
            var number = attribute["post-".Length..];
            var id = ForumPostTextConverter.CleanupWebString(number);
            return PostId.Create(id) ?? PostId.Zero;
        }

        private static string GetPostText(HtmlNode article, Quest quest)
        {
            // Predicate filtering out elements that we don't want to include
            List<string> excludedClasses = [ "bbCodeQuote", "messageTextEndMarker","advbbcodebar_encadre",
                "advbbcodebar_article", "adv_tabs_wrapper", "adv_slider_wrapper"];
            if (quest.IgnoreSpoilers)
                excludedClasses.Add("bbCodeSpoilerContainer");

            var exclusions = ForumPostTextConverter.GetClassesExclusionPredicate(excludedClasses);

            var articleBody = article.GetDescendantWithClass("article", "message-body")
                ?.GetDescendantsWithClass("div", "bbWrapper").FirstOrDefault();

            Uri host = new(quest.ThreadUri.GetLeftPart(UriPartial.Authority) + "/"); ;

            return ForumPostTextConverter.ExtractPostText(articleBody, exclusions, host);
        }

        private static int GetPostNumber(HtmlNode article)
        {
            var attribution = article.GetDescendantWithClass("header", "message-attribution");

            if (attribution == null)
                return 0;

            string postNum = attribution.Descendants("a").LastOrDefault(c => c.ChildNodes.Count == 1)?.InnerText.Trim() ?? "";

            if (string.IsNullOrEmpty(postNum))
                return 0;

            if (postNum[0] == '#')
            {
                var numSpan = postNum.AsSpan()[1..];

                if (int.TryParse(numSpan, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int number))
                {
                    return number;
                }
            }

            return 0;
        }
        #endregion Get Posts

        #region URL Manipulation
        /// <summary>
        /// Get the URL string up to the end of "...threads/thread.name.12345/"
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
            return $"{GetBaseThreadUrl(uri)}threadmarks#threadmark-category-1";
        }

        private static string GetRssThreadmarksUrl(Uri uri)
        {
            return $"{GetBaseThreadUrl(uri)}threadmarks.rss?threadmark_category_id=1";
        }

        private static Uri GetPermalinkForId(Uri uri, PostIdType postId)
        {
            string url = $"{GetHostBasePostsUrl(uri)}{postId.Id}/";
            return new Uri(url);
        }
        #endregion URL Manipulation

        #region Misc Helper Functions
        private static HtmlNode? GetPageContent(HtmlDocument page, PageType pageType)
        {
            ArgumentNullException.ThrowIfNull(page);

            var contentNode = page.GetElementbyId("top") ??
                throw new InvalidOperationException("Page does not have a content section.");

            var body = contentNode.ParentNode ??
                throw new InvalidOperationException("No body found for the page.");

            string dataTemplate = body.GetAttributeValue("data-template", "");

            switch (pageType)
            {
                case PageType.Thread:
                    if (dataTemplate != "thread_view")
                        throw new InvalidOperationException("This page does not contain a forum thread.");
                    break;
                case PageType.Threadmarks:
                    if (!dataTemplate.Contains("threadmark_list"))
                        throw new InvalidOperationException("This page does not contain threadmarks.");
                    break;
            }

            return contentNode;
        }
        #endregion Misc Helper Functions
    }
}
