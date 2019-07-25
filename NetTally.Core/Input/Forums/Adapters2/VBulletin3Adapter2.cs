using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Extensions;
using NetTally.Options;
using NetTally.Web;

namespace NetTally.Forums.Adapters2
{
    public class VBulletin3Adapter2 : IForumAdapter2
    {
        #region Static data
        #endregion

        #region IForumAdapter2 interface
        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns the string to use for a line break event when outputting the tally.</returns>
        public string GetDefaultLineBreak(Uri uri)
        {
            return "———————————————————————————————————————————————————————";
        }

        /// <summary>
        /// Get the default number of posts per page for the site used by the origin.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns a default number of posts per page for the given site.</returns>
        public int GetDefaultPostsPerPage(Uri uri)
        {
            return 20;
        }

        /// <summary>
        /// Gets whether the provided URI host is known to use RSS feeds for its threadmarks.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns whether the site is known to use or not use RSS threadmarks.</returns>
        public BoolEx GetHasRssThreadmarksFeed(Uri uri)
        {
            return BoolEx.False;
        }

        /// <summary>
        /// Get a proper URL for a specific page of a thread of the URI provided.
        /// </summary>
        /// <param name="uri">The URI of the site that we're constructing a URL for.</param>
        /// <param name="page">The page number to create a URL for.</param>
        /// <returns>Returns a URL for the page requested.</returns>
        public string GetUrlForPage(IQuest quest, int page)
        {
            if (page < 1)
                throw new ArgumentException($"Invalid page number: {page}", nameof(page));

            string append = page > 1 ? $"&page={page}" : "";

            return $"{GetBaseThreadUrl(quest.ThreadUri)}{append}";
        }

        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        public ThreadInfo GetThreadInfo(HtmlDocument page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            string title = GetPageTitle(page);
            string author = string.Empty; // vBulletin doesn't show thread authors
            int pages = GetMaxPageNumberOfThread(page);

            ThreadInfo info = new ThreadInfo(title, author, pages);

            return info;
        }

        /// <summary>
        /// Gets the range of post numbers to tally, for the given quest.
        /// This may require loading information from the site.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="pageProvider">The page provider to use to load any needed pages.</param>
        /// <param name="token">The cancellation token to check for cancellation requests.</param>
        /// <returns>Returns a ThreadRangeInfo describing which pages to load for the tally.</returns>
        public Task<ThreadRangeInfo> GetQuestRangeInfoAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (pageProvider == null)
                throw new ArgumentNullException(nameof(pageProvider));

            return Task.FromResult(new ThreadRangeInfo(true, quest.StartPost));
        }

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <param name="quest">The quest being tallied, which may have options that we need to consider.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        public IEnumerable<Post> GetPosts(HtmlDocument page, IQuest quest)
        {
            if (quest == null || quest.ThreadUri == null || quest.ThreadUri == Quest.InvalidThreadUri)
                return Enumerable.Empty<Post>();

            var posts = from p in GetPostList(page)
                        where p != null
                        let post = GetPost(page, p, quest)
                        where post != null
                        select post;

            return posts;
        }
        #endregion IForumAdapter2 interface

        #region Get Page Information
        private string GetPageTitle(HtmlDocument page)
        {
            return ForumPostTextConverter.CleanupWebString(
                page.DocumentNode
                    .Element("html")
                    .Element("head")
                    ?.Element("title")
                    ?.InnerText);
        }

        private int GetMaxPageNumberOfThread(HtmlDocument page)
        {
            // If there's no pagenav div, that means there's no navigation to alternate pages,
            // which means there's only one page in the thread.
            var pageNavDiv = page.DocumentNode.Element("html").Element("body").GetDescendantWithClass("div", "pagenav");

            if (pageNavDiv != null)
            {
                var vbMenuControl = pageNavDiv.GetDescendantWithClass("td", "vbmenu_control");

                if (vbMenuControl != null)
                {
                    Regex pageNumsRegex = new Regex(@"Page \d+ of (?<pages>\d+)");

                    Match m = pageNumsRegex.Match(vbMenuControl.InnerText);
                    if (m.Success)
                    {
                        return int.Parse(m.Groups["pages"].Value);
                    }
                }
            }

            return 1;
        }
        #endregion Get Page Information

        #region Get Posts
        private IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
        {
            var postList = page?.GetElementbyId("posts");

            if (postList == null)
                return Enumerable.Empty<HtmlNode>();

            return postList.Elements("div");
        }

        private Post? GetPost(HtmlDocument page, HtmlNode div, IQuest quest)
        {
            if (div == null)
                return null;

            var table = div.Descendants("table").FirstOrDefault(a => a.Id.StartsWith("post", StringComparison.Ordinal));

            if (table == null)
                return null;

            string id = GetPostId(table);
            string author = GetPostAuthor(page, id);
            int number = GetPostNumber(page, id);
            string text = GetPostText(page, id, quest);

            if (AdvancedOptions.Instance.DebugMode)
                author = $"{author}_{id}";

            try
            {
                Origin origin = new Origin(author, id, number, quest.ThreadUri, GetPermalinkForId(quest.ThreadUri, id));
                return new Post(origin, text);
            }
            catch (Exception e)
            {
                Logger.Error($"Attempt to create new post failed. (Author:{author}, ID:{id}, Number:{number}, Quest:{quest.DisplayName})", e);
            }

            return null;
        }

        private string GetPostId(HtmlNode table)
        {
            return table.Id.Substring("post".Length);
        }

        private string GetPostAuthor(HtmlDocument page, string id)
        {
            string author = "";
            string postAuthorDivID = $"postmenu_{id}";

            var authorAnchor = page.GetElementbyId(postAuthorDivID).Element("a");

            if (authorAnchor != null)
            {
                // ??
                if (authorAnchor.Element("span") != null)
                {
                    author = authorAnchor.Element("span").InnerText;
                }
                else
                {
                    author = authorAnchor.InnerText;
                }
            }

            return ForumPostTextConverter.CleanupWebString(author);
        }

        private string GetPostText(HtmlDocument page, string id, IQuest quest)
        {
            string postMessageId = $"post_message_{id}";

            var postContents = page.GetElementbyId(postMessageId);

            // Predicate filtering out elements that we don't want to include
            var exclusion = ForumPostTextConverter.GetClassExclusionPredicate("bbcode_quote");

            Uri host = new Uri(quest.ThreadUri.GetLeftPart(UriPartial.Authority) + "/"); ;

            // Get the full post text.
            return ForumPostTextConverter.ExtractPostText(postContents, exclusion, host);
        }

        private int GetPostNumber(HtmlDocument page, string id)
        {
            string postNumberAnchorID = $"postcount{id}";

            var anchor = page.GetElementbyId(postNumberAnchorID);

            if (anchor != null)
            {
                string postNumText = anchor.GetAttributeValue("name", "");
                return int.Parse(postNumText);
            }

            return 0;
        }
        #endregion Get Posts

        #region URL Manipulation
        static readonly Regex threadNumberRegex = new Regex(@"\?t=(?<thread>\d+)");

        /// <summary>
        /// Get the URL string up to the end of any directory paths.
        /// </summary>
        /// <param name="uri">The URI to derive the URL from.</param>
        /// <returns>Returns a string containing the URL up to the last path.</returns>
        private string GetBaseThreadUrl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            // https://forums.animesuki.com/showthread.php?t=152155

            string auth = uri.GetLeftPart(UriPartial.Authority);
            string page = uri.AbsolutePath;

            Match m = threadNumberRegex.Match(uri.Query);
            if (m.Success)
            {
                return $"{auth}{page}?t={m.Groups["thread"].Value}";
            }

            throw new ArgumentException("URI has no thread number.", nameof(uri));
        }

        /// <summary>
        /// Gets the URL string up to start of the query for posts.
        /// </summary>
        /// <param name="uri">The URI to derive the URL from.</param>
        /// <returns>Returns a string containing the URL up to the posts query.</returns>
        private string GetHostBasePostsUrl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            // https://forums.animesuki.com/showthread.php?p=6355458

            string auth = uri.GetLeftPart(UriPartial.Authority);
            string page = uri.AbsolutePath;

            return $"{auth}{page}?p=";
        }

        private string GetPermalinkForId(Uri uri, string postId)
        {
            return $"{GetHostBasePostsUrl(uri)}{postId}";
        }
        #endregion URL Manipulation
    }
}
