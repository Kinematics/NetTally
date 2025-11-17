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

            return new Post(origin, text);
        }
    }
}
