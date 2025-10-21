using System.Globalization;
using NetTally.Models.Creation;
using NetTally.Models.Defaults;
using NetTally.Models.Posts;

namespace NetTally.Models.Creation;

/// <summary>
/// Class for creating <see cref="PostNumber"/> objects.
/// </summary>
public static class PostNumberCreation
{
    extension(PostNumber)
    {
        /// <summary>
        /// Create a <see cref="PostNumber"/> using a numeric ID value.
        /// </summary>
        /// <param name="id">The post number.</param>
        /// <returns>A <see cref="PostNumber"/> if a positive value was provided. Otherwise returns <see cref="Zero"/></returns>
        public static PostNumber? Create(long id)
        {
            if (id < 1)
                return null;

            return new PostNumber(id);
        }

        /// <summary>
        /// Create a <see cref="PostNumber"/> using a string of the number.
        /// </summary>
        /// <param name="id">The string holding the number value.</param>
        /// <returns>A <see cref="PostNumber"/> if a positive numeric value was provided.
        /// If the provided value was numeric, but not positive, returns <see cref="Zero"/>
        /// If no numeric value could be extracted, returns <c>null</c>.</returns>
        public static PostNumber? Create(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (long.TryParse(id, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long idValue))
            {
                return idValue switch
                {
                    > 0 => new PostNumber(idValue),
                    _ => PostNumber.Zero
                };
            }

            return null;
        }
    }
}
