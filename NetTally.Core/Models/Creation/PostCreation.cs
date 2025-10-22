using NetTally.Tally.Parsing;
using NetTally.Tally.Processing;

namespace NetTally.Models;

/// <summary>
/// Class for creating <see cref="Post"/> objects.
/// </summary>
public static class PostCreation
{
    extension(Post)
    {
        /// <summary>
        /// Create a <see cref="Post"/> object for a provided origin
        /// and text content.
        /// </summary>
        /// <param name="origin">The origination of the post. Result is null if this is null.</param>
        /// <param name="text">The contents of the post. Result is null if this is null or empty.</param>
        /// <returns>A <see cref="Post"/> containing the post information.</returns>
        public static Post? Create(Origin? origin, string text)
        {
            if (origin is null || string.IsNullOrEmpty(text))
                return null;

            var voteLines = VoteParser.ExtractVoteLines(text);

            return new Post(origin, text, [.. voteLines]);
        }

        /// <summary>
        /// Create a <see cref="PostToProcess"/> object which encapsulates
        /// a <see cref="Post"/>.
        /// </summary>
        /// <param name="origin">The post's origin.</param>
        /// <param name="text">The text contents of the post.</param>
        /// <returns>A <see cref="PostToProcess"/>. Returns <c>null</c> if no post could be created.</returns>
        public static PostToProcess? CreateToProcess(Origin origin, string text)
        {
            var post = Create(origin, text);

            if (post is null || post == Post.None)
                return null;

            return new PostToProcess(post);
        }
    }
}
