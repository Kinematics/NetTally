using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using NetTally.Extensions;
using NetTally.Input.Utility;
using NetTally.Options;
using NetTally.Web;

namespace NetTally.Forums.Adapters
{
    /// <summary>
    /// Class for extracting data from XenForo forum threads.
    /// </summary>
    class XenForo2Adapter : IForumAdapter1
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The URI of the thread this adapter will be handling.</param>
        public XenForo2Adapter(Uri site)
        {
            this.site = site;
            (Host, BaseSite, ThreadName) = GetSiteData(site);
        }
        #endregion

        #region Regexes and stuff
        // Regex for forum URL syntax.
        static readonly Regex siteRegex = new Regex(@"^(?!.*\s)((?<base>https?://[^/]+/([^/]+/)*)threads/)?(?<thread>[^/]+\.\d+(?=/|$))");
        // May possibly end with /page-00#post-00
        static readonly Regex longFragment = new Regex(@"threads/[^/]+/(page-(?<page>\d+))?(#post-(?<post>\d+))?$");
        // The short HREF version gives the post ID
        static readonly Regex shortFragment = new Regex(@"posts/(?<tmID>\d+)/?$");
        // RSS permalink does not include the page number.
        static readonly Regex permalinkFragment = new Regex(@"threads/[^/]+/(post-(?<post>\d+))?$");

        // The default threadmark filter.
        static readonly Filter DefaultThreadmarkFilter = new Filter(Quest.OmakeFilter, null);
        #endregion

        #region Site properties
        /// <summary>
        /// Backing property for site URI.
        /// </summary>
        Uri site;

        /// <summary>
        /// Property for the site this adapter is handling.
        /// Can be changed if the quest thread details are changed.
        /// </summary>
        public Uri Site
        {
            get
            {
                return site;
            }
            set
            {
                // Get site data (which may throw an exception) before attempting
                // to set the value of the backing field.
                (Host, BaseSite, ThreadName) = GetSiteData(value);
                site = value;
            }
        }

        /// <summary>
        /// Given a site URI (on construction, or when setting the property), extract out
        /// the relevant host, base site, and thread name values.
        /// </summary>
        /// <param name="siteUri">A URI for the quest thread.</param>
        /// <returns>Returns a tuple of the relevalt components.</returns>
        /// <exception cref="ArgumentException">Throws an exception if the provided URI does not
        /// match the expected format for XenForo forums.</exception>
        private (Uri host, string baseSite, string threadName) GetSiteData(Uri siteUri)
        {
            Match m = siteRegex.Match(siteUri.AbsoluteUri);
            if (m.Success)
            {
                // Default to SV if no host information is provided in the Uri.
                string baseSite = "https://forums.sufficientvelocity.com/";

                if (m.Groups["base"].Success)
                    baseSite = m.Groups["base"].Value;

                Uri host = new Uri(baseSite);
                string threadName = m.Groups["thread"].Value;

                return (host, baseSite, threadName);
            }
            else
            {
                throw new ArgumentException($"Invalid XenForo site URL format:\n{siteUri.AbsoluteUri}");
            }
        }

        Uri Host { get; set; }
        string BaseSite { get; set; }
        string ThreadName { get; set; }
        string ThreadBaseUrl => $"{BaseSite}threads/{ThreadName}/";
        string PostsBaseUrl => $"{BaseSite}posts/";
        string ThreadmarksUrl => $"{ThreadBaseUrl}threadmarks#threadmark-category-1";
        string ThreadmarksRSSUrl => $"{ThreadBaseUrl}threadmarks.rss?threadmark_category_id=1";

        #endregion

        #region Public Interface
        /// <summary>
        /// Get the default number of posts per page for this forum.
        /// </summary>
        public int DefaultPostsPerPage
        {
            get
            {
                switch (Site.Host)
                {
                    case "forum.questionablequesting.com":
                        return 30;
                    default:
                        return 25;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has RSS threadmarks.
        /// </summary>
        public BoolEx HasRSSThreadmarks
        {
            get
            {
                switch (Site.Host)
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
        }

        /// <summary>
        /// Generate a URL to access the specified page of the adapter's thread.
        /// </summary>
        /// <param name="page">The page of the thread that is being loaded.</param>
        /// <returns>Returns a URL formatted to load the requested page of the thread.</returns>
        public string GetUrlForPage(int page, int postsPerPage)
        {
            if (page < 1)
                throw new ArgumentOutOfRangeException(nameof(page), page, "Invalid page number.");

            if (page == 1)
                return ThreadBaseUrl;
            else
                return $"{ThreadBaseUrl}page-{page}";
        }

        /// <summary>
        /// Generate a URL to access the specified post of the adapter's thread.
        /// </summary>
        /// <param name="postId">The permalink ID of the post being requested.</param>
        /// <returns>Returns a URL formatted to load the requested post.</returns>
        public string GetPermalinkForId(string postId) => $"{PostsBaseUrl}{postId}";

        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        public ThreadInfo GetThreadInfo(HtmlDocument page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            string title;
            string author;
            int pages = 0;

            HtmlNode doc = page.DocumentNode;


            // Start at the top of the structure

            var topNode = page.GetElementbyId("top");

            var bodyNode = topNode.GetChildWithClass("div", "p-body") ??
                throw new InvalidOperationException("Unable to find p-body.");

            if (bodyNode.Elements("div").Any(n => n.HasClass("p-body-inner")))
            {
                bodyNode = bodyNode.GetChildWithClass("p-body-inner")!;
            }

            var headerNode = bodyNode.GetChildWithClass("div", "p-body-header") ??
                throw new InvalidOperationException("Unable to find p-body-header.");

            {
                var titleNode = headerNode.GetChildWithClass("div", "p-title");
                title = ForumPostTextConverter.CleanupWebString(titleNode?.Element("h1")?.InnerText.Trim());

                var descripNode = headerNode.GetChildWithClass("div", "p-description");
                var authorNode = descripNode?.GetDescendantWithClass("a", "username");
                author = ForumPostTextConverter.CleanupWebString(authorNode?.InnerText.Trim() ?? "");
            }

            var mainNode = bodyNode.GetChildWithClass("div", "p-body-main") ??
                throw new InvalidOperationException("Unable to find p-body-main.");

            var navNode = mainNode.GetDescendantWithClass("nav", "pageNavWrapper");

            if (navNode != null)
            {
                var navItems = navNode.GetDescendantWithClass("ul", "pageNav-main")?.Elements("li").Where(n => n.HasClass("pageNav-page"));

                if (navItems != null && navItems.Any())
                {
                    var lastItem = ForumPostTextConverter.CleanupWebString(navItems.Last().Element("a").InnerText.Trim());

                    _ = int.TryParse(lastItem, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out pages);
                }
            }

            if (pages == 0)
                pages = 1;

            // Create a ThreadInfo object to hold the acquired information.
            ThreadInfo info = new ThreadInfo(title, author, pages);

            return info;
        }

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        public IEnumerable<Post> GetPosts(HtmlDocument page, IQuest quest)
        {
            var posts = from p in GetPostsList(page)
                            //where p.HasClass("stickyFirstContainer") == false
                        let post = GetPost(p, quest)
                        where post != null
                        select post;

            return posts;
        }

        /// <summary>
        /// Get the starting post number, to begin tallying from.  Consult the
        /// threadmarks list if the quest allows it.  Returns a value that's
        /// either a post number or a post ID (along with page number) to start from.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="pageProvider">A page provider to allow loading the threadmark page.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>Returns data indicating where to begin tallying the thread.</returns>
        public async Task<ThreadRangeInfo> GetStartingPostNumberAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (pageProvider == null)
                throw new ArgumentNullException(nameof(pageProvider));

            // Use the provided start post if we aren't trying to find the threadmarks.
            if (!quest.CheckForLastThreadmark)
                return new ThreadRangeInfo(true, quest.StartPost);

            // Attempt to use the RSS feed for threadmarks loading.
            if (quest.UseRSSThreadmarks == BoolEx.True)
            {
                // try for RSS stream
                XDocument? rss = await pageProvider.GetXmlDocumentAsync(ThreadmarksRSSUrl, "Threadmarks", CachingMode.UseCache,
                    ShouldCache.Yes, SuppressNotifications.No, token).ConfigureAwait(false);

                if (rss != null && rss.Root.Name == "rss")
                {
                    XName channelName = XName.Get("channel", "");

                    var channel = rss.Root.Element(channelName);

                    XName itemName = XName.Get("item", "");

                    var items = channel.Elements(itemName);

                    XName titleName = XName.Get("title", "");
                    XName pubDate = XName.Get("pubDate", "");

                    List<(XElement item, DateTime timestamp)> filterList = new List<(XElement, DateTime)>();

                    foreach (var item in items)
                    {
                        var title = item.Element(titleName).Value;

                        if (title.StartsWith("Threadmark: "))
                        {
                            title = title.Substring(12);
                        }
                        else if (title.StartsWith("Threadmark:"))
                        {
                            title = title.Substring(11);
                        }

                        if (quest.UseCustomThreadmarkFilters)
                        {
                            if (quest.ThreadmarkFilter.Match(title))
                                continue;
                        }
                        else
                        {
                            if (DefaultThreadmarkFilter.Match(title))
                                continue;
                        }

                        var pub = item.Element(pubDate).Value;

                        if (string.IsNullOrEmpty(pub))
                            continue;

                        var pubStamp = DateTime.Parse(pub);

                        filterList.Add((item, pubStamp));
                    }

                    if (filterList.Any())
                    {
                        var orderedList = filterList.OrderBy(a => a.timestamp);

                        var pick = orderedList.Last().item;

                        XName linkName = XName.Get("link", "");

                        var link = pick.Element(linkName);

                        string href = link.Value;

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
                            int postID = 0;

                            if (mr.Groups["page"].Success)
                                page = int.Parse(mr.Groups["page"].Value);
                            if (mr.Groups["post"].Success)
                                postID = int.Parse(mr.Groups["post"].Value);

                            // If neither matched, it's post 1/page 1
                            // Store 0 in the post ID slot, since we don't know what it is.
                            if (page == 0 && postID == 0)
                                return new ThreadRangeInfo(true, 1, 1, 0);

                            // If no page number was found, it's page 1
                            if (page == 0)
                            {
                                return new ThreadRangeInfo(false, 0, 1, postID);
                            }

                            // Otherwise, take the provided values.
                            return new ThreadRangeInfo(false, 0, page, postID);
                        }
                    }
                }
            }

            // If RSS read was successful, then the function will have ended.  If not, we continue with the next method.

            // Load the threadmarks so that we can find the starting post page or number.
            HtmlDocument? threadmarkPage = await pageProvider.GetHtmlDocumentAsync(ThreadmarksUrl, "Threadmarks", CachingMode.UseCache,
                ShouldCache.Yes, SuppressNotifications.No, token).ConfigureAwait(false);

            if (threadmarkPage == null)
                return new ThreadRangeInfo(true, quest.StartPost);

            var threadmarks = GetThreadmarksListFromIndex(quest, threadmarkPage);

            // If there aren't any threadmarks, fall back on the normal start post.
            if (!threadmarks.Any())
                return new ThreadRangeInfo(true, quest.StartPost);

            // Threadmarks have already been filtered, so just pick the last one.
            var lastThreadmark = threadmarks.Last();

            // And get the URL for the threadmark.
            string threadmarkHref = lastThreadmark.GetAttributeValue("href", "");

            // The threadmark list might use the long version of the URL (including thread info),
            // or the short version (which only shows the post number).

            // If we're given the short version of the URL, just do a HEAD query to get the long version.
            Match mShort = shortFragment.Match(threadmarkHref);
            if (mShort.Success)
            {
                // Get the post ID for the threadmark
                string tmID = mShort.Groups["tmID"].Value;

                // The threadmark href might be a relative path, so make sure to
                // create a proper absolute path to load.
                string permalink = GetPermalinkForId(tmID);

                // Attempt to load the threadmark page's headers.  Use cache if available, and cache the result as appropriate.
                string fullUrl = await pageProvider.GetRedirectUrlAsync(permalink, null, CachingMode.BypassCache, ShouldCache.No, SuppressNotifications.Yes, token).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(fullUrl))
                    threadmarkHref = fullUrl;
            }

            // If we have the long URL, we can extract the page number and post number from the URL itself.
            Match m1 = longFragment.Match(threadmarkHref);
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
                    return new ThreadRangeInfo(true, 1, 1, 0);

                // If no page number was found, it's page 1
                if (page == 0)
                    return new ThreadRangeInfo(false, 0, 1, post);

                // Otherwise, take the provided values.
                return new ThreadRangeInfo(false, 0, page, post);
            }

            // If we can't figure out how to get the threadmark's page from the threadmark,
            // just fall back on the given start post.
            return new ThreadRangeInfo(true, quest.StartPost);
        }

        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        public string LineBreak
        {
            get
            {
                if (Site.Host == "forums.spacebattles.com")
                    return "———————————————————————————————————————————————————————";

                return "[hr]——————————————————————————————————————————————[/hr]";
            }
        }

        /// <summary>
        /// Gets the value of post identifier.
        /// </summary>
        /// <param name="postID">The post identifier.</param>
        /// <returns>Returns the numeric value of the post identifier.</returns>
        public Int64 GetValueOfPostID(string postID)
        {
            if (Int64.TryParse(postID, out long result))
                return result;

            return 0;
        }

        #endregion

        #region Static support functions
        /// <summary>
        /// Find the div with the 'content' ID contained in the top-level document node.
        /// Make sure this div is appropriate for the type of page we're looking at.
        /// </summary>
        /// <param name="doc">An HTML document page.</param>
        /// <returns>Returns the node that holds the page content, if found.</returns>
        static HtmlNode GetPageContent(HtmlDocument doc, PageType pageType)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var contentNode = doc.GetElementbyId("top");

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

        /// <summary>
        /// Gets a list of HtmlNodes for the posts found on the provided page.
        /// Returns an empty list on any failures.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns a list of any found posts.</returns>
        static IEnumerable<HtmlNode> GetPostsList(HtmlDocument page)
        {
            var top = page.GetElementbyId("top");

            var articles = top.GetDescendantsWithClass("article", "message");

            return articles;
        }

        /// <summary>
        /// Get a completed post from the provided HTML list item node.
        /// </summary>
        /// <param name="article">List item node that contains the post.</param>
        /// <returns>Returns a post object with required information.</returns>
        private Post? GetPost(HtmlNode article, IQuest quest)
        {
            if (article == null)
                throw new ArgumentNullException(nameof(article));

            string author;
            string id;
            string text;
            int number;

            // Author and ID are in the basic list item attributes
            author = ForumPostTextConverter.CleanupWebString(article.GetAttributeValue("data-author", ""));
            id = ForumPostTextConverter.CleanupWebString(article.GetAttributeValue("data-content", "post-").Substring("post-".Length));

            if (AdvancedOptions.Instance.DebugMode)
                author = $"{author}_{id}";

            var attribution = article.GetDescendantWithClass("header", "message-attribution");

            if (attribution == null)
                return null;

            string postNum = attribution.Descendants("a").LastOrDefault(c => c.ChildNodes.Count == 1)?.InnerText.Trim() ?? "";

            if (string.IsNullOrEmpty(postNum))
                return null;


            if (postNum[0] == '#')
            {
                var numSpan = postNum.AsSpan()[1..];

                if (!int.TryParse(numSpan, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out number))
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            // Predicate filtering out elements that we don't want to include
            List<string> excludedClasses = new List<string> { "bbCodeQuote", "messageTextEndMarker","advbbcodebar_encadre",
                "advbbcodebar_article", "adv_tabs_wrapper", "adv_slider_wrapper"};
            if (quest.IgnoreSpoilers)
                excludedClasses.Add("bbCodeSpoilerContainer");

            var exclusions = ForumPostTextConverter.GetClassesExclusionPredicate(excludedClasses);

            var articleBody = article.GetDescendantWithClass("article", "message-body")?.GetChildWithClass("div", "bbWrapper");

            text = ForumPostTextConverter.ExtractPostText(articleBody, exclusions, Host);


            Post? post;
            try
            {
                Origin origin = new Origin(author, id, number, Site, GetPermalinkForId(id));
                post = new Post(origin, text);
            }
            catch (Exception e)
            {
                Logger.Error($"Attempt to create new post failed. (Author:{author}, ID:{id}, Number:{number}, Quest:{quest.DisplayName})", e);
                post = null;
            }

            return post;
        }

        /// <summary>
        /// Gets the list of threadmarks from an index page.
        /// Handle nested threadmarks.
        /// If the page doesn't have any threadmarks, return an empty list.
        /// Runs a filter (ThreadmarksFilter class) on any threadmark titles.
        /// Any that don't pass aren't included in the list.
        /// </summary>
        /// <param name="quest">The quest.</param>
        /// <param name="page">The index page of the threadmarks.</param>
        /// <returns>Returns a list of anchor tags for all threadmarks, after filtering.</returns>
        static IEnumerable<HtmlNode> GetThreadmarksListFromIndex(IQuest quest, HtmlDocument page)
        {
            try
            {
                HtmlNode topNode = GetPageContent(page, PageType.Threadmarks);

                var threadmarkCat1List = page.GetElementbyId("threadmark-category-1");

                if (threadmarkCat1List != null)
                {
                    var threadmarkDivs = threadmarkCat1List.GetDescendantsWithClass("div", "structItem--threadmark");

                    if (threadmarkDivs != null)
                    {
                        return threadmarkDivs
                            .Select(n => n.GetDescendantWithClass("a", ""))
                            .Where(n => !filterLambda(n))!; // Keep anything the filter returns false for. Guarantee there are no nulls.

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
                                return DefaultThreadmarkFilter.Match(n.InnerText);
                            }

                            return true;
                        }
                    }
                }
            }
            catch (ArgumentNullException e)
            {
                Logger.Error("Failure when attempting to get the list of threadmarks from the index page. Null list somewhere?", e);
            }

            return Enumerable.Empty<HtmlNode>();
        }
        #endregion

        #region Detection
        /// <summary>
        /// Static detection of whether the provided web page is a XenForo forum thread.
        /// </summary>
        /// <param name="page">Web page to examine.</param>
        /// <returns>Returns true if it's detected as a XenForo2 page.  Otherwise, false.</returns>
        public static bool CanHandlePage(HtmlDocument page)
        {
            if (page == null)
                return false;

            return (page.DocumentNode.Element("html").Id == "XF");
        }
        #endregion

    }
}
