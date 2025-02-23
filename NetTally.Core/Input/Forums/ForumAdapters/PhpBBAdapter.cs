using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Extensions;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Threads;
using NetTally.Web;

namespace NetTally.Input.Forums.ForumAdapters
{
    public partial class PhpBBAdapter(
        IOptions<GlobalSettings> options,
        ILogger<PhpBBAdapter> logger) : IForumAdapter
    {
        #region Constructor
        readonly GlobalSettings inputOptions = options.Value;
        readonly ILogger<PhpBBAdapter> logger = logger;
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
        public BoolEx HasRssThreadmarksFeed(Uri uri)
        {
            return BoolEx.False;
        }

        /// <summary>
        /// Get a proper URL for a specific page of a thread of the URI provided.
        /// </summary>
        /// <param name="uri">The URI of the site that we're constructing a URL for.</param>
        /// <param name="page">The page number to create a URL for.</param>
        /// <returns>Returns a URL for the page requested.</returns>
        public string GetUrlForPage(Quest quest, int page)
        {
            if (page < 1)
                throw new ArgumentException($"Invalid page number: {page}", nameof(page));

            int skipPosts = quest.PostsPerPage * (page - 1);

            string append = skipPosts > 0 ? $"&start={skipPosts}" : "";

            return $"{GetBaseThreadUrl(quest.ThreadUri)}{append}";
        }

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <param name="quest">The quest being tallied, which may have options that we need to consider.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        public IEnumerable<PostType> GetPosts(HtmlDocument page, Quest quest, int pageNumber)
        {
            if (quest == null || quest.ThreadUri == null || quest.ThreadUri == Quest.InvalidThreadUri)
                return [];

            int postNumber = (pageNumber - 1) * quest.PostsPerPage + 1;

            var posts = from p in GetPostList(page)
                        where p != null
                        let post = GetPost(p, quest, postNumber++)
                        where post != null
                        select post;

            return posts;
        }


        public async Task<ThreadInfo>
            GetThreadInfoAsync(Quest quest, IPageProvider pageProvider, CancellationToken token)
        {
            var infoPage = await GetInfoPageAsync(quest, pageProvider, token);

            if (infoPage == null) return ThreadInfos.None;

            return GetThreadInfo(infoPage, quest);
        }

        #endregion IForumAdapter interface

        #region IForumAdapter support
        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <param name="quest">The quest we're getting info for.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        private static ThreadInfo GetThreadInfo(HtmlDocument page, Quest quest)
        {
            string title = GetPageTitle(page);
            var author = Author.Unknown; // PhpBB doesn't show thread authors
            int pages = GetMaxPageNumberOfThread(page);

            var range = ThreadRanges.CreateByRange(quest.StartPost, quest.EndPost, quest.PostsPerPage, pages);
            var info = ThreadInfos.Create(title, author, range);

            return info;
        }

        private async Task<HtmlDocument?> GetInfoPageAsync(
            Quest quest,
            IPageProvider pageProvider,
            CancellationToken token)
        {
            string infoPageUrl = GetUrlForPage(quest, 1);

            // Make sure to bypass the cache, since it may have changed since the last load.
            HtmlDocument? page = await pageProvider.GetHtmlDocumentAsync(
                infoPageUrl, "Info Page",
                CachingMode.WriteOnly,
                SuppressNotifications.Yes, token)
                .ConfigureAwait(false);

            return page;
        }
        #endregion IForumAdapter support

        #region Get Page Information
        [GeneratedRegex(@"Page\s*\d+\s*of\s*(?<pages>\d+)")]
        private static partial Regex PageOfRegex { get; }

        private static string GetPageTitle(HtmlDocument page)
        {
            return ForumPostTextConverter.CleanupWebString(
                page.DocumentNode
                    .Element("html")
                    .Element("head")
                    ?.Element("title")
                    ?.InnerText);
        }

        private static int GetMaxPageNumberOfThread(HtmlDocument page)
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
                        Match m = PageOfRegex.Match(paginationText);
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
        private static IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
        {
            var pagebody = page?.GetElementbyId("page-body");

            if (pagebody == null)
                return [];

            return pagebody.Elements("div").Where(p => p.HasClass("post"));
        }

        private PostType? GetPost(HtmlNode div, Quest quest, int postNumber)
        {
            if (div == null)
                return null;

            var id = GetPostId(div);
            var author = GetPostAuthor(div);
            int number = postNumber;
            string text = GetPostText(div, quest);

            if (inputOptions.TrackPostAuthorsUniquely)
                author = author with { Name = $"{author.Name}_{id.Id}" };

            var origin = Origin.CreateUser(author, quest.ThreadUri, GetPermalinkForId(quest.ThreadUri, id), id, number);
            var post = Post.Create(origin, text);

            return post;
        }

        private static PostIdType GetPostId(HtmlNode div)
        {
            var idString = div.Id["p".Length..];
            var id = PostId.Create(idString);

            return id ?? PostId.Zero;
        }

        private static AuthorType GetPostAuthor(HtmlNode div)
        {
            HtmlNode? inner = div.GetChildWithClass("div", "inner");
            HtmlNode? postbody = inner?.GetChildWithClass("div", "postbody");
            HtmlNode? authorNode = postbody?.GetChildWithClass("p", "author");
            HtmlNode? authorStrong = authorNode?.Descendants("strong").FirstOrDefault();
            HtmlNode? authorAnchor = authorStrong?.Element("a");

            string authorName = ForumPostTextConverter.CleanupWebString(authorAnchor?.InnerText);

            return Author.Create(authorName);
        }

        private static string GetPostText(HtmlNode div, Quest quest)
        {
            // Get the full post text.  Two different layout variants.
            HtmlNode? inner = div.GetChildWithClass("div", "inner");
            HtmlNode? postbody = inner?.GetChildWithClass("div", "postbody");
            var content = postbody?.GetChildWithClass("div", "content");
            content ??= postbody?.Elements("div").FirstOrDefault(n => n.Id.StartsWith("post_content", StringComparison.Ordinal));

            if (content != null)
            {
                return ForumPostTextConverter.ExtractPostText(content, n => false, quest.ThreadUri);
            }

            return "";
        }
        #endregion Get Posts

        #region URL Manipulation
        [GeneratedRegex(@"[\?&]t=(?<thread>\d+)")]
        private static partial Regex ThreadNumberRegex { get; }

        /// <summary>
        /// Get the URL string up to the end of any directory paths.
        /// </summary>
        /// <param name="uri">The URI to derive the URL from.</param>
        /// <returns>Returns a string containing the URL up to the last path.</returns>
        private static string GetBaseThreadUrl(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            // http://www.ilovephilosophy.com/viewtopic.php?t=175054
            // http://www.ilovephilosophy.com/viewtopic.php?f=1&t=175054

            string auth = uri.GetLeftPart(UriPartial.Authority);
            string page = uri.AbsolutePath;

            Match m = ThreadNumberRegex.Match(uri.Query);
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
        private static string GetHostBasePostsUrl(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            // http://www.ilovephilosophy.com/viewtopic.php?p=2216430#p2216430

            string auth = uri.GetLeftPart(UriPartial.Authority);
            string page = uri.AbsolutePath;

            return $"{auth}{page}?p=";
        }

        private static Uri GetPermalinkForId(Uri uri, PostIdType postId)
        {
            string url = $"{GetHostBasePostsUrl(uri)}{postId.Id}#p{postId.Id}";
            return new Uri(url);
        }
        #endregion URL Manipulation
    }
}
