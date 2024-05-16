using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Extensions;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Tally.ComponentsF.Threads;
using NetTally.Web;

namespace NetTally.Input.Forums.ForumAdaptersF
{
    public partial class VBulletin4Adapter(
        IOptions<GlobalSettings> options,
        ILogger<VBulletin4Adapter> logger) : IForumAdapter
    {
        readonly GlobalSettings inputOptions = options.Value;
        readonly ILogger<VBulletin4Adapter> logger = logger;

        #region IForumAdapter interface
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

            // http://forums.militarytimes.com/showthread.php/9961-Furlough
            // http://forums.militarytimes.com/showthread.php/9961-Furlough/page2

            string append = page > 1 ? $"/page{page}" : "";

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

            var posts = from p in GetPostList(page)
                        where p != null
                        let post = GetPost(page, p, quest)
                        where post != null
                        select post;

            return posts;
        }

        /// <summary>
        /// Get information about the thread.
        /// This includes title, author, and starting range.
        /// </summary>
        /// <param name="quest">The quest being queried.</param>
        /// <param name="pageProvider">A page provider for loading pages.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns><see cref="ThreadInformationType"/> containing thread information.</returns>
        public async Task<ThreadInformationType?> GetThreadInformationAsync(
            Quest quest,
            IPageProvider pageProvider,
            CancellationToken token)
        {
            var infoPage = await GetInfoPageAsync(quest, pageProvider, token);

            if (infoPage == null) return null;

            return GetThreadInfo(infoPage, quest);
        }

        #endregion IForumAdapter interface

        #region IForumAdapter support
        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        private ThreadInformationType GetThreadInfo(HtmlDocument page, Quest quest)
        {
            string title = GetPageTitle(page);
            var author = Author.Unknown; // vBulletin doesn't show thread authors
            int pages = GetMaxPageNumberOfThread(page);

            var info = ThreadInformation.CreateByPostNumber(title, author, quest.StartPost, pages);

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
                CachingMode.BypassCache, ShouldCache.Yes,
                SuppressNotifications.Yes, token)
                .ConfigureAwait(false);

            return page;
        }
        #endregion IForumAdapter support

        #region Get Page Information
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
            // Get the number of pages from the navigation elements
            var paginationTop = page.GetElementbyId("pagination_top");

            var paginationForm = paginationTop.Element("form");

            // If there is no form, that means there's only one page in the thread.
            if (paginationForm != null)
            {
                var firstSpan = paginationForm.Element("span");
                var firstSpanA = firstSpan?.Element("a");
                var pagesText = firstSpanA?.InnerText;

                if (pagesText != null)
                {
                    Regex pageNumsRegex = PageNumsRegex();
                    Match m = pageNumsRegex.Match(pagesText);
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
        private static IEnumerable<HtmlNode> GetPostList(HtmlDocument page)
        {
            var postList = page?.GetElementbyId("posts");

            if (postList == null)
                return [];

            return postList.Elements("li");
        }

        private PostType? GetPost(HtmlDocument page, HtmlNode li, Quest quest)
        {
            if (li == null)
                return null;

            var id = GetPostId(li);
            var author = GetPostAuthor(li);
            int number = GetPostNumber(page, id);
            string text = GetPostText(li, id, quest);

            if (inputOptions.TrackPostAuthorsUniquely)
                author = author with { Name = $"{author.Name}_{id.Id}" };

            var origin = Origin.CreateUser(author, quest.ThreadUri, GetPermalinkForId(quest.ThreadUri, id), id, number);
            var post = Post.Create(origin, text);

            return post;
        }

        private static PostIdType GetPostId(HtmlNode li)
        {
            string id = li.Id["post_".Length..];
            return PostId.Create(id) ?? PostId.Zero;
        }

        private static AuthorType GetPostAuthor(HtmlNode li)
        {
            string author = "";

            HtmlNode? postDetails = li.Elements("div").FirstOrDefault(n => n.GetAttributeValue("class", "") == "postdetails");

            if (postDetails != null)
            {
                // Author
                HtmlNode? userinfo = postDetails.GetChildWithClass("div", "userinfo");
                HtmlNode? username = userinfo?.GetChildWithClass("a", "username");
                author = ForumPostTextConverter.CleanupWebString(username?.InnerText);
            }

            return Author.Create(author);
        }

        private static int GetPostNumber(HtmlDocument page, PostIdType id)
        {
            var postCount = page.GetElementbyId($"postcount{id.Id}");

            if (postCount != null)
                return int.Parse(postCount.GetAttributeValue("name", "0"));

            return 0;
        }

        private static string GetPostText(HtmlNode li, PostIdType id, Quest quest)
        {
            HtmlNode? postDetails = li.Elements("div").FirstOrDefault(n => n.GetAttributeValue("class", "") == "postdetails");

            if (postDetails != null)
            {
                // Text
                string postMessageId = $"post_message_{id.Id}";

                var message = li.OwnerDocument.GetElementbyId(postMessageId)?.Element("blockquote");

                // Predicate filtering out elements that we don't want to include
                var exclusion = ForumPostTextConverter.GetClassExclusionPredicate("bbcode_quote");

                Uri host = new(quest.ThreadUri.GetLeftPart(UriPartial.Authority) + "/"); ;

                // Get the full post text.
                return ForumPostTextConverter.ExtractPostText(message, exclusion, host);
            }

            return "";
        }
        #endregion Get Posts

        #region URL Manipulation
        /// <summary>
        /// Get the URL string up to the end of any directory paths.
        /// </summary>
        /// <param name="uri">The URI to derive the URL from.</param>
        /// <returns>Returns a string containing the URL up to the last path.</returns>
        private static string GetBaseThreadUrl(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            // http://forums.militarytimes.com/showthread.php/9961-Furlough

            StringBuilder sb = new();

            sb.Append(uri.GetLeftPart(UriPartial.Authority));

            bool found = false;

            foreach (var segment in uri.Segments)
            {
                if (found)
                {
                    sb.Append(segment.TrimEnd('/'));
                    break;
                }
                else
                {
                    sb.Append(segment);
                }

                if (segment.StartsWith("showthread.php"))
                    found = true;
            }

            return sb.ToString();
        }

        private static Uri GetPermalinkForId(Uri uri, PostIdType postId)
        {
            // http://forums.militarytimes.com/showthread.php/9961-Furlough?p=371392&viewfull=1#post371392

            string url = $"{GetBaseThreadUrl(uri)}?p={postId.Id}&viewfull=1#post{postId.Id}";
            return new Uri(url);
        }

        [GeneratedRegex(@"Page \d+ of (?<pages>\d+)")]
        private static partial Regex PageNumsRegex();
        #endregion URL Manipulation
    }
}
