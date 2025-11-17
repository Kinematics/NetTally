using System.Globalization;

namespace NetTally.Models;

/// <summary>
/// Class for creating <see cref="PostNumber"/> objects.
/// </summary>
public static class PostNumberCreation
{
    extension(PostNumber)
    {
        /// <summary>
        /// Create a <see cref="PostNumber"/> using a numeric value.
        /// </summary>
        /// <param name="num">The post number.</param>
        /// <returns>A <see cref="PostNumber"/> if a positive value was provided. Otherwise returns <see cref="Zero"/></returns>
        public static PostNumber? Create(long num)
        {
            if (num < 1)
                return null;

            return new PostNumber(num);
        }

        /// <summary>
        /// Create a <see cref="PostNumber"/> using a string of the number.
        /// </summary>
        /// <param name="num">The string holding the number value.</param>
        /// <returns>A <see cref="PostNumber"/> if a positive numeric value was provided.
        /// If no valid numeric value could be extracted, returns <c>null</c>.</returns>
        public static PostNumber? Create(string num)
        {
            if (string.IsNullOrEmpty(num))
                return null;

            if (long.TryParse(num, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out long numValue))
            {
                if (numValue > 0)
                    return new PostNumber(numValue);
            }

            return null;
        }
    }
}
