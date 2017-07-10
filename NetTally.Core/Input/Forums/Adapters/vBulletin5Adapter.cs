using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Extensions;
using NetTally.Web;

namespace NetTally.Forums.Adapters
{
    class vBulletin5Adapter : IForumAdapter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uri">The URI of the thread this adapter will be handling.</param>
        public vBulletin5Adapter(Uri uri)
        {
            Site = uri;
        }

        #region Site properties
        Uri site;
        static readonly Regex siteRegex = new Regex(@"^(?<base>https?://[^/]+/([^/]+/)*)(?<thread>\d+-[^&?#]+)");

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
        private void UpdateSiteData()
        {
            if (site == null)
                throw new InvalidOperationException("Site value is null.");

            Match m = siteRegex.Match(site.AbsoluteUri);
            if (m.Success)
            {
                BaseSite = m.Groups["base"].Value;
                Host = new Uri(BaseSite);
                ThreadName = m.Groups["thread"].Value;
            }
            else
            {
                throw new ArgumentException($"Invalid vBulletin4 site URL format:\n{site.AbsoluteUri}");
            }
        }

        Uri Host { get; set; }
        string BaseSite { get; set; }
        string ThreadName { get; set; }

        string ThreadBaseUrl => $"{BaseSite}{ThreadName}";
        string PostsBaseUrl => $"{ThreadBaseUrl}?p=";
        string PageBaseUrl => $"{ThreadBaseUrl}/page";
        #endregion

        #region Public interface
        /// <summary>
        /// Get the default number of posts per page for this forum type.
        /// </summary>
        public int DefaultPostsPerPage => 20;

        /// <summary>
        /// Gets a value indicating whether this instance has RSS threadmarks.
        /// </summary>
        public BoolEx HasRSSThreadmarks => BoolEx.False;

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
        public Task<ThreadRangeInfo> GetStartingPostNumberAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token) =>
            Task.FromResult(new ThreadRangeInfo(true, quest.StartPost));

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
            string author = string.Empty; // vBulletin doesn't show the thread author
            int pages = 1;

            HtmlNode doc = page.DocumentNode.Element("html");

            // Find the page title
            title = doc.Element("head").Element("title")?.InnerText;
            title = PostText.CleanupWebString(title);

            var threadViewTab = page.GetElementbyId("thread-view-tab");

            var pageNavControls = threadViewTab?.GetDescendantWithClass("div", "pagenav-controls");

            var pageTotalSpan = pageNavControls?.GetDescendantWithClass("span", "pagetotal");

            if (pageTotalSpan != null)
                pages = int.Parse(pageTotalSpan.InnerText);

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
            var postList = page?.DocumentNode.GetDescendantWithClass("u", "conversation-list");

            if (postList == null)
                return new List<PostComponents>();

            var posts = from p in postList.Elements("li")
                        where !string.IsNullOrEmpty(p.GetAttributeValue("data-node-id", ""))
                        select GetPost(p, quest);

            return posts;
        }

        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        public string LineBreak => "———————————————————————————————————————————————————————";

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

        #region Utility
        /// <summary>
        /// Get a completed post from the provided HTML list item.
        /// </summary>
        /// <param name="li">List item that contains the post.</param>
        /// <returns>Returns a post object with required information.</returns>
        private PostComponents GetPost(HtmlNode li, IQuest quest)
        {
            if (li == null)
                throw new ArgumentNullException(nameof(li));

            string author = "";
            string id = "";
            string text = "";
            int number = 0;

            // ID
            id = li.GetAttributeValue("data-node-id", "");

            if (string.IsNullOrEmpty(id))
                return null;

            // Author
            var postAuthorNode = li.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("itemprop", "") == "author");
            var authorNode = postAuthorNode?.GetDescendantWithClass("div", "author");

            if (authorNode != null)
                author = PostText.CleanupWebString(authorNode.InnerText);

            var contentArea = li.GetDescendantWithClass("div", "b-post__content");

            // Number
            var postCountAnchor = contentArea.GetDescendantWithClass("a", "b-post__count");

            if (postCountAnchor != null)
            {
                string postNumText = postCountAnchor.InnerText;
                if (postNumText.StartsWith("#", StringComparison.Ordinal))
                    postNumText = postNumText.Substring(1);

                number = int.Parse(postNumText);
            }

            // Text
            var postTextNode = contentArea.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("itemprop", "") == "text");

            // Predicate filtering out elements that we don't want to include
            var exclusion = PostText.GetClassExclusionPredicate("bbcode_quote");

            // Get the full post text.
            text = PostText.ExtractPostText(postTextNode, exclusion, Host);


            PostComponents post;
            try
            {
                post = new PostComponents(author, id, text, number, quest);
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

            return page.DocumentNode.Element("html").Element("body")?.Id == "vb-page-body";
        }
        #endregion

        public IEnumerable<NetTally.Votes.Experiment2.Post> GetPosts2(HtmlDocument page, IQuest quest)
        {
            throw new NotImplementedException();
        }
    }
}
