using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter3_2 : IForumAdapter2
    {
        public vBulletinAdapter3_2(Uri site)
        {
            Site = site;
        }

        #region Site properties
        Uri site;
        // Example: http://forums.animesuki.com/showthread.php?t=128882&page=145
        static readonly Regex siteRegex = new Regex(@"^(?!.*\s)(?<base>https?://[^/]+/([^/]+/)*)showthread.php\?(t=(?<thread>\d+))");

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
                throw new ArgumentException($"Invalid vBulletin3 site URL format:\n{site.AbsoluteUri}");
            }
        }

        string BaseSite { get; set; }
        string ThreadName { get; set; }

        string ThreadBaseUrl => $"{BaseSite}showthread.php?t={ThreadName}";
        string PostsBaseUrl => $"{BaseSite}showthread.php?p=";
        #endregion

        public int DefaultPostsPerPage => 20;

        #region Public interface
        public string GetUrlForPage(int page) => $"{ThreadBaseUrl}&page={page}";

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
            title = doc.Element("html")?.Element("head")?.Element("title")?.InnerText;
            title = PostText.CleanupWebString(title);

            author = string.Empty;


            pages = 1;

            // If there's no pagenav div, that means there's no navigation to alternate pages,
            // which means there's only one page in the thread.
            var pageNavDiv = page.DocumentNode.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("class", "") == "pagenav");

            if (pageNavDiv != null)
            {
                var vbMenuControl = pageNavDiv.Descendants("td").FirstOrDefault(a => a.GetAttributeValue("class", "") == "vbmenu_control");

                if (vbMenuControl != null)
                {
                    Regex pageNumsRegex = new Regex(@"Page \d+ of (?<pages>\d+)");

                    Match m = pageNumsRegex.Match(vbMenuControl.InnerText);
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
            var divs = page.DocumentNode.Element("html")?.Element("body")?.Elements("div");
            var postlist = divs?.FirstOrDefault(a => a.Id == "posts");

            if (postlist == null)
                return new List<PostComponents>();

            var posts = from p in postlist.Elements("div")
                        select GetPost(p);

            return posts;
        }
        #endregion

        #region Utility
        private PostComponents GetPost(HtmlNode postDiv)
        {
            if (postDiv == null)
                throw new ArgumentNullException(nameof(postDiv));

            string author = "";
            string id;
            string text;
            int number = 0;

            var postTable = postDiv.Descendants("table").FirstOrDefault(a => a.Id.StartsWith("post"));

            if (postTable == null)
                return null;

            id = postTable.Id.Substring("post".Length);

            string postAuthorDivID = "postmenu_" + id;

            var authorAnchor = postTable.Descendants("div").FirstOrDefault(a => a.Id == postAuthorDivID)?.Element("a");

            if (authorAnchor != null)
            {
                author = authorAnchor.InnerText;

                if (authorAnchor.Element("span") != null)
                {
                    author = authorAnchor.Element("span").InnerText;
                }
            }

            string postNumberAnchorID = "postcount" + id;

            var anchor = postTable.Descendants("a").FirstOrDefault(a => a.Id == postNumberAnchorID);

            if (anchor != null)
            {
                string postNumText = anchor.GetAttributeValue("name", "");
                number = int.Parse(postNumText);
            }

            string postMessageId = "post_message_" + id;

            var postContents = postTable.Descendants("div").FirstOrDefault(a => a.Id == postMessageId);



            // Predicate filtering out elements that we don't want to include
            var exclusion = PostText.GetClassExclusionPredicate("bbcode_quote");

            // Get the full post text.
            text = PostText.ExtractPostText(postContents, exclusion);


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

            var head = page.DocumentNode.Element("html")?.Element("head");
            if (head != null)
            {
                var generator = head.Elements("meta").FirstOrDefault(a => a.GetAttributeValue("name", "") == "generator");
                if (generator != null)
                {
                    if (generator.GetAttributeValue("content", "").StartsWith("vBulletin"))
                        return true;
                }
            }

            return false;
        }
        #endregion

    }
}
