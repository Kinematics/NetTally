using System;
using System.Collections.Generic;
using System.Text;

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
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number), $"Invalid post number: {number} for post with Author '{author}', ID '{postID}', and URL: {url}");

            Number = number;

            try
            {
                Author = new UserIdent(author);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid post author: {author ?? "<null>"} for post with ID '{postID}', number '{number}', and url: {url}", nameof(author), e);
            }

            try
            {
                ID = new PostID(postID);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid post ID: {postID ?? "<null>"} for post with Author '{author}', number '{number}', and url: {url}", nameof(postID), e);
            }

            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(number), $"Invalid post number: {number}");
            }

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                throw new ArgumentException($"Badly formed identity URL for post ID {postID}: {url}");

            Uri = new Uri(url);

            try
            {
                VoteContent = new MessageVoteContent(message);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid message vote content for post by Author '{author}', with ID '{postID}', number '{number}', and url: {url}", nameof(message), e);
            }

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
    }
}
