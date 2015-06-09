using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public class vBulletinAdapter5 : IForumAdapter
    {
        public vBulletinAdapter5()
        {

        }

        public vBulletinAdapter5(string site)
        {
            ForumUrl = site;
            ThreadsUrl = site;
            PostsUrl = site;
        }

        protected virtual string ForumUrl { get; }
        protected virtual string ThreadsUrl { get; }
        protected virtual string PostsUrl { get; }

        public int DefaultPostsPerPage => 20;

        // Extract color attributes from span style.
        readonly Regex spanColorRegex = new Regex(@"\bcolor\s*:\s*(?<color>\w+)", RegexOptions.IgnoreCase);
        readonly Regex strikeSpanRegex = new Regex(@"text-decoration:\s*line-through");


        #region Public interface functions

        // Functions for constructing URLs

        public string GetPageUrl(string threadName, int page)
        {
            if (threadName == null)
                throw new ArgumentNullException(nameof(threadName));
            if (threadName == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(threadName));

            if (page == 1)
                return threadName;

            string trailSlash = "";
            if (!threadName.EndsWith("/"))
                trailSlash = "/";

            return threadName + trailSlash + "page" + page.ToString();
        }

        public string GetPostUrlFromId(string threadName, string postId)
        {
            if (threadName == null)
                throw new ArgumentNullException(nameof(threadName));
            if (threadName == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(threadName));
            if (postId == null)
                throw new ArgumentNullException(nameof(postId));
            if (postId == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(postId));

            // http://www.vbulletin.com/forum/forum/vbulletin-sales-and-feedback/site-feedback/326978-is-vbulletin-3-8-5-going-to-be-released-this-month?p=2860988#post2860988

            string trimSlash = threadName.TrimEnd('/');

            return trimSlash + "?p=" + postId + "#post" + postId;
        }
        
        public string GetRelativeUrl(string relative) => ForumUrl + relative;

        /// <summary>
        /// Get the title of the web page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the title of the page.</returns>
        public string GetPageTitle(HtmlDocument page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var title = page.DocumentNode.Element("html")?.Element("head")?.Element("title")?.InnerText;

            if (title == null)
                return string.Empty;

            return Utility.PostText.CleanupWebString(title);
        }

        /// <summary>
        /// Check if the name of the thread is valid for inserting into a URL.
        /// </summary>
        /// <param name="name">The name of the quest/thread.</param>
        /// <returns>Returns true if the name is valid.</returns>
        public bool IsValidThreadName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            // vBulletin v5 has no specific formatting.  Just make sure there aren't any spaces.
            Regex validateQuestNameForUrl = new Regex(@"\S+");
            return validateQuestNameForUrl.Match(name).Success;
        }

        /// <summary>
        /// Get the author of the thread.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the thread author.</returns>
        public string GetAuthorOfThread(HtmlDocument page)
        {
            // vBulletin does not provide thread author information
            return string.Empty;
        }

        /// <summary>
        /// Calculate the page number that corresponds to the post number given.
        /// </summary>
        /// <param name="quest">Quest that we're getting the page number for.</param>
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        public int GetPageNumberFromPostNumber(IQuest quest, int postNumber) => ((postNumber - 1) / quest.PostsPerPage) + 1;

        /// <summary>
        /// Get the last page number of the thread, based on info available
        /// from the given page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the last page number of the thread.</returns>
        public int GetLastPageNumberOfThread(HtmlDocument page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var threadViewTab = page.DocumentNode.Descendants("div").FirstOrDefault(a => a.Id == "thread-view-tab");

            var pageNavControls = threadViewTab.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pagenav-controls"));

            var pageTotalSpan = pageNavControls.Descendants("span").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pagetotal"));

            int lastPage = 0;
            if (int.TryParse(pageTotalSpan.InnerText, out lastPage))
                return lastPage;

            throw new InvalidOperationException("Unable to get the last page number of the thread.");
        }

        /// <summary>
        /// Given a page, return a list of posts on the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns an enumerable list of posts.</returns>
        public IEnumerable<HtmlNode> GetPostsFromPage(HtmlDocument page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var postList = GetPostListFromPage(page);
            return GetPostsFromList(postList);
        }

        /// <summary>
        /// Get the ID string of the provided post (the portion that can be used in a URL).
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the portion of the ID that can be inserted into a URL
        /// to reach this post.</returns>
        public string GetIdOfPost(HtmlNode post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            return post.GetAttributeValue("data-node-id", "");
        }

        /// <summary>
        /// This gets the sequential post number of a given post message.
        /// </summary>
        /// <param name="post">The post to inspect.</param>
        /// <returns>Returns the post number that's found.</returns>
        public int GetPostNumberOfPost(HtmlNode post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            var postCountAnchor = post.Descendants("a").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("b-post__count"));

            if (postCountAnchor != null)
            {
                string postNumText = postCountAnchor.InnerText;
                if (postNumText.StartsWith("#"))
                    postNumText = postNumText.Substring(1);

                int postNum = 0;
                if (int.TryParse(postNumText, out postNum))
                    return postNum;
            }

            throw new InvalidOperationException("Unable to extract a post number from the post.");
        }

        /// <summary>
        /// Gets the author of the post.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the author of the post.</returns>
        public string GetAuthorOfPost(HtmlNode post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            var userInfo = post?.Elements("div").FirstOrDefault(a => a.GetAttributeValue("itemprop", "") == "author");
            var author = userInfo?.Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("author"));

            var authorAnchor = author?.Descendants("a").FirstOrDefault();

            return HtmlEntity.DeEntitize(authorAnchor?.InnerText);
        }

        /// <summary>
        /// Get the collated text of the post, that can be used in other parts of the program.
        /// This includes BBCode formatting, where appropriate.
        /// </summary>
        /// <param name="post">The post or post content to query.</param>
        /// <returns>Returns the contents of the post as a formatted string.</returns>
        public string GetTextOfPost(HtmlNode post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            // Get the inner contents, if it wasn't passed in directly
            HtmlNode postContents = GetContentsOfPost(post);

            // Predicate filtering out elements that we don't want to include
            var exclusion = Utility.PostText.GetClassExclusionPredicate("bbcode_quote");

            // Get the full post text.
            return Utility.PostText.ExtractPostText(postContents, exclusion);
        }

        #endregion

        #region Special Interface function
        public Task<int> GetStartingPostNumber(IPageProvider pageProvider, IQuest quest, CancellationToken token)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            return Task.FromResult(quest.StartPost);
        }

        #endregion


        // Utility functions to support the above interface functions

        #region Functions dealing with pages

        /// <summary>
        /// Get the node element containing all the posts on the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the page element that contains the posts.</returns>
        private HtmlNode GetPostListFromPage(HtmlDocument page)
        {
            var posts = page.DocumentNode.Descendants("ul").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("conversation-list"));

            return posts;
        }

        /// <summary>
        /// Given the page node containing all of the posts on the thread,
        /// get an IEnumerable list of all posts on the page.
        /// </summary>
        /// <param name="postList">Element containing posts from the page.</param>
        /// <returns>Returns a list of posts.</returns>
        private IEnumerable<HtmlNode> GetPostsFromList(HtmlNode postList)
        {
            if (postList == null)
                return new List<HtmlNode>();

            var posts = postList.Elements("li").Where<HtmlNode>(a => a.GetAttributeValue("data-node-id", "") != string.Empty);

            return posts;
        }

        /// <summary>
        /// Get a specific post from the page, when provided with the unique
        /// ID of the post.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <param name="id">The ID of the post.</param>
        /// <returns>Returns the post.</returns>
        private HtmlNode GetPostFromPageById(HtmlDocument page, string id)
        {
            var postsList = GetPostsFromPage(page);

            return postsList.FirstOrDefault(a => a.GetAttributeValue("data-node-id", "") == id);
        }
        #endregion

        #region Functions dealing with posts
        /// <summary>
        /// Function to tell if the provided HTML node is a thread post.
        /// </summary>
        /// <param name="post">Proposed post.</param>
        /// <returns>Returns true if the node appears to be a legitimate post.</returns>
        private bool IsPost(HtmlNode post)
        {
            if (post == null)
                return false;

            return post.Name == "li" && post.GetAttributeValue("data-node-id", "") != string.Empty;
        }

        /// <summary>
        /// Gets the inner HTML node containing the actual contents of the post.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the article node within the post.</returns>
        private HtmlNode GetContentsOfPost(HtmlNode post)
        {
            if (post == null)
                return null;

            var postTextNode = post?.Elements("div").FirstOrDefault(a => a.GetAttributeValue("itemprop", "") == "text");

            return postTextNode;
        }

        #endregion
    }
}
