using System;
using System.Collections.Generic;
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

namespace NetTally.Forums.Adapters2
{
    public class XenForo1Adapter2 : IForumAdapter2
    {
        #region Static data
        // May possibly end with /page-00#post-00
        static readonly Regex longFragment = new Regex(@"threads/[^/]+/(page-(?<page>\d+))?(#post-(?<post>\d+))?$");
        // The short HREF version gives the post ID
        static readonly Regex shortFragment = new Regex(@"posts/(?<tmID>\d+)/?$");
        #endregion

        #region Constructor
        readonly IGeneralInputOptions inputOptions;
        readonly ILogger<XenForo1Adapter2> logger;

        public XenForo1Adapter2(IGeneralInputOptions inputOptions, ILogger<XenForo1Adapter2> logger)
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
        public string GetUrlForPage(IQuest quest, int page)
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

            string title = GetPageTitle(page);
            string author = GetPageAuthor(page);
            int pages = GetMaxPageNumberOfThread(page);

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
        public async Task<ThreadRangeInfo> GetQuestRangeInfoAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token)
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
        public IEnumerable<Post> GetPosts(HtmlDocument page, IQuest quest, int pageNumber)
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
        private string GetPageTitle(HtmlDocument page)
        {
            return ForumPostTextConverter.CleanupWebString(
                page.DocumentNode
                    .Element("html")
                    .Element("head")
                    ?.Element("title")
                    ?.InnerText);
        }

        private string GetPageAuthor(HtmlDocument page)
        {
            // Find a common parent for other data
            HtmlNode pageContent = GetPageContent(page, PageType.Thread);

            if (pageContent == null)
                throw new InvalidOperationException("Cannot find content on page.");

            // Non-thread pages (such as threadmark pages) won't have a title bar.
            HtmlNode titleBar = pageContent.GetDescendantWithClass("titleBar") ??
                throw new InvalidOperationException("Not a valid forum thread.");

            // Find the thread author
            HtmlNode authorNode = page.GetElementbyId("pageDescription")?.GetChildWithClass("username");

            return ForumPostTextConverter.CleanupWebString(authorNode?.InnerText ?? "");
        }

        private int GetMaxPageNumberOfThread(HtmlDocument page)
        {
            // Find a common parent for other data
            HtmlNode pageContent = GetPageContent(page, PageType.Thread);

            if (pageContent == null)
                throw new InvalidOperationException("Cannot find content on page.");

            // Find the number of pages in the thread
            var pageNavLinkGroup = pageContent.GetDescendantWithClass("div", "pageNavLinkGroup");
            var pageNav = pageNavLinkGroup?.GetChildWithClass("PageNav");
            string lastPage = pageNav?.GetAttributeValue("data-last", "") ?? "";

            if (string.IsNullOrEmpty(lastPage))
                return 1;
            else
                return Int32.Parse(lastPage);
        }
        #endregion Get Page Information

        #region Get ThreadInfoRange information
        private async Task<(bool found, ThreadRangeInfo rangeInfo)> TryGetThreadmarksRange(
            IQuest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null)
                return (false, ThreadRangeInfo.Empty);

            // Load the threadmarks so that we can find the starting post page or number.
            HtmlDocument threadmarksPage = await pageProvider.GetHtmlDocumentAsync(
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
            IQuest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null || quest.ThreadUri == null)
                return (false, ThreadRangeInfo.Empty);

            if (quest.UseRSSThreadmarks == BoolEx.False)
                return (false, ThreadRangeInfo.Empty);

            XDocument rss = await pageProvider.GetXmlDocumentAsync(
                GetRssThreadmarksUrl(quest.ThreadUri), "Threadmarks",
                CachingMode.UseCache, ShouldCache.Yes,
                SuppressNotifications.No, token).ConfigureAwait(false);

            if (rss == null)
            {
                if (quest.UseRSSThreadmarks == BoolEx.Unknown)
                    quest.UseRSSThreadmarks = BoolEx.False;

                return (false, ThreadRangeInfo.Empty);
            }

            if (rss.Root.Name != "rss")
                return (false, ThreadRangeInfo.Empty);

            var channel = rss.Root.Element(XName.Get("channel", ""));

            var items = channel.Elements(XName.Get("item", ""));

            XName titleName = XName.Get("title", "");
            XName pubDate = XName.Get("pubDate", "");

            // Use threadmark filters to filter out unwanted threadmark titles.
            var filteredItems = from item in items
                                let title = item.Element(titleName).Value
                                where !((quest.UseCustomThreadmarkFilters && (quest.ThreadmarkFilter?.Match(title) ?? false)) ||
                                        (!quest.UseCustomThreadmarkFilters && Filter.DefaultThreadmarkFilter.Match(title)))
                                let pub = item.Element(pubDate).Value
                                where string.IsNullOrEmpty(pub) == false
                                let pubStamp = DateTime.Parse(pub)
                                orderby pubStamp descending // Most recent is first
                                select item;

            // Take the first (most recent) item from the list.
            var recentItem = filteredItems.FirstOrDefault();

            if (recentItem != null)
            {
                string href = recentItem.Element(XName.Get("link", "")).Value;

                // If we have the long URL, we can extract the page number and post number from the URL itself.
                Match mr = longFragment.Match(href);
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

            return (false, ThreadRangeInfo.Empty);
        }

        private IEnumerable<HtmlNode> GetThreadmarksListFromPage(HtmlDocument threadmarksPage, IQuest quest)
        {
            try
            {
                HtmlNode content = GetPageContent(threadmarksPage, PageType.Threadmarks);

                HtmlNode threadmarksDiv = content?.GetDescendantWithClass("div", "threadmarks");

                HtmlNode listOfThreadmarks = null;

                HtmlNode threadmarkList = threadmarksDiv?.GetDescendantWithClass("threadmarkList");

                if (threadmarkList != null)
                {
                    // We have a .threadmarkList node.  This is either an ol itself, or it will contain a ThreadmarkCategory_# ol node.  We want category 1.

                    if (threadmarkList.Name == "ol")
                    {
                        if (threadmarkList.GetAttributeValue("class", "").Contains("ThreadmarkCategory"))
                        {
                            if (!threadmarkList.HasClass("ThreadmarkCategory_1"))
                                return Enumerable.Empty<HtmlNode>();
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
                    listOfThreadmarks = threadmarksDiv?.Descendants("ul").FirstOrDefault(e => e.Elements("li").Any(a => a.HasClass("threadmarkItem")));
                }

                if (listOfThreadmarks != null)
                {
                    Func<HtmlNode, bool> filterLambda = (n) => n != null &&
                        ((quest.UseCustomThreadmarkFilters && (quest.ThreadmarkFilter?.Match(n.InnerText) ?? false)) ||
                        (!quest.UseCustomThreadmarkFilters && Filter.DefaultThreadmarkFilter.Match(n.InnerText)));

                    Func<HtmlNode, HtmlNode> nodeSelector = (n) => n.Element("a");

                    Func<HtmlNode, IEnumerable<HtmlNode>> childSelector = (i) => i.Element("ul")?.Elements("li") ?? new List<HtmlNode>();

                    var results = listOfThreadmarks.Elements("li").TraverseList(childSelector, nodeSelector, filterLambda);

                    return results;
                }
            }
            catch (ArgumentNullException e)
            {
                logger.LogError(e, "Failure when attempting to get the list of threadmarks from the index page. Null list somewhere?");
            }

            return Enumerable.Empty<HtmlNode>();
        }
        #endregion Get ThreadInfoRange information

        #region Get Posts
        private IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
        {
            // The ordered list containing all messages.
            var messageList = page?.GetElementbyId("messageList");

            // Return all found list items in the message list, or an empty list.
            return messageList?.Elements("li") ?? Enumerable.Empty<HtmlNode>();
        }

        private Post GetPost(HtmlNode li, IQuest quest)
        {
            if (li == null)
                return null;

            string author = GetPostAuthor(li);
            string id = GetPostId(li);
            string text = GetPostText(li, quest);
            int number = GetPostNumber(li);

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

        private string GetPostAuthor(HtmlNode li)
        {
            return ForumPostTextConverter.CleanupWebString(li.GetAttributeValue("data-author", ""));
        }

        private string GetPostId(HtmlNode li)
        {
            return li.Id.Substring("post-".Length);
        }

        private string GetPostText(HtmlNode li, IQuest quest)
        {
            // Get the primary content of the list item
            HtmlNode primaryContent = li.GetChildWithClass("primaryContent");

            // On one branch, we can get the post text
            HtmlNode messageContent = primaryContent?.GetChildWithClass("messageContent");
            HtmlNode postBlock = messageContent?.Element("article")?.Element("blockquote");

            List<string> excludedClasses = new List<string> { "bbCodeQuote", "messageTextEndMarker","advbbcodebar_encadre",
                "advbbcodebar_article", "adv_tabs_wrapper", "adv_slider_wrapper"};
            if (quest.IgnoreSpoilers)
                excludedClasses.Add("bbCodeSpoilerContainer");

            // Predicate for filtering out elements that we don't want to include
            var exclusions = ForumPostTextConverter.GetClassesExclusionPredicate(excludedClasses);

            Uri host = new Uri(quest.ThreadUri.GetLeftPart(UriPartial.Authority) + "/"); ;

            // Get the full post text.
            return ForumPostTextConverter.ExtractPostText(postBlock, exclusions, host);
        }

        private int GetPostNumber(HtmlNode li)
        {
            // Get the primary content of the list item
            HtmlNode primaryContent = li.GetChildWithClass("primaryContent");

            // On another branch of the primary content, we can get the post number.
            HtmlNode messageMeta = primaryContent?.GetChildWithClass("messageMeta");
            // HTML parsing of the post was corrupted somehow.
            if (messageMeta == null)
            {
                return 0;
            }
            HtmlNode publicControls = messageMeta.GetChildWithClass("publicControls");
            HtmlNode postNumber = publicControls?.GetChildWithClass("postNumber");

            if (postNumber == null)
                return 0;

            string postNumberText = postNumber.InnerText;
            // Skip the leading # character.
            if (postNumberText.StartsWith("#", StringComparison.Ordinal))
                postNumberText = postNumberText.Substring(1);

            return int.Parse(postNumberText);
        }
        #endregion Get Posts

        #region URL Manipulation
        /// <summary>
        /// Get the URL string up to the end of ".../threads/thread.name.12345/"
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
            return $"{GetBaseThreadUrl(uri)}threadmarks?category_id=1";
        }

        private string GetRssThreadmarksUrl(Uri uri)
        {
            return $"{GetBaseThreadUrl(uri)}threadmarks.rss?category_id=1";
        }

        private string GetPermalinkForId(Uri uri, string postId)
        {
            return $"{GetHostBasePostsUrl(uri)}{postId}/";
        }
        #endregion URL Manipulation

        #region Misc Helper Functions
        private HtmlNode GetPageContent(HtmlDocument page, PageType pageType)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var contentNode = page.GetElementbyId("content");

            if (contentNode == null)
                throw new InvalidOperationException("Page does not have a content section.");

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
}
