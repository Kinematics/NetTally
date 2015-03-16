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
    /// <summary>
    /// Class for web scraping forums using version 3 of vBulletin.
    /// </summary>
    public class vBulletinAdapter3 : IForumAdapter
    {
        public vBulletinAdapter3(string baseSiteName)
        {
            if (baseSiteName == null)
                throw new ArgumentNullException(nameof(baseSiteName));

            ForumUrl = baseSiteName;
            ThreadsUrl = baseSiteName;
            PostsUrl = baseSiteName;
        }

        protected virtual string ForumUrl { get; }
        protected virtual string ThreadsUrl { get; }
        protected virtual string PostsUrl { get; }

        // Bad characters we want to remove
        // \u200b = Zero width space (8203 decimal/html).  Trim() does not remove this character.
        readonly Regex badCharactersRegex = new Regex("\u200b");


        #region Public interface functions

        // Functions for constructing URLs

        public string GetThreadsUrl(string questTitle) => ThreadsUrl + questTitle;

        public string GetThreadPageBaseUrl(string questTitle) => GetThreadsUrl(questTitle) + "&page=";

        public string GetThreadmarksPageUrl(string questTitle) => GetThreadsUrl(questTitle) + "/threadmarks";

        public string GetPageUrl(string questTitle, int page)
        {
            if (page > 1)
                return GetThreadPageBaseUrl(questTitle) + page.ToString();
            else
                return GetThreadsUrl(questTitle);
        }

        public string GetPostUrlFromId(string postId)
        {
            if (postId == null)
                throw new ArgumentNullException(nameof(postId));

            // http://forums.animesuki.com/showthread.php?p=5460127#post5460127
            string url = ForumUrl + "showthread.php?p=" + postId + "#post" + postId;
            return url;
        }

        public string GetUrlFromRelativeAddress(string relative) => ForumUrl + relative;

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

            return CleanupPostString(title);
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

            // vBulletin thread name always starts with showthread.php
            Regex validateQuestNameForUrl = new Regex(@"^showthread.php");
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
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        public int GetPageNumberFromPostNumber(int postNumber) => ((postNumber - 1) / 20) + 1;

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

            // If there's no pagenav div, that means there's no navigation to alternate pages,
            // which means there's only one page in the thread.
            var pageNavDiv = page.DocumentNode.Descendants("div").FirstOrDefault(a => a.GetAttributeValue("class", "") == "pagenav");

            if (pageNavDiv == null)
                return 1;

            var vbMenuControl = pageNavDiv.Descendants("td").FirstOrDefault(a => a.GetAttributeValue("class", "") == "vbmenu_control");

            if (vbMenuControl != null)
            {
                string pageNumbersText = vbMenuControl.InnerText;
                Regex pageNumsRegex = new Regex(@"Page \d+ of (?<lastPage>\d+)");
                Match m = pageNumsRegex.Match(pageNumbersText);
                if (m.Success)
                {
                    int lastPage = 0;
                    if (int.TryParse(m.Groups["lastPage"].Value, out lastPage))
                        return lastPage;
                }
            }

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

            string postIdString = post.Id;
            if (postIdString.StartsWith("post"))
                postIdString = postIdString.Substring("post".Length);

            return postIdString;
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

            string id = GetIdOfPost(post);

            string postNumberAnchorID = "postcount" + id;

            var anchor = post.Descendants("a").FirstOrDefault(a => a.Id == postNumberAnchorID);

            if (anchor != null)
            {
                string postNumText = anchor.GetAttributeValue("name", "");
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

            string author = string.Empty;

            string id = GetIdOfPost(post);

            string postAuthorDivID = "postmenu_" + id;

            var authorDiv = post.Descendants("div").FirstOrDefault(a => a.Id == postAuthorDivID);

            if (authorDiv != null)
            {
                var authorAnchor = authorDiv.Element("a");

                if (authorAnchor != null)
                {
                    author = authorAnchor.InnerText;

                    if (authorAnchor.Element("span") != null)
                    {
                        author = authorAnchor.Element("span").InnerText;
                    }
                }
            }

            return HtmlEntity.DeEntitize(author);
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
            post = GetContentsOfPost(post);

            // Start recursing at the child blockquote node.
            string postText = ExtractNodeText(post);

            // Clean up the extracted text before returning.
            return CleanupPostString(postText);
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
            var divs = page.DocumentNode.Element("html")?.Element("body")?.Elements("div");
            var posts = divs?.FirstOrDefault(a => a.Id == "posts");

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

            var posts = from d in postList.Elements("div")
                        select d.Descendants("table").FirstOrDefault(a => a.Id.StartsWith("post"));

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
            string postId = "post" + id;

            return postsList.FirstOrDefault(a => a.Id == postId);
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

            return post.Name == "table" && post.Id.StartsWith("post");
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

            string id = GetIdOfPost(post);

            string postMessageId = "post_message_" + id;

            var message = post.Descendants("div").FirstOrDefault(a => a.Id == postMessageId);

            return message;
        }

        #endregion

        #region Text processing functions
        /// <summary>
        /// Extract the text of the provided HTML node.  Recurses into nested
        /// divs.
        /// </summary>
        /// <param name="node">The node to pull text content from.</param>
        /// <returns>A string containing the text of the post, with formatting
        /// elements converted to BBCode tags.</returns>
        private string ExtractNodeText(HtmlNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            StringBuilder sb = new StringBuilder();

            // Search the post for valid element types, and put them all together
            // into a single string.
            foreach (var childNode in node.ChildNodes)
            {
                // A <br> element adds a newline.
                // Usually redundant, but sometimes needed before we bail out on
                // nodes without any inner text (such as <br/>).
                if (childNode.Name == "br")
                {
                    sb.AppendLine("");
                    continue;
                }

                // If the node doesn't contain any text, move to the next.
                if (childNode.InnerText.Trim() == string.Empty)
                    continue;

                // Add BBCode markup in place of HTML format elements,
                // while collecting the text in the post.
                switch (childNode.Name)
                {
                    case "#text":
                        sb.Append(childNode.InnerText);
                        break;
                    case "i":
                        sb.Append("[i]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/i]");
                        break;
                    case "b":
                        sb.Append("[b]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/b]");
                        break;
                    case "u":
                        sb.Append("[u]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/u]");
                        break;
                    case "span":
                        string spanStyle = childNode.GetAttributeValue("style", "");
                        if (spanStyle.StartsWith("color:", StringComparison.OrdinalIgnoreCase))
                        {
                            string spanColor = spanStyle.Substring("color:".Length).Trim();
                            sb.Append("[color=");
                            sb.Append(spanColor);
                            sb.Append("]");
                            sb.Append(childNode.InnerText);
                            sb.Append("[/color]");
                        }
                        break;
                    case "a":
                        sb.Append("[url=\"");
                        sb.Append(childNode.GetAttributeValue("href", ""));
                        sb.Append("\"]");
                        sb.Append(childNode.InnerText);
                        sb.Append("[/url]");
                        break;
                    case "div":
                        // Don't Recurse into divs
                        //sb.Append(ExtractNodeText(childNode));
                        break;
                    default:
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clean up problematic bits of text in the extracted post string.
        /// </summary>
        /// <param name="postText">The text of the post.</param>
        /// <returns>Returns a cleaned version of the post text.</returns>
        private string CleanupPostString(string postText)
        {
            if (postText == null)
                throw new ArgumentNullException(nameof(postText));

            postText = postText.TrimStart();
            postText = HtmlEntity.DeEntitize(postText);
            postText = badCharactersRegex.Replace(postText, "");
            return postText;
        }
        #endregion

    }
}
