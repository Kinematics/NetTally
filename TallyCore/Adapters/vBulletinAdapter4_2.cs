using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter4_2 : IForumAdapter2
    {
        public vBulletinAdapter4_2(Uri uri)
        {
            Site = uri;
        }

        public int DefaultPostsPerPage => 20;

        Uri site;
        static readonly Regex siteRegex = new Regex(@"^(?!.*\s)(?<base>https?://[^/]+/([^/]+/)*)showthread.php\?(?<thread>[^&/]+))");

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

        public IEnumerable<PostComponents> GetPosts(HtmlDocument page)
        {
            var body = page.DocumentNode.Element("html")?.Element("body");
            var postlist = body.Descendants("ol").FirstOrDefault(a => a.Id == "posts");

            if (postlist == null)
                return new List<PostComponents>();

            var posts = from p in postlist.Elements("li")
                        where p.Id.StartsWith("post_")
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
            id = li.Id.Substring("post_".Length);

            HtmlNode node;

            // Number
            node = li.Elements("div").FirstOrDefault(n => n.GetAttributeValue("class", "") == "posthead");
            node = node?.Elements("span").FirstOrDefault(n => n.GetAttributeValue("class", "") == "nodecontrols");
            node = node?.Elements("a").FirstOrDefault(n => n.Id.StartsWith("postcount"));

            if (node != null)
                number = int.Parse(node.GetAttributeValue("name", "0"));


            HtmlNode postDetails = li.Elements("div").FirstOrDefault(n => n.GetAttributeValue("class", "") == "postdetails");

            if (postDetails != null)
            {
                // Author
                HtmlNode userinfo = postDetails.Elements("div").FirstOrDefault(n => n.GetAttributeValue("class", "") == "userinfo");
                author = userinfo?.Elements("a").FirstOrDefault(n => n.GetAttributeValue("class", "").Split(' ').Contains("username")).InnerText;


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
