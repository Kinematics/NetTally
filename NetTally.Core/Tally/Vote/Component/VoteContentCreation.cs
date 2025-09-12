using NetTally.Tally.Components.Votes;
using NetTally.Utility;

namespace NetTally.Tally.Vote.Component;

/// <summary>
/// Extension class for creating <see cref="VoteContent"/> objects.
/// </summary>
public static class VoteContentCreation
{
    extension(VoteContent)
    {
        /// <summary>
        /// Default, empty content.
        /// </summary>
        public static VoteContent Empty => _empty;

        /// <summary>
        /// Create a new <see cref="VoteContent"/> object containing the provided text.
        /// </summary>
        /// <param name="content">The text content of the vote.</param>
        /// <returns>A <see cref="VoteContent"/></returns>
        public static VoteContent Create(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return VoteContent.Empty;
            }

            content = content.RemoveUnsafeCharacters().Trim();

            string cleanContent = VoteLineParser.StripBBCode(content);

            return new VoteContent(content, cleanContent);
        }
    }

    private static readonly VoteContent _empty = new("", "");
}
