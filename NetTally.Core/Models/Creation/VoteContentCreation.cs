using NetTally.Models.Defaults;
using NetTally.Models.Votes;
using NetTally.Tally.Parsing;
using NetTally.Utility;

namespace NetTally.Models.Creation;

/// <summary>
/// Extension class for creating <see cref="VoteContent"/> objects.
/// </summary>
public static class VoteContentCreation
{
    extension(VoteContent)
    {
        /// <summary>
        /// Create a new <see cref="VoteContent"/> object containing the provided text.
        /// </summary>
        /// <param name="content">The text content of the vote.</param>
        /// <returns>A <see cref="VoteContent"/></returns>
        public static VoteContent Create(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return VoteContent.None;
            }

            content = content.RemoveUnsafeCharacters().Trim();

            string cleanContent = VoteLineParser.StripBBCode(content);

            cleanContent = cleanContent.Trim();

            return new VoteContent(content, cleanContent);
        }
    }
}
