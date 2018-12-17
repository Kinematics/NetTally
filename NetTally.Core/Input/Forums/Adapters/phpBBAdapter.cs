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
    class phpBBAdapter : IForumAdapter
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="site">URI for the thread to manage.</param>
        public phpBBAdapter(Uri uri)
        {
            (Host, BaseSite, ThreadName) = GetSiteData(uri);
            site = uri;
        }

        #region Site properties
        Uri site;
        // Example: https://www.phpbb.com/community/viewtopic.php?f=466&t=2347241&start=15#p14278841
        // https://www.phpbb.com/community/viewtopic.php?t=2347241&p14278841
        static readonly Regex siteRegex = new Regex(@"^(?<base>https?://[^/]+/([^/]+/)*)viewtopic.php?(f=\d+&)?(t=(?<thread>\d+(?=\?|#|$)))");

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
        /// match the expected format for forums.</exception>
        private (Uri host, string baseSite, string threadName) GetSiteData(Uri siteUri)
        {
            Match m = siteRegex.Match(siteUri.AbsoluteUri);
            if (m.Success)
            {
                string baseSite = m.Groups["base"].Value;
                Uri host = new Uri(baseSite);
                string threadName = m.Groups["thread"].Value;

                return (host, baseSite, threadName);
            }
            else
            {
                throw new ArgumentException($"Invalid phpBB site URL format:\n{siteUri.AbsoluteUri}");
            }
        }

        Uri Host { get; set; }
        string BaseSite { get; set; }
        string ThreadName { get; set; }

        string ThreadBaseUrl => $"{BaseSite}viewtopic.php?t={ThreadName}";
        string PostsBaseUrl => $"{ThreadBaseUrl}&p";
        #endregion

        #region Public interface
        /// <summary>
        /// Get the default number of posts per page for this forum type.
        /// </summary>
        public int DefaultPostsPerPage => 15;

        /// <summary>
        /// Gets a value indicating whether this instance has RSS threadmarks.
        /// </summary>
        public BoolEx HasRSSThreadmarks => BoolEx.False;

        /// <summary>
        /// Generate a URL to access the specified page of the adapter's thread.
        /// </summary>
        /// <param name="page">The page of the thread that is being loaded.</param>
        /// <returns>Returns a URL formatted to load the requested page of the thread.</returns>
        public string GetUrlForPage(int page, int postsPerPage)
        {
            if (page < 1)
                return ThreadBaseUrl;

            int postIncrement = postsPerPage * (page - 1);

            return $"{ThreadBaseUrl}&start={postIncrement}";
        }

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
            string author = string.Empty;
            int pages = 1;

            HtmlNode doc = page.DocumentNode.Element("html");

            // Find the page title
            title = PostText.CleanupWebString(doc.Element("head")?.Element("title")?.InnerText);

            // Find the number of pages
            var pagebody = page.GetElementbyId("page-body");

            if (pagebody != null)
            {
                // Different versions of the forum have different methods of showing page numbers

                var topicactions = pagebody.GetChildWithClass("topic-actions");
                if (topicactions != null)
                {
                    var pagination = topicactions.GetChildWithClass("pagination");
                    string paginationText = pagination?.InnerText;
                    if (paginationText != null)
                    {
                        Regex pageOf = new Regex(@"Page\s*\d+\s*of\s*(?<pages>\d+)");
                        Match m = pageOf.Match(paginationText);
                        if (m.Success)
                            pages = int.Parse(m.Groups["pages"].Value);
                    }
                }
                else
                {
                    var actionbar = pagebody.GetChildWithClass("action-bar");
                    var pagination = actionbar?.GetChildWithClass("pagination");

                    var ul = pagination?.Element("ul");
                    var lastPageLink = ul?.Elements("li")?.LastOrDefault(n => !n.GetAttributeValue("class", "").Split(' ').Contains("next"));

                    if (lastPageLink != null)
                    {
                        pages = int.Parse(lastPageLink.InnerText);
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
        public IEnumerable<PostComponents> GetPosts(HtmlDocument page, IQuest quest)
        {
            var pagebody = page?.GetElementbyId("page-body");

            if (pagebody == null)
                return new List<PostComponents>();

            var posts = from p in pagebody.Elements("div")
                        where p.GetAttributeValue("class", "").Split(' ').Contains("post")
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
        /// Get a completed post from the provided HTML div node.
        /// </summary>
        /// <param name="div">Div node that contains the post.</param>
        /// <returns>Returns a post object with required information.</returns>
        private PostComponents GetPost(HtmlNode div, IQuest quest)
        {
            if (div == null)
                throw new ArgumentNullException(nameof(div));

            string author = "";
            string id;
            string text;
            int number = 0;

            // id="p12345"
            id = div.Id.Substring(1);


            var inner = div.GetChildWithClass("div", "inner");
            var postbody = inner.GetChildWithClass("div", "postbody");
            var authorNode = postbody.GetChildWithClass("p", "author");
            var authorStrong = authorNode.Descendants("strong").FirstOrDefault();
            var authorAnchor = authorStrong.Element("a");

            author = PostText.CleanupWebString(authorAnchor.InnerText);

            // No way to get the post number??


            // Get the full post text.  Two different layout variants.
            var content = postbody.GetChildWithClass("div", "content");
            if (content == null)
                content = postbody.Elements("div").FirstOrDefault(n => n.Id.StartsWith("post_content", StringComparison.Ordinal));

            text = PostText.ExtractPostText(content, n => false, Host);


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

            //var body = page.DocumentNode.Element("html").Element("body");

            // Not viable to tally without post numbers in posts

            //if (body.Id == "phpbb")
            //    return true;

            return false;
        }
        #endregion

    }
}
