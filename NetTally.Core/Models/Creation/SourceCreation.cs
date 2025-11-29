namespace NetTally.Models;

/// <summary>
/// Provides extension methods for creating a <see cref="Source"/> instance from thread, permalink, post ID, and post
/// number information.
/// </summary>
/// <remarks>This class contains static methods intended to simplify the construction of <see cref="Source"/>
/// objects from common identifiers. All methods are static and can be accessed without instantiating the
/// class.</remarks>
public static class SourceCreation
{
    extension(Source)
    {
        /// <summary>
        /// Creates a new instance of the source location using the specified thread, permalink, post ID, and post
        /// number.
        /// </summary>
        /// <param name="thread">The URI of the thread associated with the source location. Cannot be null.</param>
        /// <param name="permalink">The URI representing the permalink to the post. Cannot be null.</param>
        /// <param name="postId">The unique identifier of the post. Cannot be null.</param>
        /// <param name="postNumber">The number of the post within the thread. Cannot be null.</param>
        /// <returns>A new <see cref="SourceLocation"/> instance initialized with the provided parameters, or <see langword="null"/> if any parameter is null.</returns>
        public static Source? Create(
            Uri? thread,
            Uri? permalink,
            PostId? postId,
            PostNumber? postNumber)
        {
            if (thread is null ||
                permalink is null ||
                postId is null ||
                postNumber is null)
                return null;

            return new SourceLocation(thread, permalink, postId, postNumber);
        }
    }
}

