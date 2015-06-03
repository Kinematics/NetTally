using System;
using System.Text.RegularExpressions;

namespace NetTally
{
    /// <summary>
    /// Enum for separating vote categories
    /// </summary>
    public enum VoteType
    {
        Vote,
        Plan,
        Rank
    }

    public static class Utility
    {
        // Regex for control and formatting characters that we don't want to allow processing of.
        // EG: \u200B, non-breaking space
        // Do not remove CR/LF characters
        public static Regex UnsafeCharsRegex { get; } = new Regex(@"[\p{C}-[\r\n]]");

        /// <summary>
        /// Filter unsafe characters from the provided string.
        /// </summary>
        /// <param name="input">The string to filter.</param>
        /// <returns>The input string with all unicode control characters (except cr/lf) removed.</returns>
        public static string SafeString(string input)
        {
            return UnsafeCharsRegex.Replace(input, "");
        }
    }

    public static class VoteLine
    {
        // Regex to get the different parts of the vote.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>-*)\[(?<marker>[xX+✓✔1-9])\]\s*(\[\s*(?<task>(\w|\d)(\s*(\w|\d)+)*\??)\s*\])?(?<content>.*)");
        // Regex to match any markup that we'll want to remove during comparisons.
        static readonly Regex markupRegex = new Regex(@"\[/?[ibu]\]|\[color[^]]*\]|\[/color\]");
        // Regex to allow us to collapse a vote to a commonly comparable version.
        static readonly Regex collapseRegex = new Regex(@"\s|\.");
        // Regex to allow us to strip leading dashes from a per-line vote.
        static readonly Regex leadHyphenRegex = new Regex(@"^-+");

        /// <summary>
        /// Given a vote line, remove any BBCode formatting chunks, and trim the result.
        /// </summary>
        /// <param name="voteLine">The vote line to examine.</param>
        /// <returns>Returns the vote line without any BBCode formatting.</returns>
        public static string CleanVote(string voteLine)
        {
            // Need to trim the result because removing markup may reveal new whitespace.
            return markupRegex.Replace(voteLine, "").Trim();
        }

        /// <summary>
        /// Collapse a vote to a minimized form, for comparison.
        /// All BBCode markup is removed, along with all spaces and periods,
        /// and leading dashes when partitioning by line.  The text is then
        /// lowercased.
        /// </summary>
        /// <param name="voteLine">Original vote line to minimize.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns a minimized version of the vote string.</returns>
        public static string MinimizeVote(string voteLine, IQuest quest)
        {
            string cleaned = CleanVote(voteLine);
            cleaned = collapseRegex.Replace(cleaned, "");
            cleaned = cleaned.ToLower();
            if (quest.UseVotePartitions && quest.PartitionByLine)
                cleaned = leadHyphenRegex.Replace(cleaned, "");

            return cleaned;
        }

        /// <summary>
        /// Get the marker of the vote line.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the marker of the vote line.</returns>
        public static string GetVotePrefix(string voteLine)
        {
            string cleaned = CleanVote(voteLine);
            Match m = voteLineRegex.Match(cleaned);
            if (m.Success)
            {
                return m.Groups["prefix"].Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the marker of the vote line.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the marker of the vote line.</returns>
        public static string GetVoteMarker(string voteLine)
        {
            string cleaned = CleanVote(voteLine);
            Match m = voteLineRegex.Match(cleaned);
            if (m.Success)
            {
                return m.Groups["marker"].Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the (optional) task name from the provided vote line.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the name of the task, if found, or an empty string.</returns>
        public static string GetVoteTask(string voteLine)
        {
            string cleaned = CleanVote(voteLine);
            Match m = voteLineRegex.Match(cleaned);
            if (m.Success)
            {
                string task = m.Groups["task"].Value;

                // A task name is composed of any number of characters or digits, with an optional ending question mark.
                // The returned value will capitalize the first letter, and lowercase any following letters.

                if (task.Length == 1)
                    return task.ToUpper();

                if (task.Length > 1)
                    return char.ToUpper(task[0]) + task.Substring(1).ToLower();
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the content of the vote line.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the content of the vote line.</returns>
        public static string GetVoteContent(string voteLine)
        {
            string cleaned = CleanVote(voteLine);
            Match m = voteLineRegex.Match(cleaned);
            if (m.Success)
            {
                return m.Groups["content"].Value.Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the content of the vote line.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the content of the vote line.</returns>
        public static string GetVotePlanName(string voteLine)
        {
            string content = GetVoteContent(voteLine);

            if (content.Length > 5 && content.StartsWith("plan ", StringComparison.OrdinalIgnoreCase))
                content = content.Substring(5);
            if (content.EndsWith("."))
                content = content.Substring(0, content.Length - 2);

            return content.Trim();
        }

        /// <summary>
        /// Get whether the vote line is a ranked vote line (ie: marker uses digits 1-9).
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns true if the vote marker is a digit.</returns>
        public static bool IsRankedVote(string voteLine)
        {
            return Char.IsDigit(VoteLine.GetVoteMarker(voteLine), 0);
             //&& voteLine.StartsWith("-") == false;
        }
    }
}
