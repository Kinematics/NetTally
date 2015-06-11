using System;
using System.Text.RegularExpressions;

namespace NetTally
{
    public static class VoteLine
    {
        // Regex to get the different parts of the vote.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>-*)\s*\[\s*(?<marker>[xX+✓✔1-9])\s*\]\s*(\[\s*(?<task>(\w|\d)(\s*(\w|\d)+)*\??)\s*\])?(?<content>.*)");
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
        /// Get the name of a vote plan from the contents of a vote line.
        /// Standard version will mark the content with the plan name marker character
        /// if the content starts with the word "plan", but won't add the character
        /// if it doesn't.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns a possible plan name from the vote line.</returns>
        public static string GetVotePlanName(string voteLine)
        {
            string content = GetVoteContent(voteLine);

            if (content.Length > 5 && content.StartsWith("plan ", StringComparison.OrdinalIgnoreCase))
            {
                content = content.Substring(5);
                content = Utility.Text.PlanNameMarker + content;
            }

            if (content.EndsWith("."))
                content = content.Substring(0, content.Length - 2);

            return content.Trim();
        }

        /// <summary>
        /// Get the name of a vote plan from the contents of a vote line.
        /// Alternate version will mark the content with the plan name marker character
        /// if the content does *not* start with the word "plan", but will not add the
        /// character if it does.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns a possible plan name from the vote line.</returns>
        public static string GetAltVotePlanName(string voteLine)
        {
            string content = GetVoteContent(voteLine);

            if (content.Length > 5 && content.StartsWith("plan ", StringComparison.OrdinalIgnoreCase))
            {
                content = content.Substring(5);
            }
            else
            {
                content = Utility.Text.PlanNameMarker + content;
            }

            if (content.EndsWith("."))
                content = content.Substring(0, content.Length - 2);

            return content.Trim();
        }

        /// <summary>
        /// Function to get all of the individual components of the vote line at once, rather than calling and
        /// parsing the line multiple times.
        /// </summary>
        /// <param name="voteLine">The vote line to analyze.</param>
        /// <param name="prefix">The prefix (if any) for the vote line.</param>
        /// <param name="marker">The marker for the vote line.</param>
        /// <param name="task">The task for the vote line.</param>
        /// <param name="content">The content of the vote line.</param>
        public static void GetVoteComponents(string voteLine, out string prefix, out string marker, out string task, out string content)
        {
            string cleaned = CleanVote(voteLine);
            Match m = voteLineRegex.Match(cleaned);
            if (m.Success)
            {
                prefix = m.Groups["prefix"].Value;
                marker = m.Groups["marker"].Value;
                content = m.Groups["content"].Value.Trim();

                task = m.Groups["task"].Value.Trim();

                // A task name is composed of any number of characters or digits, with an optional ending question mark.
                // The returned value will capitalize the first letter, and lowercase any following letters.

                if (task.Length == 1)
                    task = task.ToUpper();

                if (task.Length > 1)
                    task = char.ToUpper(task[0]) + task.Substring(1).ToLower();
            }
            else
            {
                throw new InvalidOperationException("Unable to parse vote line.");
            }
        }

        /// <summary>
        /// Create a vote line composed of the provided components.
        /// </summary>
        /// <param name="prefix">The prefix for the line.</param>
        /// <param name="marker">The marker for the line.</param>
        /// <param name="task">The task the line should be grouped with.</param>
        /// <param name="content">The contents of the vote.</param>
        /// <returns>Returns a complete vote line.</returns>
        public static string CreateVoteLine(string prefix, string marker, string task, string content)
        {
            if (prefix == null)
                prefix = "";

            if (marker == null || marker == string.Empty)
                marker = "X";

            if (task == null)
                task = "";

            if (content == null || content == string.Empty)
                throw new ArgumentNullException(nameof(content));

            string line;

            if (task == string.Empty)
                line = string.Format("{0}[{1}] {3}", prefix, marker, task, content);
            else
                line = string.Format("{0}[{1}][{2}] {3}", prefix, marker, task, content);

            return line;
        }

        /// <summary>
        /// Create a vote line composed of the provided components.
        /// Parameters should be null for values that won't change.
        /// </summary>
        /// <param name="prefix">The prefix for the line.</param>
        /// <param name="marker">The marker for the line.</param>
        /// <param name="task">The task the line should be grouped with.</param>
        /// <param name="content">The contents of the vote.</param>
        /// <returns>Returns a complete vote line.</returns>
        public static string ModifyVoteLine(string voteLine, string prefix, string marker, string task, string content)
        {
            if (voteLine == null || voteLine == string.Empty)
                throw new ArgumentNullException(nameof(voteLine));

            // If all parameters are null, the vote line doesn't change.
            if (prefix == null && marker == null && task == null && content == null)
                return voteLine;

            string votePrefix;
            string voteMarker;
            string voteTask;
            string voteContent;

            // Use the original vote line value for any parameter that is null.
            GetVoteComponents(voteLine, out votePrefix, out voteMarker, out voteTask, out voteContent);

            if (prefix == null)
                prefix = votePrefix;

            if (marker == null)
                marker = voteMarker;

            if (task == null)
                task = voteTask;

            if (content == null)
                content = voteContent;

            string modifiedLine;

            if (task == string.Empty)
                modifiedLine = string.Format("{0}[{1}] {3}", prefix, marker, task, content);
            else
                modifiedLine = string.Format("{0}[{1}][{2}] {3}", prefix, marker, task, content);

            return modifiedLine;
        }

        /// <summary>
        /// Get whether the vote line is a ranked vote line (ie: marker uses digits 1-9).
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns true if the vote marker is a digit, false if not.</returns>
        public static bool IsRankedVote(string voteLine)
        {
            string marker = VoteLine.GetVoteMarker(voteLine);

            if (marker == string.Empty)
                return false;

            return char.IsDigit(marker, 0);
        }
    }
}
