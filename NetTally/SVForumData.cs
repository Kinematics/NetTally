using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public class SVForumData : IForumData
    {
        const string ForumUrl = "http://forums.sufficientvelocity.com/";
        const string ThreadsUrl = "http://forums.sufficientvelocity.com/threads/";
        const string PostsUrl = "http://forums.sufficientvelocity.com/posts/";

        #region String concatenation functions for constructing URLs.
        public string GetThreadsUrl(string questTitle) => ThreadsUrl + questTitle;

        public string GetThreadPageBaseUrl(string questTitle) => GetThreadsUrl(questTitle) + "/page-";

        public string GetThreadmarksPageUrl(string questTitle) => GetThreadsUrl(questTitle) + "/threadmarks";

        public string GetPageUrl(string questTitle, int page) => GetThreadPageBaseUrl(questTitle) + page.ToString();

        public string GetPostUrlFromId(string postId) => PostsUrl + postId;

        public string GetUrlFromRelativeAddress(string relative) => ForumUrl + relative;
        #endregion


        #region Functions for extracting data from threads
        public HtmlNode GetPageContent(HtmlDocument page)
        {
            return page.DocumentNode?.Descendants("div").FirstOrDefault(a => a.Id == "content");
        }

        /// <summary>
        /// Gets the author of the thread.
        /// </summary>
        /// <param name="page">The HTML page being examined.</param>
        /// <returns>Returns the author of the thread.</returns>
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
                    throw new ArgumentException("Unable to extract author from provided page.");

                return HtmlEntity.DeEntitize(usernameA.InnerText);
            }

            throw new ArgumentException("Page is not a forum thread.");
        }


        public HtmlNode GetPostListFromPage(HtmlDocument page)
        {
            var pageContent = GetPageContent(page);

            if (pageContent.GetAttributeValue("class", "").Contains("thread_view"))
            {
                return pageContent.Descendants("ol").First(n => n.Id == "messageList");
            }

            throw new ArgumentException("Page is not a forum thread.");
        }

        public IEnumerable<HtmlNode> GetPostsFromList(HtmlNode postList)
        {
            if (postList.Name == "ol" && postList.Id == "messageList")
            {
                return postList.ChildNodes.Where(n => n.Name == "li");
            }

            throw new ArgumentException("Provided post list is not valid.");
        }

        public IEnumerable<HtmlNode> GetPostsFromPage(HtmlDocument page)
        {
            var postList = GetPostListFromPage(page);
            return GetPostsFromList(postList);
        }

        #endregion



        #region Functions for extracting data from posts.
        /// <summary>
        /// Function to tell if the provided HTML node is a thread post.
        /// </summary>
        /// <param name="post">Proposed post.</param>
        /// <returns>Returns true if the node appears to be a legitimate post.</returns>
        public bool IsPost(HtmlNode post)
        {
            // A post is a <li> node:
            // <li id = "post-2948970" class="message " data-author="Fanhunter696">

            return post.Name == "li" && post.GetAttributeValue("class", "").Contains("message") && post.Id.StartsWith("post-");
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

            throw new ArgumentException("Cannot get ID.  Does not appear to be a valid post.");
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

            throw new ArgumentException("Unable to extract a post number from the post.");
        }

        /// <summary>
        /// Calculate the page number that corresponds to the post number given.
        /// </summary>
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        public int GetPageNumberFromPostNumber(int postNumber) => ((postNumber - 1) / 25) + 1;

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

            throw new ArgumentException("Cannot get post author.  Does not appear to be a valid post.");

        }

        /// <summary>
        /// Get the threadmarker node of the post, if any.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the threadmarker node of the post, or null if none is found.</returns>
        public HtmlNode GetThreadmarkerOfPost(HtmlNode post)
        {
            if (IsPost(post))
            {
                // The threadmarker of a post is contained in a div directly under the <li>, with class="threadmarker".
                return post.Elements("div").FirstOrDefault(a => a.GetAttributeValue("class", "").Contains("threadmarker"));
            }

            throw new ArgumentException("Cannot get post threadmarker.  Does not appear to be a valid post.");
        }

        /// <summary>
        /// Gets the inner HTML node containing the actual contents of the post.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the article node within the post.</returns>
        public HtmlNode GetContentsOfPost(HtmlNode post)
        {
            if (IsPost(post))
            {
                // There is only one <article> node within each post, which contains the post contents.
                return post.Descendants("article").Single();
            }

            throw new ArgumentException("Cannot get post threadmarker.  Does not appear to be a valid post.");
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

        // \u200b = Zero width space (8203 decimal/html).  Trim() does not remove this character.
        Regex badCharactersRegex = new Regex("\u200b");

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

    }
}
