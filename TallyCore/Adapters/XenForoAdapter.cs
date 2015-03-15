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
    public class XenForoAdapter : IForumAdapter
    {
        const string ForumUrl = "http://forums.spacebattles.com/";
        const string ThreadsUrl = "http://forums.spacebattles.com/threads/";
        const string PostsUrl = "http://forums.spacebattles.com/posts/";

        // Bad characters we want to remove
        // \u200b = Zero width space (8203 decimal/html).  Trim() does not remove this character.
        readonly Regex badCharactersRegex = new Regex("\u200b");


        #region Public interface functions

        // Functions for constructing URLs

        public string GetThreadsUrl(string questTitle) => ThreadsUrl + questTitle;

        public string GetThreadPageBaseUrl(string questTitle) => GetThreadsUrl(questTitle) + "/page-";

        public string GetThreadmarksPageUrl(string questTitle) => GetThreadsUrl(questTitle) + "/threadmarks";

        public string GetPageUrl(string questTitle, int page)
        {
            if (page > 1)
                return GetThreadPageBaseUrl(questTitle) + page.ToString();
            else
                return GetThreadsUrl(questTitle);
        }

        public string GetPostUrlFromId(string postId) => PostsUrl + postId + "/";

        public string GetUrlFromRelativeAddress(string relative) => ForumUrl + relative;

        /// <summary>
        /// Calculate the page number that corresponds to the post number given.
        /// </summary>
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        public int GetPageNumberFromPostNumber(int postNumber) => ((postNumber - 1) / 25) + 1;

        /// <summary>
        /// Check if the name of the thread is valid for inserting into a URL.
        /// </summary>
        /// <param name="name">The name of the quest/thread.</param>
        /// <returns>Returns true if the name is valid.</returns>
        public bool IsValidThreadName(string name)
        {
            // URL should not have any whitespace in it, and should end with a thread number (eg: .11111).
            Regex validateQuestNameForUrl = new Regex(@"^\S+\.\d+$");
            return validateQuestNameForUrl.Match(name).Success;
        }

        /// <summary>
        /// Get the author of the thread.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the thread author.</returns>
        public string GetAuthorOfThread(HtmlDocument page)
        {
            var pageContent = GetPageContent(page);

            if (pageContent.GetAttributeValue("class", "").Contains("thread_view"))
            {
                var pageDescription = pageContent.Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pageWidth"))?.
                    Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pageContent"))?.
                    Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("titleBar"))?.
                    Elements("p").FirstOrDefault(a => a.Id == "pageDescription");

                var usernameA = pageDescription?.Elements("a").FirstOrDefault(n => n.GetAttributeValue("class", "") == "username");

                if (usernameA == null)
                    throw new InvalidOperationException("Unable to extract author from provided page.");

                return HtmlEntity.DeEntitize(usernameA.InnerText);
            }

            throw new ArgumentException("Page is not a forum thread.");
        }

        /// <summary>
        /// Get the last page number of the thread, based on info available
        /// from the given page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the last page number of the thread.</returns>
        public int GetLastPageNumberOfThread(HtmlDocument page)
        {
            var pageContent = GetPageContent(page);

            if (pageContent.GetAttributeValue("class", "").Contains("thread_view"))
            {
                var pageNav = pageContent.Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pageWidth"))?.
                    Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pageContent"))?.
                    Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("pageNavLinkGroup"))?.
                    Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("PageNav"));

                // PageNav div does not exist if the thread only has one page.

                if (pageNav == null)
                    return 1;

                int lastPage = 0;
                string lastPageAttr = pageNav.GetAttributeValue("data-last", "1");
                if (int.TryParse(lastPageAttr, out lastPage))
                    return lastPage;

                throw new InvalidOperationException("Unable to get the last page number of the thread.");
            }

            throw new ArgumentException("Page is not a forum thread.");
        }

        /// <summary>
        /// Given a page, return a list of posts on the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns an enumerable list of posts.</returns>
        public IEnumerable<HtmlNode> GetPostsFromPage(HtmlDocument page)
        {
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
            if (IsPost(post))
            {
                // The format is "post-12345678", but we only want the number (as text).
                return post.Id.Substring("post-".Length);
            }

            throw new InvalidOperationException("Cannot get ID.  Does not appear to be a valid post.");
        }

        /// <summary>
        /// This gets the sequential post number of a given post message.
        /// </summary>
        /// <param name="post">The post to inspect.</param>
        /// <returns>Returns the post number that's found.</returns>
        public int GetPostNumberOfPost(HtmlNode post)
        {

            if (IsPost(post))
            {
                // Post content structure is like this:
                // post > div.primaryContent > div.messageMeta > div.publicControls > a.postNumber

                var anchor = post.Descendants("a").FirstOrDefault(n => n.GetAttributeValue("class", "").Contains("postNumber"))?.InnerText;

                // Text format of the post number is #1234.  Remove the leading #
                var postNumText = anchor?.Substring(1);

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
            if (IsPost(post))
            {
                // The author is provided in a data attribute of the root element.
                // Be sure to fix HTML entities first.
                return HtmlEntity.DeEntitize(post.GetAttributeValue("data-author", ""));
            }

            throw new InvalidOperationException("Cannot get post author.  Does not appear to be a valid post.");

        }

        /// <summary>
        /// Get the collated text of the post, that can be used in other parts of the program.
        /// This includes BBCode formatting, where appropriate.
        /// </summary>
        /// <param name="post">The post or post content to query.</param>
        /// <returns>Returns the contents of the post as a formatted string.</returns>
        public string GetTextOfPost(HtmlNode post)
        {
            // Get the inner contents, if it wasn't passed in directly
            if (post.Name != "article")
                post = GetContentsOfPost(post);

            // Start recursing at the child blockquote node.
            string postText = ExtractNodeText(post.Element("blockquote"));

            // Clean up the extracted text before returning.
            return CleanupPostString(postText);
        }

        #endregion

        #region Special Interface function
        public async Task<int> GetStartingPostNumber(IPageProvider pageProvider, IQuest quest, CancellationToken token)
        {
            // Use the provided start post if we aren't trying to find the threadmarks.
            if (!quest.CheckForLastThreadmark)
                return quest.StartPost;

            // Attempt to get the starting post number from threadmarks, if that option is checked.
            var threadmarkPage = await pageProvider.GetPage(GetThreadmarksPageUrl(quest.Name), "Threadmarks", true, token).ConfigureAwait(false);

            var threadmarks = GetThreadmarksFromPage(threadmarkPage);

            if (threadmarks == null || !threadmarks.Any())
                return quest.StartPost;

            var lastThreadmark = threadmarks.Last();
            string threadmarkUrl = GetUrlOfThreadmark(lastThreadmark);
            string postId = GetPostIdFromUrl(threadmarkUrl);

            var lastThreadmarkPage = await pageProvider.GetPage(threadmarkUrl, postId, false, token).ConfigureAwait(false);

            var threadmarkPost = GetPostFromPageById(lastThreadmarkPage, postId);
            int threadmarkPostNumber = GetPostNumberOfPost(threadmarkPost);

            if (threadmarkPostNumber > 0)
                return threadmarkPostNumber + 1;
            else
                return quest.StartPost;
        }

        #endregion


        // Utility functions to support the above interface functions

        #region Functions dealing with pages
        /// <summary>
        /// Get the HTML node of the page that represents the top-level element
        /// that contains all the primary content.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>The element containing the main page content.</returns>
        private HtmlNode GetPageContent(HtmlDocument page)
        {
            return page.DocumentNode?.Descendants("div").FirstOrDefault(a => a.Id == "content");
        }

        /// <summary>
        /// Get the node element containing all the posts on the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the page element that contains the posts.</returns>
        private HtmlNode GetPostListFromPage(HtmlDocument page)
        {
            var pageContent = GetPageContent(page);

            if (pageContent.GetAttributeValue("class", "").Contains("thread_view"))
            {
                return pageContent.Descendants("ol").First(n => n.Id == "messageList");
            }

            throw new InvalidOperationException("Page is not a forum thread.");
        }

        /// <summary>
        /// Given the page node containing all of the posts on the thread,
        /// get an IEnumerable list of all posts on the page.
        /// </summary>
        /// <param name="postList">Element containing posts from the page.</param>
        /// <returns>Returns a list of posts.</returns>
        private IEnumerable<HtmlNode> GetPostsFromList(HtmlNode postList)
        {
            if (postList.Name == "ol" && postList.Id == "messageList")
            {
                return postList.ChildNodes.Where(n => n.Name == "li");
            }

            throw new InvalidOperationException("Provided post list is not valid.");
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
            if (!id.StartsWith("post-"))
                id = "post-" + id;

            var postsList = GetPostsFromPage(page);

            return postsList.FirstOrDefault(a => a.Id == id);
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
            // A post is a <li> node:
            // <li id = "post-2948970" class="message " data-author="Fanhunter696">

            return post.Name == "li" && post.GetAttributeValue("class", "").Contains("message") && post.Id.StartsWith("post-");
        }

        /// <summary>
        /// Gets the inner HTML node containing the actual contents of the post.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the article node within the post.</returns>
        private HtmlNode GetContentsOfPost(HtmlNode post)
        {
            if (IsPost(post))
            {
                // There is only one <article> node within each post, which contains the post contents.
                return post.Descendants("article").Single();
            }

            throw new InvalidOperationException("Cannot get post threadmarker.  Does not appear to be a valid post.");
        }

        #endregion

        #region Functions for threadmarks
        /// <summary>
        /// Given a page (presumably the threadmarks page), attempt to
        /// get the list node that holds all the threadmarks.
        /// </summary>
        /// <param name="page">The page to check.</param>
        /// <returns>Returns the element containing the threadmarks,
        /// or null if it's not a valid page.</returns>
        private HtmlNode GetThreadmarksListFromPage(HtmlDocument page)
        {
            var pageContent = GetPageContent(page);

            if (pageContent.GetAttributeValue("class", "").Contains("threadmarks"))
            {
                return pageContent.Descendants("ol").First(n => n.GetAttributeValue("class", "").Contains("overlayScroll"));
            }
            else if (pageContent.GetAttributeValue("class", "").Contains("error"))
            {
                // An class of "error" indicates that there are no threadmarks for this thread.
                return null;
            }

            throw new InvalidOperationException("Page is not a threadmarks listing.");
        }

        /// <summary>
        /// Given a list of threadmarks, return an IEnumerable of the threadmark
        /// entries.
        /// </summary>
        /// <param name="list">The list of potential threadmarks.</param>
        /// <returns>Returns an enumerable list of the threadmarks, or
        /// null if it fails.</returns>
        private IEnumerable<HtmlNode> GetThreadmarksFromThreadmarksList(HtmlNode list)
        {
            return list?.Elements("li");
        }

        /// <summary>
        /// Given a page (presumably the threadmarks page), attempt to
        /// get an enumeration list of all the threadmarks from the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns an enumerable list of threadmarks, or
        /// null if it fails.</returns>
        private IEnumerable<HtmlNode> GetThreadmarksFromPage(HtmlDocument page)
        {
            var threadmarksList = GetThreadmarksListFromPage(page);

            return GetThreadmarksFromThreadmarksList(threadmarksList);
        }

        /// <summary>
        /// Given a threadmark entry from a list of threadmarks, get
        /// the URL of the post the threadmark is for.
        /// </summary>
        /// <param name="threadmarkEntry">A threadmark list entry.</param>
        /// <returns>Returns the full URL to the post.</returns>
        private string GetUrlOfThreadmark(HtmlNode threadmarkEntry)
        {
            return GetUrlFromRelativeAddress(threadmarkEntry.Element("a").GetAttributeValue("href", ""));
        }

        /// <summary>
        /// Given a url for a post, using its unique ID, extract the ID.
        /// </summary>
        /// <param name="url">A url with a post's unique ID in it.</param>
        /// <returns>Returns the post's ID.</returns>
        private string GetPostIdFromUrl(string url)
        {
            Regex postLinkRegex = new Regex(@"posts/(?<postId>\d+)/");
            var m = postLinkRegex.Match(url);
            if (m.Success)
                return m.Groups["postId"].Value;

            throw new ArgumentException("Unable to extract post ID from link:\n" + url, nameof(url));
        }

        /// <summary>
        /// Get the threadmarker node of the post, if any.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the threadmarker node of the post, or null if none is found.</returns>
        private HtmlNode GetThreadmarkerOfPost(HtmlNode post)
        {
            if (IsPost(post))
            {
                // The threadmarker of a post is contained in a div directly under the <li>, with class="threadmarker".
                return post.Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("threadmarker"));
            }

            throw new InvalidOperationException("Cannot get post threadmarker.  Does not appear to be a valid post.");
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
            StringBuilder sb = new StringBuilder();

            // Search the post for valid element types, and put them all together
            // into a single string.
            foreach (var childNode in node.ChildNodes)
            {
                string nodeClass = childNode.GetAttributeValue("class", "");

                // Once we reach the end marker of the post, no more processing is needed.
                if (nodeClass.Contains("messageTextEndMarker"))
                    return sb.ToString();

                // If we encounter a quote, skip past it
                if (nodeClass.Contains("bbCodeQuote"))
                    continue;

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
                        // Recurse into divs
                        sb.Append(ExtractNodeText(childNode));
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
            postText = postText.TrimStart();
            postText = HtmlEntity.DeEntitize(postText);
            postText = badCharactersRegex.Replace(postText, "");
            return postText;
        }
        #endregion
    }
}
