using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;
using NetTally.Extensions;
using NetTally.Utility;
using NetTally.Web;

namespace NetTally.Forums.Adapters
{
    /// <summary>
    /// Class for extracting data from XenForo forum threads.
    /// </summary>
    class XenForo1Adapter : IForumAdapter
    {
        // May possibly end with /page-00#post-00
        static readonly Regex longFragment = new Regex(@"threads/[^/]+/(page-(?<page>\d+))?(#post-(?<post>\d+))?$");
        // The short HREF version gives the post ID
        static readonly Regex shortFragment = new Regex(@"posts/(?<tmID>\d+)/?$");

        // The default threadmark filter.
        static Filter DefaultThreadmarkFilter = new Filter(Quest.OmakeFilter, null);


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The URI of the thread this adapter will be handling.</param>
        public XenForo1Adapter(Uri site)
        {
            Site = site;
        }

        #region Site properties
        Uri site;
        static readonly Regex siteRegex = new Regex(@"^(?!.*\s)((?<base>https?://[^/]+/([^/]+/)*)threads/)?(?<thread>[^/]+\.\d+(?=/|$))");

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
                site = value ?? throw new ArgumentNullException(nameof(Site));
                UpdateSiteData();
            }
        }

        /// <summary>
        /// When the Site value changes, update the base site and thread name values appropriately.
        /// </summary>
        void UpdateSiteData()
        {
            Match m = siteRegex.Match(site.AbsoluteUri);
            if (m.Success)
            {
                // Default to SV if no host information is provided in the Uri.
                if (m.Groups["base"].Success)
                    BaseSite = m.Groups["base"].Value;
                else
                    BaseSite = "https://forums.sufficientvelocity.com/";

                Host = new Uri(BaseSite);
                ThreadName = m.Groups["thread"].Value;
            }
            else
            {
                throw new ArgumentException($"Invalid XenForo site URL format:\n{site.AbsoluteUri}");
            }
        }

        Uri Host { get; set; }
        string BaseSite { get; set; }
        string ThreadName { get; set; }

        string ThreadBaseUrl => $"{BaseSite}threads/{ThreadName}/";
        string PostsBaseUrl => $"{BaseSite}posts/";
        string ThreadmarksUrl => $"{ThreadBaseUrl}threadmarks?category_id=1";
        string ThreadmarksRSSUrl => $"{ThreadBaseUrl}threadmarks.rss?category_id=1";
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
            int pages;

            HtmlNode doc = page.DocumentNode;

            // Find the page title
            title = PostText.CleanupWebString(doc.Element("html").Element("head")?.Element("title")?.InnerText);

            // Find a common parent for other data
            HtmlNode pageContent = GetPageContent(page, PageType.Thread);

            if (pageContent == null)
                throw new InvalidOperationException("Cannot find content on page.");

            // Find the thread author
            HtmlNode titleBar = pageContent.GetDescendantWithClass("titleBar");

            // Non-thread pages (such as threadmark pages) won't have a title bar.
            if (titleBar == null)
                throw new InvalidOperationException("Not a valid forum thread.");

            var pageDesc = page.GetElementbyId("pageDescription");

            var authorNode = pageDesc?.GetChildWithClass("username");

            author = PostText.CleanupWebString(authorNode?.InnerText);

            // Find the number of pages in the thread
            var pageNavLinkGroup = pageContent.GetDescendantWithClass("div", "pageNavLinkGroup");
            var pageNav = pageNavLinkGroup?.GetChildWithClass("PageNav");
            string lastPage = pageNav?.GetAttributeValue("data-last", "");

            if (string.IsNullOrEmpty(lastPage))
            {
                pages = 1;
            }
            else
            {
                pages = Int32.Parse(lastPage);
            }

            // Create a ThreadInfo object to hold the acquired information.
            ThreadInfo info = new ThreadInfo(title, author, pages);

            return info;
        }

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        public IEnumerable<PostComponents> GetPosts(HtmlDocument page, IQuest quest)
        {
            var posts = from li in GetPostsList(page)
                        //where li.HasClass("stickyFirstContainer") == false
                        select GetPost(li, quest);

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
                XDocument rss = await pageProvider.GetXmlPage(ThreadmarksRSSUrl, "Threadmarks", CachingMode.UseCache,
                    ShouldCache.Yes, SuppressNotifications.No, token).ConfigureAwait(false);

                if (rss.Root.Name == "rss")
                {
                    XName channelName = XName.Get("channel", "");

                    var channel = rss.Root.Element(channelName);

                    XName itemName = XName.Get("item", "");

                    var items = channel.Elements(itemName);

                    XName titleName = XName.Get("title", "");
                    XName pubDate = XName.Get("pubDate", "");

                    var filteredItems = from item in items
                                        let title = item.Element(titleName).Value
                                        where !((quest.UseCustomThreadmarkFilters && quest.ThreadmarkFilter.Match(title)) ||
                                                (!quest.UseCustomThreadmarkFilters && DefaultThreadmarkFilter.Match(title)))
                                        let pub = item.Element(pubDate).Value
                                        where string.IsNullOrEmpty(pub) == false
                                        let pubStamp = DateTime.Parse(pub)
                                        orderby pubStamp
                                        select item;

                    var lastItem = filteredItems.LastOrDefault();

                    if (lastItem != null)
                    {
                        XName linkName = XName.Get("link", "");

                        var link = lastItem.Element(linkName);

                        string href = link.Value;

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
                                return new ThreadRangeInfo(true, 1, 1, 0);

                            // If no page number was found, it's page 1
                            if (page == 0)
                                return new ThreadRangeInfo(false, 0, 1, post);

                            // Otherwise, take the provided values.
                            return new ThreadRangeInfo(false, 0, page, post);
                        }
                    }
                }
            }


            // Load the threadmarks so that we can find the starting post page or number.
            HtmlDocument threadmarkPage = await pageProvider.GetPage(ThreadmarksUrl, "Threadmarks", CachingMode.UseCache,
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
                string fullUrl = await pageProvider.GetRedirectUrl(permalink, null, CachingMode.BypassCache, ShouldCache.No, SuppressNotifications.Yes, token).ConfigureAwait(false);

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

            var contentNode = doc.GetElementbyId("content");

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

        /// <summary>
        /// Gets a list of HtmlNodes for the posts found on the provided page.
        /// Returns an empty list on any failures.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns a list of any found posts.</returns>
        static IEnumerable<HtmlNode> GetPostsList(HtmlDocument page)
        {
            // The ordered list containing all messages.
            var messageList = page?.GetElementbyId("messageList");

            // Return all found list items in the message list, or an empty list.
            return messageList?.Elements("li") ?? new List<HtmlNode>();
        }

        /// <summary>
        /// Get a completed post from the provided HTML list item node.
        /// </summary>
        /// <param name="li">List item node that contains the post.</param>
        /// <returns>Returns a post object with required information.</returns>
        private PostComponents GetPost(HtmlNode li, IQuest quest)
        {
            if (li == null)
                throw new ArgumentNullException(nameof(li));

            string author;
            string id;
            string text;
            int number;

            // Author and ID are in the basic list item attributes
            author = PostText.CleanupWebString(li.GetAttributeValue("data-author", ""));
            id = li.Id.Substring("post-".Length);

            if (AdvancedOptions.Instance.DebugMode)
                author = $"{author}_{id}";

            // Get the primary content of the list item
            HtmlNode primaryContent = li.GetChildWithClass("primaryContent");

            // On one branch, we can get the post text
            HtmlNode messageContent = primaryContent.GetChildWithClass("messageContent");
            HtmlNode postBlock = messageContent.Element("article").Element("blockquote");

            // Predicate filtering out elements that we don't want to include
            List<string> excludedClasses = new List<string> { "bbCodeQuote", "messageTextEndMarker","advbbcodebar_encadre",
                "advbbcodebar_article", "adv_tabs_wrapper", "adv_slider_wrapper"};
            if (quest.IgnoreSpoilers)
                excludedClasses.Add("bbCodeSpoilerContainer");

            var exclusions = PostText.GetClassesExclusionPredicate(excludedClasses);

            // Get the full post text.
            text = PostText.ExtractPostText(postBlock, exclusions, Host);

            // On another branch of the primary content, we can get the post number.
            HtmlNode messageMeta = primaryContent.GetChildWithClass("messageMeta");
            // HTML parsing of the post was corrupted somehow.
            if (messageMeta == null)
            {
                return null;
            }
            HtmlNode publicControls = messageMeta.GetChildWithClass("publicControls");
            HtmlNode postNumber = publicControls.GetChildWithClass("postNumber");

            string postNumberText = postNumber.InnerText;
            // Skip the leading # character.
            if (postNumberText.StartsWith("#", StringComparison.Ordinal))
                postNumberText = postNumberText.Substring(1);

            number = int.Parse(postNumberText);

            PostComponents post;
            try
            {
                post = new PostComponents(author, id, text, number, quest);
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
        /// <returns>Returns a list of all unfiltered threadmark links.</returns>
        static IEnumerable<HtmlNode> GetThreadmarksListFromIndex(IQuest quest, HtmlDocument page)
        {
            try
            {
                HtmlNode content = GetPageContent(page, PageType.Threadmarks);

                HtmlNode threadmarksDiv = content.GetDescendantWithClass("div", "threadmarks");

                HtmlNode listOfThreadmarks = null;

                HtmlNode threadmarkList = threadmarksDiv.GetDescendantWithClass("threadmarkList");

                if (threadmarkList != null)
                {
                    // We have a .threadmarkList node.  This is either an ol itself, or it will contain a ThreadmarkCategory_# ol node.  We want category 1.

                    if (threadmarkList.Name == "ol")
                    {
                        if (threadmarkList.GetAttributeValue("class", "").Contains("ThreadmarkCategory"))
                        {
                            if (!threadmarkList.HasClass("ThreadmarkCategory_1"))
                                return new List<HtmlNode>();
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
                    listOfThreadmarks = threadmarksDiv.Descendants("ul").FirstOrDefault(e => e.Elements("li").Any(a => a.HasClass("threadmarkItem")));
                }

                if (listOfThreadmarks != null)
                {
                    Predicate<HtmlNode> filterLambda = (n) =>
                        (quest.UseCustomThreadmarkFilters && quest.ThreadmarkFilter.Match(n.InnerText)) ||
                        (!quest.UseCustomThreadmarkFilters && DefaultThreadmarkFilter.Match(n.InnerText));

                    Func<HtmlNode, HtmlNode> nodeSelector = (n) => n.Element("a");

                    Func<HtmlNode, IEnumerable<HtmlNode>> childSelector = (i) => i.Element("ul")?.Elements("li") ?? new List<HtmlNode>();

                    var results = listOfThreadmarks.Elements("li").TraverseList(childSelector, nodeSelector, filterLambda);

                    return results;
                }
            }
            catch (ArgumentNullException e)
            {
                Logger.Error("Failure when attempting to get the list of threadmarks from the index page. Null list somewhere?", e);
            }

            return new List<HtmlNode>();
        }
        #endregion

        #region Detection
        /// <summary>
        /// Static detection of whether the provided web page is a XenForo forum thread.
        /// </summary>
        /// <param name="page">Web page to examine.</param>
        /// <returns>Returns true if it's detected as a XenForo page.  Otherwise, false.</returns>
        public static bool CanHandlePage(HtmlDocument page)
        {
            if (page == null)
                return false;

            return (page.DocumentNode.Element("html").Id == "XenForo");
        }
        #endregion

    }
}
