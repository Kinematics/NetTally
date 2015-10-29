using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace NetTally
{
    /// <summary>
    /// Class for converting web pages into posts that can be analyzed, independent of the HTML.
    /// </summary>
    public static class PostExtractor
    {
        /// <summary>
        /// Get all the posts present on the provided pages.
        /// </summary>
        /// <param name="pages">The pages that were read from the forum thread.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns a list of the posts extracted from the pages.</returns>
        public static List<PostComponents> GetPosts(List<HtmlDocument> pages, IQuest quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (pages == null)
                throw new ArgumentNullException(nameof(pages));

            IForumAdapter forumAdapter = quest.GetForumAdapter();

            if (forumAdapter == null)
                throw new ApplicationException($"Unable to get forum adapter for thread {quest.ThreadName}");

            var posts = from page in pages
                        where page != null
                        from post in forumAdapter.GetPostsFromPage(page)
                        where post != null
                        select GetPostComponents(post, forumAdapter);

            return posts.ToList();
        }

        /// <summary>
        /// Converts an HTML post into a post object for further processing.
        /// </summary>
        /// <param name="post">The post extracted from a forum web page.</param>
        /// <param name="forumAdapter">The forum adapter to be used to extract data from the post.</param>
        /// <returns>Returns the PostComponents object for the post.</returns>
        private static PostComponents GetPostComponents(HtmlNode post, IForumAdapter forumAdapter)
        {
            string postAuthor = forumAdapter.GetAuthorOfPost(post);
            string postID = forumAdapter.GetIdOfPost(post);
            string postText = forumAdapter.GetTextOfPost(post);
            int postNumber = forumAdapter.GetPostNumberOfPost(post);

            return new PostComponents(postAuthor, postID, postText, postNumber);
        }
    }
}
