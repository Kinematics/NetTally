using System;
using System.Text;
using System.Text.RegularExpressions;

namespace NetTally
{
    public static class VoteString
    {
        // Regex to get the different parts of the vote. Content includes only the first line of the vote.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>-*)\s*\[\s*(?<marker>[xX+✓✔1-9])\s*\]\s*(\[\s*(?<task>(\w|\d)(\s*(\w|\d)+)*\??)\s*\])?\s*(?<content>.*)");
        // Regex to get the different parts of the votes. Content includes the entire vote.
        static readonly Regex voteFullRegex = new Regex(@"^(?<prefix>-*)\s*\[\s*(?<marker>[xX+✓✔1-9])\s*\]\s*(\[\s*(?<task>(\w|\d)(\s*(\w|\d)+)*\??)\s*\])?\s*(?<content>.*)", RegexOptions.Singleline);
        // Regex to match any markup that we'll want to remove during comparisons.
        static readonly Regex markupRegex = new Regex(@"\[/?[ibu]\]|\[color[^]]*\]|\[/color\]");
        // Regex to allow us to collapse a vote to a commonly comparable version.
        static readonly Regex collapseRegex = new Regex(@"\s|\.");
        // Regex to allow us to strip leading dashes from a per-line vote.
        static readonly Regex leadHyphenRegex = new Regex(@"^-+");
        // Regex for separating out the task from the other portions of a vote line.
        static readonly Regex taskRegex = new Regex(@"^(?<pre>.*?\[[xX+✓✔1-9]\])\s*(\[\s*(?<task>(\w|\d)(\s*(\w|\d)+)*\??)\s*\])?\s*(?<remainder>.+)", RegexOptions.Singleline);
        // Potential reference to another user's plan.
        static readonly Regex referenceNameRegex = new Regex(@"^(plan\s+)?(?<reference>.+)", RegexOptions.IgnoreCase);

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
            if (quest.PartitionMode == PartitionMode.ByLine)
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
        /// Treats the entire vote as a single line.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the content of the entire vote, after the first marker/task.</returns>
        public static string GetVoteContent(string voteLine)
        {
            string cleaned = CleanVote(voteLine);
            Match m = voteFullRegex.Match(cleaned);
            if (m.Success)
            {
                return m.Groups["content"].Value.Trim();
            }

            return string.Empty;
        }

        /// <summary>
        /// Get the content of the vote line.
        /// Treats the vote as multiple separate lines, and only returns the content of the first line.
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns the content of the first vote line.</returns>
        public static string GetVoteContentFirstLine(string voteLine)
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
        /// <param name="alt">Flag to alternate whether having the content start
        /// with the word "plan" should mark the output with the special character.
        /// The default adds the character if the content does start with "plan".
        /// Sending a value of true will add the character if the content does not start with "plan".</param>
        /// <returns>Returns a possible plan name from the vote line.</returns>
        public static string GetVoteReferenceName(string voteLine, bool alt = false)
        {
            string content = GetVoteContentFirstLine(voteLine);

            Match m = referenceNameRegex.Match(content);
            if (m.Success)
            {
                string name = m.Groups["reference"].Value;

                if (name.EndsWith("."))
                    name = name.Substring(0, name.Length - 1);

                if (alt ^ content.StartsWith("plan ", StringComparison.OrdinalIgnoreCase))
                    name = Utility.Text.PlanNameMarker + name;

                return name.Trim();
            }

            return content;
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
                line = $"{prefix}[{marker}] {content}";
            else
                line = $"{prefix}[{marker}][{task}] {content}";

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
                modifiedLine = $"{prefix}[{marker}] {content}";
            else
                modifiedLine = $"{prefix}[{marker}][{task}] {content}";

            return modifiedLine;
        }

        /// <summary>
        /// Replace the task in a vote line without modifying any BBCode markup.
        /// </summary>
        /// <param name="voteLine">The original vote line.</param>
        /// <param name="newTask">The new task to apply.
        /// Null or an empty string removes any existing task.</param>
        /// <returns>Returns the vote line with the task replaced, or the original
        /// string if the original line couldn't be matched by the regex.</returns>
        public static string ReplaceTask(string voteLine, string newTask)
        {
            Match m = taskRegex.Match(voteLine);
            if (m.Success)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(m.Groups["pre"]);

                if (newTask != null && newTask != string.Empty)
                {
                    sb.Append($"[{newTask}]");
                }

                sb.Append(" ");

                sb.Append(m.Groups["remainder"]);

                return sb.ToString();
            }

            return voteLine;
        }

        /// <summary>
        /// Get whether the vote line is a ranked vote line (ie: marker uses digits 1-9).
        /// </summary>
        /// <param name="voteLine">The vote line being examined.</param>
        /// <returns>Returns true if the vote marker is a digit, false if not.</returns>
        public static bool IsRankedVote(string voteLine)
        {
            string marker = VoteString.GetVoteMarker(voteLine);

            if (marker == string.Empty)
                return false;

            return char.IsDigit(marker, 0);
        }
    }
}
