using HtmlAgilityPack;
using NetTally.Enums;
using NetTally.Models;
using NetTally.Web;

namespace NetTally.Input.Forums.ForumAdapters;

public interface IForumAdapter
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
    BoolEx HasRssThreadmarksFeed(Uri uri);

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
    string GetUrlForPage(Quest quest, int page);

    /// <summary>
    /// Get a list of posts from the provided page.
    /// </summary>
    /// <param name="page">A web page from a forum that this adapter can handle.</param>
    /// <param name="quest">The quest being tallied, which may have options that we need to consider.</param>
    /// <returns>Returns a list of constructed posts from this page.</returns>
    IEnumerable<Post> GetPosts(HtmlDocument page, Quest quest, int pageNumber);

    /// <summary>
    /// Get information about the thread.
    /// This includes title, author, and starting range.
    /// </summary>
    /// <param name="quest">The quest being queried.</param>
    /// <param name="pageProvider">A page provider for loading pages.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns><see cref="ThreadInfo"/> containing thread information.</returns>
    Task<ThreadInfo?> GetThreadInfoAsync(
        Quest quest,
        IPageProvider pageProvider,
        CancellationToken token);
}
