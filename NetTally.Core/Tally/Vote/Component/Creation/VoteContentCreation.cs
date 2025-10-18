using NetTally.Tally.Parsing;
using NetTally.Tally.Vote.Component;
using NetTally.Tally.Vote.Component.Creation;
using NetTally.Utility;

namespace NetTally.Tally.Vote.Component.Creation;

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
                return VoteContent.Empty;
            }

            content = content.RemoveUnsafeCharacters().Trim();

            string cleanContent = VoteLineParser.StripBBCode(content);

            cleanContent = cleanContent.Trim();

            return new VoteContent(content, cleanContent);
        }
    }
}
