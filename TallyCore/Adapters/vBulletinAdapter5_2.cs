using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter5_2 : IForumAdapter2
    {
        public vBulletinAdapter5_2(Uri uri)
        {
            Site = uri;
        }

        public int DefaultPostsPerPage => 20;

        Uri site;
        static readonly Regex siteRegex = new Regex(@"^(?!.*\s)(?<base>https?://[^/]+/([^/]+/)*)(?<thread>\d+-[^&?#]+))");

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

        string ThreadBaseUrl => $"{BaseSite}{ThreadName}";
        string PostsBaseUrl => $"{ThreadBaseUrl}?p=";
        string PageBaseUrl => $"{ThreadBaseUrl}/page";


        public string GetUrlForPage(int page) => $"{PageBaseUrl}{page}";

        public string GetPermalinkForId(string postId) => $"{PostsBaseUrl}{postId}";

        public Task<ThreadStartValue> GetStartingPostNumber(IQuest quest, IPageProvider pageProvider, CancellationToken token) =>
            Task.FromResult(new ThreadStartValue(true, quest.StartPost));

        public ThreadInfo GetThreadInfo(HtmlDocument page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            string title, author;
            int pages;

            HtmlNode doc = page.DocumentNode;

            // Find the page title
            title = doc.Element("html").Element("head").Element("title")?.InnerText;
            title = PostText.CleanupWebString(title);

            author = string.Empty;

            pages = 1;

            var threadViewTab = page.DocumentNode.Descendants("div").FirstOrDefault(a => a.Id == "thread-view-tab");

            var pageNavControls = threadViewTab?.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pagenav-controls"));

            var pageTotalSpan = pageNavControls?.Descendants("span").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pagetotal"));

            if (pageTotalSpan != null)
                pages = int.Parse(pageTotalSpan.InnerText);

            ThreadInfo info = new ThreadInfo(title, author, pages);

            return info;
        }

        public IEnumerable<PostComponents> GetPosts(HtmlDocument page)
        {
            var postList = page.DocumentNode.GetDescendantNodeWithClass("u", "conversation-list");

            if (postList == null)
                return new List<PostComponents>();

            var posts = from p in postList.Elements("li")
                        select GetPost(p);

            return posts;
        }

        private PostComponents GetPost(HtmlNode li)
        {
            if (li == null)
                throw new ArgumentNullException(nameof(li));

            string author = "";
            string id = "";
            string text = "";
            int number = 0;

            // ID
            id = li.GetAttributeValue("data-node-id", "");

            if (id == string.Empty)
                return null;

            // Author
            var postAuthorNode = li.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("itemprop", "") == "author");
            var authorNode = postAuthorNode?.GetDescendantNodeWithClass("div", "author");

            if (authorNode != null)
                author = authorNode.InnerText;

            var contentArea = li.GetDescendantNodeWithClass("div", "b-post__content");

            // Number
            var postCountAnchor = contentArea.GetDescendantNodeWithClass("a", "b-post__count");

            if (postCountAnchor != null)
            {
                string postNumText = postCountAnchor.InnerText;
                if (postNumText.StartsWith("#"))
                    postNumText = postNumText.Substring(1);

                number = int.Parse(postNumText);
            }

            // Text
            var postTextNode = contentArea.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("itemprop", "") == "text");

            // Predicate filtering out elements that we don't want to include
            var exclusion = PostText.GetClassExclusionPredicate("bbcode_quote");

            // Get the full post text.
            text = PostText.ExtractPostText(postTextNode, exclusion);


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

            return page.DocumentNode.Element("html")?.Element("body")?.Id == "vb-page-body";
        }
        #endregion

    }
}
