using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        string GetThreadsUrl(string questTitle);

        string GetThreadPageBaseUrl(string questTitle);

        string GetThreadmarksPageUrl(string questTitle);

        string GetPageUrl(string questTitle, int page);

        string GetPostUrlFromId(string postId);

        string GetUrlFromRelativeAddress(string relative);

        /// <summary>
        /// Check if the name of the thread is valid for inserting into a URL.
        /// </summary>
        /// <param name="name">The name of the quest/thread.</param>
        /// <returns>Returns true if the name is valid.</returns>
        bool IsValidThreadName(string name);
        #endregion

        #region Functions for extracting data from forum pages
        /// <summary>
        /// Get the HTML node of the page that represents the top-level element
        /// that contains all the primary content.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>The element containing the main page content.</returns>
        HtmlNode GetPageContent(HtmlDocument page);

        /// <summary>
        /// Get the author of the thread.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the thread author.</returns>
        string GetAuthorOfThread(HtmlDocument page);

        /// <summary>
        /// Get the last page number of the thread, based on info available
        /// from the given page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the last page number of the thread.</returns>
        int GetLastPageNumberOfThread(HtmlDocument page);

        /// <summary>
        /// Get the node element containing all the posts on the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns the page element that contains the posts.</returns>
        HtmlNode GetPostListFromPage(HtmlDocument page);

        /// <summary>
        /// Given the page node containing all of the posts on the thread,
        /// get an IEnumerable list of all posts on the page.
        /// </summary>
        /// <param name="postList">Element containing posts from the page.</param>
        /// <returns>Returns a list of posts.</returns>
        IEnumerable<HtmlNode> GetPostsFromList(HtmlNode postList);

        /// <summary>
        /// Given a page, return a list of posts on the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns an enumerable list of posts.</returns>
        IEnumerable<HtmlNode> GetPostsFromPage(HtmlDocument page);

        /// <summary>
        /// Get a specific post from the page, when provided with the unique
        /// ID of the post.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <param name="id">The ID of the post.</param>
        /// <returns>Returns the post.</returns>
        HtmlNode GetPostFromPageById(HtmlDocument page, string id);
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

        #region Functions for extracting data from other pages
        /// <summary>
        /// Given a page (presumably the threadmarks page), attempt to
        /// get the list node that holds all the threadmarks.
        /// </summary>
        /// <param name="page">The page to check.</param>
        /// <returns>Returns the element containing the threadmarks,
        /// or null if it's not a valid page.</returns>
        HtmlNode GetThreadmarksListFromPage(HtmlDocument page);

        /// <summary>
        /// Given a list of threadmarks, return an IEnumerable of the threadmark
        /// entries.
        /// </summary>
        /// <param name="list">The list of potential threadmarks.</param>
        /// <returns>Returns an enumerable list of the threadmarks, or
        /// null if it fails.</returns>
        IEnumerable<HtmlNode> GetThreadmarksFromThreadmarksList(HtmlNode list);

        /// <summary>
        /// Given a page (presumably the threadmarks page), attempt to
        /// get an enumeration list of all the threadmarks from the page.
        /// </summary>
        /// <param name="page">The page to search.</param>
        /// <returns>Returns an enumerable list of threadmarks, or
        /// null if it fails.</returns>
        IEnumerable<HtmlNode> GetThreadmarksFromPage(HtmlDocument page);

        /// <summary>
        /// Given a threadmark entry from a list of threadmarks, get
        /// the URL of the post the threadmark is for.
        /// </summary>
        /// <param name="threadmarkEntry">A threadmark list entry.</param>
        /// <returns>Returns the full URL to the post.</returns>
        string GetUrlOfThreadmark(HtmlNode threadmarkEntry);

        /// <summary>
        /// Given a url for a post, using its unique ID, extract the ID.
        /// </summary>
        /// <param name="url">A url with a post's unique ID in it.</param>
        /// <returns>Returns the post's ID.</returns>
        string GetPostIdFromUrl(string url);
        #endregion
    }
}
