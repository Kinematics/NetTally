using System.Globalization;

namespace NetTally.Models;

/// <summary>
/// Class for creating <see cref="PostId"/> objects.
/// </summary>
public static class PostIdCreation
{
    extension(PostId)
    {
        /// <summary>
        /// Create a <see cref="PostId"/> using a numeric ID value.
        /// </summary>
        /// <param name="id">The numeric ID value.</param>
        /// <returns>A <see cref="PostId"/> if a positive value was provided. Otherwise returns <see cref="Zero"/></returns>
        public static PostId? Create(long id)
        {
            if (id < 1)
                return null;

            return new PostId(id);
        }

        /// <summary>
        /// Create a <see cref="PostId"/> using a string of the ID value.
        /// </summary>
        /// <param name="id">The string ID value.</param>
        /// <returns>A <see cref="PostId"/> if a positive numeric value was provided.
        /// If no numeric value could be extracted, returns <c>null</c>.</returns>
        public static PostId? Create(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (long.TryParse(id, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long idValue))
            {
                if (idValue > 0)
                    return new PostId(idValue);
            }

            return null;
        }
    }
}
