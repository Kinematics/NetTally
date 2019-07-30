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
    public class PhpBBAdapter2 : IForumAdapter2
    {
        #region Constructor
        readonly IGeneralInputOptions inputOptions;

        public PhpBBAdapter2(IGeneralInputOptions inputOptions)
        {
            this.inputOptions = inputOptions;
        }
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
            return 25;
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

            int skipPosts = quest.PostsPerPage * (page - 1);

            string append = skipPosts > 0 ? $"&start={skipPosts}" : "";

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
            string author = string.Empty; // PhpBB doesn't show thread authors
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
        public IEnumerable<Post> GetPosts(HtmlDocument page, IQuest quest, int pageNumber)
        {
            if (quest == null || quest.ThreadUri == null || quest.ThreadUri == Quest.InvalidThreadUri)
                return Enumerable.Empty<Post>();

            int postNumber = (pageNumber - 1) * quest.PostsPerPage + 1;

            var posts = from p in GetPostList(page)
                        where p != null
                        let post = GetPost(p, quest, postNumber++)
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
            // Find the number of pages
            var pagebody = page.GetElementbyId("page-body");

            if (pagebody != null)
            {
                // Different versions of the forum have different methods of showing page numbers

                var topicactions = pagebody.GetChildWithClass("topic-actions");
                if (topicactions != null)
                {
                    HtmlNode? pagination = topicactions.GetChildWithClass("pagination");
                    string? paginationText = pagination?.InnerText;
                    if (paginationText != null)
                    {
                        Regex pageOf = new Regex(@"Page\s*\d+\s*of\s*(?<pages>\d+)");
                        Match m = pageOf.Match(paginationText);
                        if (m.Success)
                            return int.Parse(m.Groups["pages"].Value);
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
                        return int.Parse(lastPageLink.InnerText);
                    }
                }
            }

            return 1;
        }
        #endregion Get Page Information

        #region Get Posts
        private IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
        {
            var pagebody = page?.GetElementbyId("page-body");

            if (pagebody == null)
                return Enumerable.Empty<HtmlNode>();

            return pagebody.Elements("div").Where(p => p.HasClass("post"));
        }

        private Post? GetPost(HtmlNode div, IQuest quest, int postNumber)
        {
            if (div == null)
                return null;

            string id = GetPostId(div);
            string author = GetPostAuthor(div);
            int number = postNumber;
            string text = GetPostText(div, quest);

            if (inputOptions.TrackPostAuthorsUniquely)
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

        private string GetPostId(HtmlNode div)
        {
            return div.Id.Substring("p".Length);
        }

        private string GetPostAuthor(HtmlNode div)
        {
            HtmlNode? inner = div.GetChildWithClass("div", "inner");
            HtmlNode? postbody = inner?.GetChildWithClass("div", "postbody");
            HtmlNode? authorNode = postbody?.GetChildWithClass("p", "author");
            HtmlNode? authorStrong = authorNode?.Descendants("strong").FirstOrDefault();
            HtmlNode? authorAnchor = authorStrong?.Element("a");

            return ForumPostTextConverter.CleanupWebString(authorAnchor?.InnerText);
        }

        private string GetPostText(HtmlNode div, IQuest quest)
        {
            // Get the full post text.  Two different layout variants.
            HtmlNode? inner = div.GetChildWithClass("div", "inner");
            HtmlNode? postbody = inner?.GetChildWithClass("div", "postbody");
            var content = postbody?.GetChildWithClass("div", "content");
            if (content == null)
                content = postbody?.Elements("div").FirstOrDefault(n => n.Id.StartsWith("post_content", StringComparison.Ordinal));

            if (content != null)
            {
                return ForumPostTextConverter.ExtractPostText(content, n => false, quest.ThreadUri);
            }

            return "";
        }
        #endregion Get Posts

        #region URL Manipulation
        static readonly Regex threadNumberRegex = new Regex(@"[\?&]t=(?<thread>\d+)");

        /// <summary>
        /// Get the URL string up to the end of any directory paths.
        /// </summary>
        /// <param name="uri">The URI to derive the URL from.</param>
        /// <returns>Returns a string containing the URL up to the last path.</returns>
        private string GetBaseThreadUrl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            // http://www.ilovephilosophy.com/viewtopic.php?t=175054
            // http://www.ilovephilosophy.com/viewtopic.php?f=1&t=175054

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

            // http://www.ilovephilosophy.com/viewtopic.php?p=2216430#p2216430

            string auth = uri.GetLeftPart(UriPartial.Authority);
            string page = uri.AbsolutePath;

            return $"{auth}{page}?p=";
        }

        private string GetPermalinkForId(Uri uri, string postId)
        {
            return $"{GetHostBasePostsUrl(uri)}{postId}#p{postId}";
        }
        #endregion URL Manipulation
    }
}
