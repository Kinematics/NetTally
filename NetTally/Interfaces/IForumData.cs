using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IForumData
    {

        #region Functions for constructing URLs
        string GetThreadsUrl(string questTitle);

        string GetThreadPageBaseUrl(string questTitle);

        string GetThreadmarksPageUrl(string questTitle);

        string GetPageUrl(string questTitle, int page);

        string GetPostUrlFromId(string postId);

        string GetUrlFromRelativeAddress(string relative);
        #endregion

        #region Functions for extracting data from forum pages
        HtmlNode GetPageContent(HtmlDocument page);

        string GetAuthorOfThread(HtmlDocument page);


        HtmlNode GetPostListFromPage(HtmlDocument page);

        IEnumerable<HtmlNode> GetPostsFromList(HtmlNode postList);

        IEnumerable<HtmlNode> GetPostsFromPage(HtmlDocument page);

        #endregion

        #region Functions for extracting data from posts
        bool IsPost(HtmlNode post);

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
        /// Calculate the page number that corresponds to the post number given.
        /// </summary>
        /// <param name="post">Post number.</param>
        /// <returns>Page number.</returns>
        int GetPageNumberFromPostNumber(int postNumber);

        /// <summary>
        /// Gets the author of the post.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the author of the post.</returns>
        string GetAuthorOfPost(HtmlNode post);

        /// <summary>
        /// Get the threadmarker node of the post, if any.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the threadmarker node of the post, or null if none is found.</returns>
        HtmlNode GetThreadmarkerOfPost(HtmlNode post);

        /// <summary>
        /// Gets the inner HTML node containing the actual contents of the post.
        /// </summary>
        /// <param name="post">The post to query.</param>
        /// <returns>Returns the content node within the post.</returns>
        HtmlNode GetContentsOfPost(HtmlNode post);

        /// <summary>
        /// Get the collated text of the post, that can be used in other parts of the program.
        /// This includes BBCode formatting, where appropriate.
        /// </summary>
        /// <param name="post">The post or post content to query.</param>
        /// <returns>Returns the contents of the post as a formatted string.</returns>
        string GetTextOfPost(HtmlNode post);
        #endregion
    }
}
