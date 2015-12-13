using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter4 : IForumAdapter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">The URI of the thread this adapter will be handling.</param>
        public vBulletinAdapter4(Uri uri)
        {
            Site = uri;
        }

        #region Site properties
        Uri site;
        static readonly Regex siteRegex = new Regex(@"^(?<base>https?://[^/]+/([^/]+/)*)showthread.php\?(?<thread>[^&/]+)");

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
                // Must set a valid value
                if (value == null)
                    throw new ArgumentNullException(nameof(Site));

                // If object doesn't have a value, just set and be done.
                if (site == null)
                {
                    site = value;
                    UpdateSiteData();
                    return;
                }

                // If the URI didn't change, we don't need to do anything.
                if (site.AbsoluteUri == value.AbsoluteUri)
                {
                    return;
                }

                // If the host *did* change, we can't consider this a valid adapter anymore.
                if (site.Host != value.Host)
                {
                    throw new InvalidOperationException("Host has changed.");
                }

                // Otherwise, just update the site URI and data.
                site = value;
                UpdateSiteData();
            }
        }

        /// <summary>
        /// When the Site value changes, update the base site and thread name values appropriately.
        /// </summary>
        private void UpdateSiteData()
        {
            if (site == null)
                throw new InvalidOperationException("Site value is null.");

            Match m = siteRegex.Match(site.AbsoluteUri);
            if (m.Success)
            {
                BaseSite = m.Groups["base"].Value;
                ThreadName = m.Groups["thread"].Value;
            }
            else
            {
                throw new ArgumentException($"Invalid vBulletin4 site URL format:\n{site.AbsoluteUri}");
            }
        }

        string BaseSite { get; set; }
        string ThreadName { get; set; }

        string ThreadBaseUrl => $"{BaseSite}showthread.php?{ThreadName}";
        string PostsBaseUrl => $"{ThreadBaseUrl}&p=";
        string PageBaseUrl => $"{ThreadBaseUrl}/page";
        #endregion

        #region Public interface
        /// <summary>
        /// Get the default number of posts per page for this forum type.
        /// </summary>
        public int DefaultPostsPerPage => 20;

        /// <summary>
        /// Generate a URL to access the specified page of the adapter's thread.
        /// </summary>
        /// <param name="page">The page of the thread that is being loaded.</param>
        /// <returns>Returns a URL formatted to load the requested page of the thread.</returns>
        public string GetUrlForPage(int page, int postsPerPage) => $"{PageBaseUrl}{page}";

        /// <summary>
        /// Generate a URL to access the specified post of the adapter's thread.
        /// </summary>
        /// <param name="postId">The permalink ID of the post being requested.</param>
        /// <returns>Returns a URL formatted to load the requested post.</returns>
        public string GetPermalinkForId(string postId) => $"{PostsBaseUrl}{postId}";

        /// <summary>
        /// Get the starting post number, to begin tallying from.
        /// Since vBulletin doesn't have threadmarks, this will always be whatever
        /// is specified as the start post for the quest.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="pageProvider">A page provider to allow loading the threadmark page.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>Returns data indicating where to begin tallying the thread.</returns>
        public Task<ThreadStartInfo> GetStartingPostNumber(IQuest quest, IPageProvider pageProvider, CancellationToken token) =>
            Task.FromResult(new ThreadStartInfo(true, quest.StartPost));

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
            string author = string.Empty; // vBulletin doesn't show thread authors
            int pages = 1;

            HtmlNode doc = page.DocumentNode;

            // Find the page title
            title = doc.Element("html").Element("head").Element("title")?.InnerText;
            title = PostText.CleanupWebString(title);

            // Get the number of pages from the navigation elements
            var paginationTop = page.DocumentNode.Descendants("div").FirstOrDefault(a => a.Id == "pagination_top");

            var paginationForm = paginationTop.Element("form");

            // If there is no form, that means there's only one page in the thread.
            if (paginationForm != null)
            {
                var firstSpan = paginationForm.Element("span");
                var firstSpanA = firstSpan?.Element("a");
                var pagesText = firstSpanA?.InnerText;

                if (pagesText != null)
                {
                    Regex pageNumsRegex = new Regex(@"Page \d+ of (?<pages>\d+)");
                    Match m = pageNumsRegex.Match(pagesText);
                    if (m.Success)
                    {
                        pages = int.Parse(m.Groups["pages"].Value);
                    }
                }
            }

            ThreadInfo info = new ThreadInfo(title, author, pages);

            return info;
        }

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        public IEnumerable<PostComponents> GetPosts(HtmlDocument page)
        {
            var body = page.DocumentNode.Element("html")?.Element("body");
            var postlist = body.Descendants("ol").FirstOrDefault(a => a.Id == "posts");

            if (postlist == null)
                return new List<PostComponents>();

            var posts = from p in postlist.Elements("li")
                        where p.Id.StartsWith("post_", StringComparison.Ordinal)
                        select GetPost(p);

            return posts;
        }

        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        public string LineBreak => "———————————————————————————————————————————————————————";

        #endregion

        #region Utility
        /// <summary>
        /// Get a completed post from the provided HTML list item node.
        /// </summary>
        /// <param name="postDiv">List item node that contains the post.</param>
        /// <returns>Returns a post object with required information.</returns>
        private PostComponents GetPost(HtmlNode li)
        {
            if (li == null)
                throw new ArgumentNullException(nameof(li));

            string author = "";
            string id = "";
            string text = "";
            int number = 0;

            // ID
            id = li.Id.Substring("post_".Length);

            // Number
            HtmlNode postHead = li.GetChildWithClass("div", "posthead");
            HtmlNode nodeControls = postHead?.GetChildWithClass("nodecontrols");
            HtmlNode postCount = nodeControls?.Elements("a").FirstOrDefault(n => n.Id.StartsWith("postcount", StringComparison.Ordinal));

            if (postCount != null)
                number = int.Parse(postCount.GetAttributeValue("name", "0"));


            HtmlNode postDetails = li.Elements("div").FirstOrDefault(n => n.GetAttributeValue("class", "") == "postdetails");

            if (postDetails != null)
            {
                // Author
                HtmlNode userinfo = postDetails.GetChildWithClass("div", "userinfo");
                HtmlNode username = userinfo?.GetChildWithClass("a", "username");
                author = PostText.CleanupWebString(username?.InnerText);

                // Text
                string postMessageId = "post_message_" + id;

                HtmlNode message = postDetails.Descendants("div").FirstOrDefault(a => a.Id == postMessageId)?.Element("blockquote");

                // Predicate filtering out elements that we don't want to include
                var exclusion = PostText.GetClassExclusionPredicate("bbcode_quote");

                // Get the full post text.
                text = PostText.ExtractPostText(message, exclusion);
            }

            PostComponents post;
            try
            {
                post = new PostComponents(author, id, text, number);
            }
            catch
            {
                post = null;
            }

            return post;
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

            return page.DocumentNode.Element("html")?.Id == "vbulletin_html";
        }
        #endregion

    }
}
