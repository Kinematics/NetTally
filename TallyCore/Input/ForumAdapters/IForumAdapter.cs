using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally.Adapters
{
    public interface IForumAdapter
    {
        // General meta-info

        Uri Site { get; set; }

        /// <summary>
        /// Get the default number of posts per page for this forum.
        /// </summary>
        int DefaultPostsPerPage { get; }

        // How to format links

        /// <summary>
        /// Generate a URL to access the specified page of the adapter's thread.
        /// </summary>
        /// <param name="page">The page of the thread that is being loaded.</param>
        /// <returns>Returns a URL formatted to load the requested page of the thread.</returns>
        string GetUrlForPage(int page, int postsPerPage);

        /// <summary>
        /// Generate a URL to access the specified post of the adapter's thread.
        /// </summary>
        /// <param name="postId">The permalink ID of the post being requested.</param>
        /// <returns>Returns a URL formatted to load the requested post.</returns>
        string GetPermalinkForId(string postId);

        // Special query to allow getting the starting post based on threadmarks, etc.

        /// <summary>
        /// Determine the starting post number of the thread, based on the provided information.
        /// Runs asynchronously.
        /// </summary>
        /// <param name="quest">The quest being loaded.</param>
        /// <param name="pageProvider">A page provider for loading web pages.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>Returns the number of the post where tallying should begin.</returns>
        Task<ThreadRangeInfo> GetStartingPostNumberAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token);

        // Extract information from a downloaded page

        /// <summary>
        /// Get thread info from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns thread information that can be gleaned from that page.</returns>
        ThreadInfo GetThreadInfo(HtmlDocument page);

        /// <summary>
        /// Get a list of posts from the provided page.
        /// </summary>
        /// <param name="page">A web page from a forum that this adapter can handle.</param>
        /// <returns>Returns a list of constructed posts from this page.</returns>
        IEnumerable<PostComponents> GetPosts(HtmlDocument page);


        /// <summary>
        /// String to use for a line break between tasks.
        /// </summary>
        string LineBreak { get; }
    }
}
