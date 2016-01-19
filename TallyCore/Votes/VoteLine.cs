using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetTally.Votes
{
    public class VoteLine
    {
        // Formatting markup may be preserved within the content of the vote, but will be ignored
        // if it is outside the vote as a whole (eg: bolding the entire vote, vs individual words).

        static readonly Regex prePostRegex = new Regex(@"");
        // Regex to match any markup that we'll want to remove during comparisons.
        static readonly Regex markupRegex = new Regex(@"\[/?[ibu]\]|\[color[^]]*\]|\[/color\]");
        // Regex to get the different parts of the vote. Content includes only the first line of the vote.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>-*)\s*\[\s*(?<marker>[xX✓✔1-9])\s*\]\s*(\[\s*(?!url=)(?<task>[^]]*?)\s*\])?\s*(?<content>.*)");
        // Regex to allow us to collapse a vote to a commonly comparable version.
        static readonly Regex collapseRegex = new Regex(@"\s|\.");
        // Regex to allow us to strip leading dashes from a per-line vote.
        static readonly Regex leadHyphenRegex = new Regex(@"^-+");


        public string Text { get; }
        public string Prefix { get; }
        public string Marker { get; }
        public string Task { get; }
        public string Content { get; }

        public string CleanedText { get; }
        public string MinimizedText { get; }
        public bool MinimizeByLine { get; }

        public VoteLine(string voteLine, IQuest quest = null)
        {
            CleanedText = CleanText(voteLine);
            string prefix, marker, task, content;
            GetComponentsFromLine(voteLine, out prefix, out marker, out task, out content);

            Prefix = prefix;
            Marker = marker;
            Task = task;
            Content = content;

            MinimizeByLine = quest?.PartitionMode == PartitionMode.ByLine;
            MinimizedText = MinimizeVote();
        }

        public VoteLine(string prefix, string marker, string task, string content)
        {
            Prefix = prefix;
            Marker = marker;
            Task = task;
            Content = content;

            Text = BuildText();
            CleanedText = CleanText(Text);
        }

        private string BuildText()
        {
            return $"{Prefix}[{Marker}]" + (!string.IsNullOrEmpty(Task) ? $"[{Task}]" : "") + $" {Content}";
        }



        /// <summary>
        /// Given a vote line, remove any BBCode formatting chunks, and trim the result.
        /// </summary>
        /// <param name="voteLine">The vote line to examine.</param>
        /// <returns>Returns the vote line without any BBCode formatting.</returns>
        public string CleanText(string voteLine)
        {
            // Need to trim the result because removing markup may reveal new whitespace.
            return markupRegex.Replace(voteLine, "").Trim();
        }



        public void GetComponentsFromLine(string line, out string prefix, out string marker, out string task, out string content)
        {
            Match m = voteLineRegex.Match(CleanedText);
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
        /// Collapse a vote to a minimized form, for comparison.
        /// All BBCode markup is removed, along with all spaces and periods,
        /// and leading dashes when partitioning by line.  The text is then
        /// lowercased.
        /// </summary>
        /// <param name="voteLine">Original vote line to minimize.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns a minimized version of the vote string.</returns>
        public string MinimizeVote()
        {
            string collapsed = collapseRegex.Replace(CleanedText, "");
            collapsed = collapsed.ToLower();
            if (MinimizeByLine)
                collapsed = leadHyphenRegex.Replace(collapsed, "");

            return collapsed;
        }

    }
}
