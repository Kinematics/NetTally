using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Web;

namespace NetTally.Forums
{
    public interface IForumAdapter2
    {
        /// <summary>
        /// Get the default number of posts per page for the site used by the origin.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns a default number of posts per page for the given site.</returns>
        int GetDefaultPostsPerPage(Uri uri);

        /// <summary>
        /// Gets whether the provided URI host is known to use RSS feeds for its threadmarks.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns whether the site is known to use or not use RSS threadmarks.</returns>
        BoolEx GetHasRssThreadmarksFeed(Uri uri);

        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        /// <param name="uri">The uri of the site that we're querying information for.</param>
        /// <returns>Returns the string to use for a line break event when outputting the tally.</returns>
        string GetDefaultLineBreak(Uri uri);


        /// <summary>
        /// Get a proper URL for a specific page of a thread of the URI provided.
        /// </summary>
        /// <param name="uri">The URI of the site that we're constructing a URL for.</param>
        /// <param name="page">The page number to create a URL for.</param>
        /// <returns>Returns a URL for the page requested.</returns>
        string GetUrlForPage(Uri uri, int page);

        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        ThreadInfo GetThreadInfo(HtmlDocument page);

        /// <summary>
        /// Gets the range of post numbers to tally, for the given quest.
        /// This may require loading information from the site.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="pageProvider">The page provider to use to load any needed pages.</param>
        /// <param name="token">The cancellation token to check for cancellation requests.</param>
        /// <returns>Returns a ThreadRangeInfo describing which pages to load for the tally.</returns>
        Task<ThreadRangeInfo> GetQuestRangeInfo(IQuest quest, IPageProvider pageProvider, CancellationToken token);

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <param name="quest">The quest being tallied, which may have options that we need to consider.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        IEnumerable<Post> GetPosts(HtmlDocument page, IQuest quest);
    }
}
