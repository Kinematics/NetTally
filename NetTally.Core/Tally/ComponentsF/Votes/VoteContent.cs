using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NetTally.Utility;
using NetTally.Utility.Comparers;
using NetTally.Votes;

namespace NetTally.Tally.ComponentsF.Votes;
/// <summary>
/// Data type for vote content.
/// </summary>
/// <param name="Content">The full content of a vote line.</param>
/// <param name="CleanContent">The content of a vote line with BBCode removed.</param>
public record VoteContentType(string Content, string CleanContent);

/// <summary>
/// Static class for creating <see cref="VoteContentType"/> objects.
/// </summary>
public static partial class VoteContent
{
    public static VoteContentType Empty { get; } = new("", "");

    public static VoteContentType? Create(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        content = content.RemoveUnsafeCharacters().Trim();

        string cleanContent = VoteLineParser.StripBBCode(content);

        return new VoteContentType(content, cleanContent);
    }

    public static VoteContentType Trim(VoteContentType content)
    {
        int trimIndex = GetTrimIndexForContent(content);

        return trimIndex == 0 ?
            content :
            content with { CleanContent = content.CleanContent[..trimIndex] };
    }


    static readonly Regex extendedTextRegex = ExtendedTextRegex();
    static readonly Regex extendedTextSentenceRegex = ExtendedTextSentenceRegex();
    static readonly Regex wordCountRegex = WordCountRegex();

    [GeneratedRegex(@"(?<!\([^)]*)(((?<![pP][lL][aA][nN]\s*):(?!//))|—|(-(-+|\s+|\s*[^\p{Ll}])))")]
    private static partial Regex ExtendedTextRegex();
    [GeneratedRegex(@"(?<!\([^)]*)(?<![pP][lL][aA][nN]\b.+)(((?<=\S{4,})|(?<=\s[\p{Ll}]\S+))([.?!])(?:\s+[^\p{Ll}]))")]
    private static partial Regex ExtendedTextSentenceRegex();
    [GeneratedRegex(@"\S+\b")]
    private static partial Regex WordCountRegex();

    /// <summary>
    /// Gets the index to trim from for a given content line.
    /// Determines the trim point as the last valid separation
    /// character that fits under the length limit.  If there are
    /// multiple separation points on the line, the untrimmed portion
    /// of the line must have more than one word in it.
    /// </summary>
    /// <param name="content">Content of the vote line.</param>
    /// <returns>Returns the index that marks where to remove further text,
    /// or 0 if no cutoff point is found.</returns>
    private static int GetTrimIndexForContent(VoteContentType content)
    {
        // If content is less than about 8 words long, don't try to trim it.
        if (content.CleanContent.Length < 50)
            return 0;

        // The furthest into the content area that we're going to allow a
        // separator to be placed is 30% into the line length.
        int separatorLimit = content.CleanContent.Length * 3 / 10;

        // Colons are always allowed as separators, though it needs
        // to run through a regex to be sure it's not part of a plan
        // definition line, or part of an absolute path on Windows.

        // Em dashes are always allowed as separators.

        // Search for any instances of hyphens in the content.
        // Only counts if there's a space after the hyphen, or if
        // the next word starts with a capital letter.

        // Select the one that comes closest to, without passing,
        // the separator limit.


        MatchCollection matches = extendedTextRegex.Matches(content.CleanContent);

        // If there is only one separator, use it as long as it's within the limit.
        if (matches.Count == 1)
        {
            Match m = matches[0];
            if (m.Success && m.Index > 0 && m.Index < separatorLimit)
            {
                return m.Index;
            }
        }
        // If there's more than one separator, take the last one that fits, but
        // only if there's more than one word before it.
        else if (matches.Count > 1)
        {
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match m = matches[i];
                if (m.Success && m.Index > 0 && m.Index < separatorLimit)
                {
                    string partial = content.CleanContent[..m.Index];

                    if (CountWords(partial) > 1)
                        return m.Index;
                }
            }
        }

        // Alternate trimming that reduces the vote to only the first sentence.
        matches = extendedTextSentenceRegex.Matches(content.CleanContent);
        // Sentences may be taken up to half the line length.
        separatorLimit = content.CleanContent.Length / 2;

        if (matches.Count > 0)
        {
            Match m = matches[0];
            if (m.Success && m.Index > 0 && m.Index < separatorLimit)
            {
                return m.Index + 1;
            }
        }

        // If no proper matches were found, return 0.
        return 0;
    }

    /// <summary>
    /// Counts the words in the provided string.
    /// </summary>
    /// <param name="partial">Part of a content line that we're going to count the words of.</param>
    /// <returns>Returns the number of words found in the provided string.</returns>
    private static int CountWords(string partial)
    {
        var matches = wordCountRegex.Matches(partial);
        return matches.Count;
    }
}

/// <summary>
/// Comparer class for <see cref="VoteContentType"/> objects.
/// </summary>
public class VoteContentComparer : IEqualityComparer<VoteContentType>, IComparer<VoteContentType>
{
    public static VoteContentComparer Instance { get; } = new();

    public int Compare(VoteContentType? x, VoteContentType? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Agnostic.CurrentStringComparer.Compare(x.CleanContent, y.CleanContent);
    }

    public bool Equals(VoteContentType? x, VoteContentType? y)
    {
        return Compare(x, y) == 0;
    }

    public int GetHashCode([DisallowNull] VoteContentType obj)
    {
        return Agnostic.InsensitiveComparer.GetHashCode(obj.CleanContent);
    }
}
