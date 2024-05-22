using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetTally.Tally.ComponentsF.Votes;
/// <summary>
/// Data type to store vote indentation information.
/// </summary>
/// <param name="Indent">The indent string.</param>
public record PrefixType(string Indent)
{
    public int Depth => Indent.Length;
}

/// <summary>
/// Static class for creating and modifying <see cref="PrefixType"/> objects.
/// </summary>
public static partial class Prefix
{
    public static PrefixType Empty { get; } = new PrefixType("");

    /// <summary>
    /// Create a new prefix based on the provided text.
    /// </summary>
    /// <param name="indent">A string with dashes indicating a vote indent.</param>
    /// <returns>A <see cref="PrefixType"/> containing a normalized indent string.</returns>
    public static PrefixType Create(string indent)
    {
        if (string.IsNullOrWhiteSpace(indent))
            return Empty;

        // If it's already well-formatted, just use that.
        if (indent.All(c => c == '-'))
            return new PrefixType(indent);

        // Otherwise use the regex to filter out spaces, and include
        // alternate dashes, such as em dash or en dash.
        int depth = IndentCharsRegex().Count(indent);

        return CreateDepth(depth);
    }

    /// <summary>
    /// Reduce the level of indentation by a certain amount (default 1).
    /// </summary>
    /// <param name="prefix">The prefix to reduce.</param>
    /// <param name="promotionDepth">How far to reduce the prefix indentation by.
    /// Defaults to 1. Anything less than 1 is ignored.</param>
    /// <returns>A <see cref="PrefixType"/> with depth reduced by the requested amount.</returns>
    public static PrefixType Reduce(PrefixType prefix, int promotionDepth = 1)
    {
        if (promotionDepth < 1) return prefix;

        int finalDepth = Math.Max(prefix.Depth - promotionDepth, 0);
        return CreateDepth(finalDepth);
    }

    /// <summary>
    /// Creates a <see cref="PrefixType"/> of the specified depth, using
    /// standard hyphens.
    /// </summary>
    /// <param name="depth">How many hyphens to generate in the prefix.</param>
    /// <returns>A constructed <see cref="PrefixType"/></returns>
    private static PrefixType CreateDepth(int depth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        if (depth == 0) return Empty;

        string prefix = new('-', depth);
        return new PrefixType(prefix);
    }

    [GeneratedRegex("[-–—]")]
    private static partial Regex IndentCharsRegex();
}
