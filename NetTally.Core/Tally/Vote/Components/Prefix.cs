using System.Text.RegularExpressions;

namespace NetTally.Tally.Vote.Components;

/// <summary>
/// Data type to store vote indentation information.
/// </summary>
/// <param name="Indent">The indent string.</param>
public sealed record Prefix(string Indent)
{
    public int Depth => Indent.Length;
}

/// <summary>
/// Extension class containing factory methods for creating <see cref="Prefix"/> objects.
/// </summary>
public static partial class PrefixCreation
{
    extension(Prefix)
    {
        /// <summary>
        /// The default, empty, prefix.
        /// </summary>
        public static Prefix Empty => _empty;

        /// <summary>
        /// Create a new prefix based on the provided text.
        /// </summary>
        /// <param name="indentString">A string with dashes indicating a vote indent.</param>
        /// <returns>A <see cref="Prefix"/> containing a normalized indent string.</returns>
        public static Prefix Create(string indentString)
        {
            if (string.IsNullOrWhiteSpace(indentString))
                return Prefix.Empty;

            // If it's already well-formatted, just use that.
            if (indentString.All(c => c == '-'))
                return new Prefix(indentString);

            // Otherwise use the regex to filter out spaces, and include
            // alternate dashes, such as em dash or en dash.
            int depth = IndentCharsRegex.Count(indentString);

            return new Prefix(new('-', depth));
        }

        /// <summary>
        /// Create a new <see cref="Prefix"/> based on the specified indentation depth.
        /// </summary>
        /// <param name="depth">A value indicating how many -'s to use. (minimum 0)</param>
        /// <returns>A <see cref="Prefix"/> containing a normalized indent string.</returns>
        public static Prefix Create(int depth)
        {
            if (depth < 1)
                return Prefix.Empty;

            return new Prefix(new('-', depth));
        }
    }

    private static readonly Prefix _empty = new("");

    [GeneratedRegex("[-–—]")]
    private static partial Regex IndentCharsRegex { get; }
}

/// <summary>
/// Extension class containing methods to manpulate a <see cref="Prefix"/>.
/// </summary>
public static class PrefixManipulation
{
    extension(Prefix prefix)
    {
        public Prefix Promote(int levels = 1)
        {
            if (levels < 1)
                return prefix;

            int newDepth = prefix.Depth - levels;

            return Prefix.Create(newDepth);
        }
    }
}
