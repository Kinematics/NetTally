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
using NetTally.Extensions;
using NetTally.Input.Utility;
using NetTally.Options;
using NetTally.Web;
using NetTally.Types.Enums;
using NetTally.Types.Components;

namespace NetTally.Forums.Adapters2
{
    public class XenForo2Adapter2 : IForumAdapter2
    {
        #region Static data
        // May possibly end with /page-00#post-00
        static readonly Regex longFragment = new Regex(@"threads/[^/]+/(page-(?<page>\d+))?(#post-(?<post>\d+))?$");
        // The short HREF version gives the post ID
        static readonly Regex shortFragment = new Regex(@"posts/(?<tmID>\d+)/?$");
        // RSS permalink does not include the page number.
        static readonly Regex permalinkFragment = new Regex(@"threads/[^/]+/(post-(?<post>\d+))?$");
        #endregion

        #region Constructor
        readonly IGeneralInputOptions inputOptions;
        readonly ILogger<XenForo2Adapter2> logger;

        public XenForo2Adapter2(IGeneralInputOptions inputOptions, ILogger<XenForo2Adapter2> logger)
        {
            this.inputOptions = inputOptions;
            this.logger = logger;
        }
        #endregion

        #region IForumAdapter2 interface
        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns the string to use for a line break event when outputting the tally.</returns>
        public string GetDefaultLineBreak(Uri uri)
        {
            if (uri.Host == "forums.spacebattles.com")
                return "———————————————————————————————————————————————————————";

            return "[hr]——————————————————————————————————————————————[/hr]";
        }

        /// <summary>
        /// Get the default number of posts per page for the site used by the origin.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns a default number of posts per page for the given site.</returns>
        public int GetDefaultPostsPerPage(Uri uri)
        {
            if (uri.Host == "forum.questionablequesting.com")
                return 30;

            return 25;
        }

        /// <summary>
        /// Gets whether the provided URI host is known to use RSS feeds for its threadmarks.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns whether the site is known to use or not use RSS threadmarks.</returns>
        public BoolEx GetHasRssThreadmarksFeed(Uri uri)
        {
            switch (uri.Host)
            {
                case "forums.sufficientvelocity.com":
                case "forums.spacebattles.com":
                    return BoolEx.True;
                case "forum.questionablequesting.com":
                    return BoolEx.False;
                default:
                    return BoolEx.Unknown;
            }
        }

        /// <summary>
        /// Get a proper URL for a specific page of a thread of the URI provided.
        /// </summary>
        /// <param name="uri">The URI of the site that we're constructing a URL for.</param>
        /// <param name="page">The page number to create a URL for.</param>
        /// <returns>Returns a URL for the page requested.</returns>
        public string GetUrlForPage(Quest quest, int page)
        {
            if (page < 1)
                throw new ArgumentException($"Invalid page number: {page}", nameof(page));

            string append = page > 1 ? $"page-{page}" : "";

            return $"{GetBaseThreadUrl(quest.ThreadUri)}{append}";
        }

        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        public ThreadInfo GetThreadInfo(HtmlDocument page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var (headerNode, bodyNode) = GetPageInfoNodes(page);
            string title = GetPageTitle(page, headerNode);
            string author = GetPageAuthor(headerNode);
            int pages = GetMaxPageNumberOfThread(bodyNode);

            ThreadInfo info = new ThreadInfo(title, author, pages);

            return info;
        }

        /// <summary>
        /// Gets the range of post numbers to tally, for the given quest.
        /// This may require loading information from the site.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="pageProvider">The page provider to use to load any needed pages.</param>
        /// <param name="token">The cancellation token to check for cancellation requests.</param>
        /// <returns>Returns a ThreadRangeInfo describing which pages to load for the tally.</returns>
        public async Task<ThreadRangeInfo> GetQuestRangeInfoAsync(Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (pageProvider == null)
                throw new ArgumentNullException(nameof(pageProvider));

            // Use the provided start post if we aren't trying to find the threadmarks.
            if (!quest.CheckForLastThreadmark)
                return new ThreadRangeInfo(true, quest.StartPost);

            var (foundThreadmark, rangeInfo) = await TryGetRSSThreadmarksRange(quest, pageProvider, token);

            if (foundThreadmark)
                return rangeInfo;

            (foundThreadmark, rangeInfo) = await TryGetThreadmarksRange(quest, pageProvider, token);

            if (foundThreadmark)
                return rangeInfo;

            // If we get here, just fall back on default start post.
            return new ThreadRangeInfo(true, quest.StartPost);
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
                return Enumerable.Empty<Post>();

            var posts = from p in GetPostList(page)
                        where p != null
                        let post = GetPost(p, quest)
                        where post != null
                        select post;

            return posts;
        }

        #endregion IForumAdapter2 interface

        #region Get Page Information
        private (HtmlNode headerNode, HtmlNode bodyNode) GetPageInfoNodes(HtmlDocument page)
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

        private string GetPageTitle(HtmlDocument page, HtmlNode headerNode)
        {
            //var titleNode = headerNode.GetChildWithClass("div", "p-title");
            //string title = ForumPostTextConverter.CleanupWebString(titleNode?.Element("h1")?.InnerText.Trim());

            //if (!string.IsNullOrEmpty(title))
            //    return title;

            return ForumPostTextConverter.CleanupWebString(
                page.DocumentNode
                    .Element("html")
                    .Element("head")
                    ?.Element("title")
                    ?.InnerText);
        }

        private string GetPageAuthor(HtmlNode headerNode)
        {
            var descripNode = headerNode.GetChildWithClass("div", "p-description");
            var authorNode = descripNode?.GetDescendantWithClass("a", "username");
            return ForumPostTextConverter.CleanupWebString(authorNode?.InnerText.Trim() ?? "");
        }

        private int GetMaxPageNumberOfThread(HtmlNode bodyNode)
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
        private async Task<(bool found, ThreadRangeInfo rangeInfo)> TryGetThreadmarksRange(
            Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null || quest.ThreadUri == null)
                return (false, ThreadRangeInfo.Empty);

            // Load the threadmarks so that we can find the starting post page or number.
            HtmlDocument? threadmarksPage = await pageProvider.GetHtmlDocumentAsync(
                GetThreadmarksPageUrl(quest.ThreadUri), "Threadmarks",
                CachingMode.UseCache, ShouldCache.Yes,
                SuppressNotifications.No, token).ConfigureAwait(false);

            if (threadmarksPage == null)
                return (false, ThreadRangeInfo.Empty);

            var threadmarks = GetThreadmarksListFromPage(threadmarksPage, quest);

            // If there aren't any threadmarks, bail.
            if (!threadmarks.Any())
                return (false, ThreadRangeInfo.Empty);

            // Threadmarks have already been filtered, so just pick the last one,
            // and get the URL for the threadmark.
            string lastThreadmarkHref = threadmarks.Last().GetAttributeValue("href", "");

            // Make sure we found something.
            if (string.IsNullOrEmpty(lastThreadmarkHref))
                return (false, ThreadRangeInfo.Empty);

            // The threadmark list might use the long version of the URL (including thread info),
            // or the short version (which only shows the post number).

            // If we're given the short version of the URL, just do a HEAD query to get the long version.
            Match mShort = shortFragment.Match(lastThreadmarkHref);
            if (mShort.Success)
            {
                // Get the post ID for the threadmark
                string tmID = mShort.Groups["tmID"].Value;

                // The threadmark href might be a relative path, so make sure to
                // create a proper absolute path to load.
                string permalink = GetPermalinkForId(quest.ThreadUri, tmID);

                // Attempt to load the threadmark page's headers.  Use cache if available, and cache the result as appropriate.
                string fullUrl = await pageProvider.GetRedirectUrlAsync(
                    permalink, null,
                    CachingMode.BypassCache, ShouldCache.No,
                    SuppressNotifications.Yes, token).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(fullUrl))
                    lastThreadmarkHref = fullUrl;
            }

            // If we have the long URL, we can extract the page number and post number from the URL itself.
            Match m1 = longFragment.Match(lastThreadmarkHref);
            if (m1.Success)
            {
                int page = 0;
                int post = 0;

                if (m1.Groups["page"].Success)
                    page = int.Parse(m1.Groups["page"].Value);
                if (m1.Groups["post"].Success)
                    post = int.Parse(m1.Groups["post"].Value);

                // If neither matched, it's post 1/page 1
                // Store 0 in the post ID slot, since we don't know what it is.
                if (page == 0 && post == 0)
                    return (true, new ThreadRangeInfo(true, 1, 1, 0));

                // If no page number was found, it's page 1
                if (page == 0)
                    return (true, new ThreadRangeInfo(false, 0, 1, post));

                // Otherwise, take the provided values.
                return (true, new ThreadRangeInfo(false, 0, page, post));
            }

            // Failed to find anything.
            return (false, ThreadRangeInfo.Empty);
        }

        private async Task<(bool found, ThreadRangeInfo rangeInfo)> TryGetRSSThreadmarksRange(
            Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null || quest.ThreadUri == null)
                return (false, ThreadRangeInfo.Empty);

            if (quest.UseRSSThreadmarks == BoolEx.False)
                return (false, ThreadRangeInfo.Empty);

            XDocument? rss = await pageProvider.GetXmlDocumentAsync(
                GetRssThreadmarksUrl(quest.ThreadUri), "Threadmarks",
                CachingMode.UseCache, ShouldCache.Yes,
                SuppressNotifications.No, token).ConfigureAwait(false);

            if (rss == null)
            {
                if (quest.UseRSSThreadmarks == BoolEx.Unknown)
                    quest.UseRSSThreadmarks = BoolEx.False;

                return (false, ThreadRangeInfo.Empty);
            }

            if (rss.Root?.Name != "rss")
                return (false, ThreadRangeInfo.Empty);

            XElement? channel = rss.Root.Element(XName.Get("channel", ""));

            IEnumerable<XElement> items = channel?.Elements(XName.Get("item", "")) ?? Enumerable.Empty<XElement>(); ;

            XName titleName = XName.Get("title", "");
            XName pubDate = XName.Get("pubDate", "");

            // Use threadmark filters to filter out unwanted threadmark titles.
            var filteredItems = from item in items
                                let title1 = item.Element(titleName)?.Value
                                let title = title1.StartsWith("Threadmark:") ? title1.Substring("Threadmark:".Length).Trim() : title1
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
                    Match mr = permalinkFragment.Match(href);
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
                    mr = longFragment.Match(href);
                    if (mr.Success)
                    {
                        int page = 0;
                        int post = 0;

                        if (mr.Groups["page"].Success)
                            page = int.Parse(mr.Groups["page"].Value);
                        if (mr.Groups["post"].Success)
                            post = int.Parse(mr.Groups["post"].Value);

                        // If neither matched, it's post 1/page 1
                        // Store 0 in the post ID slot, since we don't know what it is.
                        if (page == 0 && post == 0)
                            return (true, new ThreadRangeInfo(true, 1, 1, 0));

                        // If no page number was found, it's page 1
                        if (page == 0)
                            return (true, new ThreadRangeInfo(false, 0, 1, post));

                        // Otherwise, take the provided values.
                        return (true, new ThreadRangeInfo(false, 0, page, post));
                    }
                }
            }

            return (false, ThreadRangeInfo.Empty);
        }

        private IEnumerable<HtmlNode> GetThreadmarksListFromPage(HtmlDocument threadmarksPage, Quest quest)
        {
            try
            {
                HtmlNode? topNode = GetPageContent(threadmarksPage, PageType.Threadmarks);

                if (topNode == null)
                    return Enumerable.Empty<HtmlNode>();

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

            return Enumerable.Empty<HtmlNode>();

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
        private IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
        {
            var top = page.GetElementbyId("top");

            var articles = top.GetDescendantsWithClass("article", "message");

            return articles;
        }

        private Post? GetPost(HtmlNode article, Quest quest)
        {
            if (article == null)
                return null;

            string author = GetPostAuthor(article);
            string id = GetPostId(article);
            string text = GetPostText(article, quest);
            int number = GetPostNumber(article);

            if (inputOptions.TrackPostAuthorsUniquely)
                author = $"{author}_{id}";

            try
            {
                Origin origin = new Origin(author, id, number, quest.ThreadUri, GetPermalinkForId(quest.ThreadUri, id));
                return new Post(origin, text);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Attempt to create new post failed. (Author:{author}, ID:{id}, Number:{number}, Quest:{quest.DisplayName})");
            }

            return null;
        }

        private string GetPostAuthor(HtmlNode article)
        {
            return ForumPostTextConverter.CleanupWebString(article.GetAttributeValue("data-author", ""));
        }

        private string GetPostId(HtmlNode article)
        {
            return ForumPostTextConverter.CleanupWebString(article.GetAttributeValue("data-content", "post-")
                                                                  .Substring("post-".Length));
        }

        private string GetPostText(HtmlNode article, Quest quest)
        {
            // Predicate filtering out elements that we don't want to include
            List<string> excludedClasses = new List<string> { "bbCodeQuote", "messageTextEndMarker","advbbcodebar_encadre",
                "advbbcodebar_article", "adv_tabs_wrapper", "adv_slider_wrapper"};
            if (quest.IgnoreSpoilers)
                excludedClasses.Add("bbCodeSpoilerContainer");

            var exclusions = ForumPostTextConverter.GetClassesExclusionPredicate(excludedClasses);

            var articleBody = article.GetDescendantWithClass("article", "message-body")
                ?.GetDescendantsWithClass("div", "bbWrapper").FirstOrDefault();

            Uri host = new Uri(quest.ThreadUri.GetLeftPart(UriPartial.Authority) + "/"); ;

            return ForumPostTextConverter.ExtractPostText(articleBody, exclusions, host);
        }

        private int GetPostNumber(HtmlNode article)
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
        private string GetBaseThreadUrl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            StringBuilder sb = new StringBuilder();

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

            if (sb[sb.Length - 1] != '/')
                sb.Append('/');

            return sb.ToString();
        }

        /// <summary>
        /// Gets the URL string up to the end of ".../posts/"
        /// It's generated at the same level as the ".../threads/" directory.
        /// </summary>
        /// <param name="uri">The URI to derive the URL from.</param>
        /// <returns>Returns a string containing the URL up to the posts directory.</returns>
        private string GetHostBasePostsUrl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            StringBuilder sb = new StringBuilder();

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

        private string GetThreadmarksPageUrl(Uri uri)
        {
            return $"{GetBaseThreadUrl(uri)}threadmarks#threadmark-category-1";
        }

        private string GetRssThreadmarksUrl(Uri uri)
        {
            return $"{GetBaseThreadUrl(uri)}threadmarks.rss?threadmark_category_id=1";
        }

        private string GetPermalinkForId(Uri uri, string postId)
        {
            return $"{GetHostBasePostsUrl(uri)}{postId}/";
        }
        #endregion URL Manipulation

        #region Misc Helper Functions
        private HtmlNode? GetPageContent(HtmlDocument page, PageType pageType)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var contentNode = page.GetElementbyId("top");

            if (contentNode == null)
                throw new InvalidOperationException("Page does not have a content section.");

            var body = contentNode.ParentNode;

            if (body == null)
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
