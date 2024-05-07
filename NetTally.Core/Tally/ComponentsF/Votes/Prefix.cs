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

    public static PrefixType Create(string indent)
    {
        if (string.IsNullOrWhiteSpace(indent))
            return Empty;

        if (indent.All(c => c == '-'))
            return new PrefixType(indent);

        int depth = IndentCharsRegex().Count(indent);

        string prefix = new('-', depth);

        return new PrefixType(prefix);
    }

    public static PrefixType Reduce(PrefixType prefix)
    {
        if (prefix.Depth > 1)
            return prefix with { Indent = prefix.Indent[1..] };

        return Empty;
    }

    [GeneratedRegex("[-–—]")]
    private static partial Regex IndentCharsRegex();
}
