using System;
using System.Collections.Generic;
using System.Text;
using NetTally.Forums;

namespace NetTally.Votes.Experiment2
{
    class Post
    {
        public UserIdent Author { get; }
        public PostID ID { get; }
        public int Number { get; }
        public Uri Uri { get; }
        public MessageVoteContent VoteContent { get; }

        public Post(string author, string postID, int number, string message, string url)
            :this(author, postID, number, message, new Uri(url))
        {
        }

        public Post(string author, string postID, int number, string message, Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri), $"No valid URI provided for post with author: {author}, ID: {postID}, and post number {number}.");
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number), $"Invalid post number: {number} for post with Author '{author}', ID '{postID}', and URI: {uri.ToString()}");

            Number = number;
            Uri = uri;

            try
            {
                Author = new UserIdent(author);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid post author: {author ?? "<null>"} for post with ID '{postID}', number '{number}', and URI: {uri.ToString()}", nameof(author), e);
            }

            try
            {
                ID = new PostID(postID);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid post ID: {postID ?? "<null>"} for post with Author '{author}', number '{number}', and URI: {uri.ToString()}", nameof(postID), e);
            }

            try
            {
                VoteContent = new MessageVoteContent(message);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid message vote content for post by Author '{author}', with ID '{postID}', number '{number}', and URI: {uri.ToString()}", nameof(message), e);
            }

        }

        /// <summary>
        /// Checks whether the current post is 'after' the starting point that
        /// is documented in the start info object.
        /// </summary>
        /// <param name="startInfo">The information about where the tally starts.</param>
        /// <returns>Returns true if this post comes after the defined starting point.</returns>
        public bool IsAfterStart(ThreadRangeInfo startInfo)
        {
            if (startInfo == null)
                throw new ArgumentNullException(nameof(startInfo));

            if (startInfo.ByNumber)
            {
                return Number >= startInfo.Number;
            }

            return ID.Value > startInfo.ID;
        }

    }
}
