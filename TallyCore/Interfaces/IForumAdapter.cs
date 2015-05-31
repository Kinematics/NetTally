using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    /// <summary>
    /// Interface for classes that can be used to extract data from forum threads in
    /// a site-specific manner.
    /// </summary>
    public interface IForumAdapter
    {
        #region Functions for constructing URLs
        int DefaultPostsPerPage { get; }

        string GetPageUrl(string threadName, int page);

        string GetPostUrlFromId(string threadName, string postId);
        #endregion

        /// <summary>
        /// Get the starting post number of the quest.  Use special options such as
        /// searching threadmarks if the quest options allow for it.
        /// </summary>
        /// <param name="pageProvider">The currently active page provider.</param>
        /// <param name="quest">The quest being checked.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns the first post number to consider when tallying posts in the thread.</returns>
        Task<int> GetStartingPostNumber(IPageProvider pageProvider, IQuest quest, CancellationToken token);

        /// <summary>
        /// Calculate the page number that corresponds to the post number given.
        /// </summary>
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        int GetPageNumberFromPostNumber(IQuest quest, int postNumber);

        /// <summary>
        /// Get the last page number of the thread, based on info available
        /// from the given page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the last page number of the thread.</returns>
        int GetLastPageNumberOfThread(HtmlDocument page);

        /// <summary>
        /// Get the author of the thread.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the thread author.</returns>
        string GetAuthorOfThread(HtmlDocument page);

        /// <summary>
        /// Given a page, return a list of posts on the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns an enumerable list of posts.</returns>
        IEnumerable<HtmlNode> GetPostsFromPage(HtmlDocument page);

        /// <summary>
        /// Get the ID string of the provided post (the portion that can be used in a URL).
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the portion of the ID that can be inserted into a URL
        /// to reach this post.</returns>
        string GetIdOfPost(HtmlNode post);

        /// <summary>
        /// This gets the sequential post number of a given post message.
        /// </summary>
        /// <param name="post">The post to inspect.</param>
        /// <returns>Returns the post number that's found.</returns>
        int GetPostNumberOfPost(HtmlNode post);
        
        /// <summary>
        /// Gets the author of the post.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the author of the post.</returns>
        string GetAuthorOfPost(HtmlNode post);

        /// <summary>
        /// Get the collated text of the post, that can be used in other parts of the program.
        /// This includes BBCode formatting, where appropriate.
        /// </summary>
        /// <param name="post">The post or post content to query.</param>
        /// <returns>Returns the contents of the post as a formatted string.</returns>
        string GetTextOfPost(HtmlNode post);

        /// <summary>
        /// Get the title of an HTML page.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        string GetPageTitle(HtmlDocument page);

    }
}
