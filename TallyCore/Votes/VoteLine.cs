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
        // Regex to match any markup that we'll want to remove during comparisons.
        static readonly Regex markupRegex = new Regex(@"\[/?[ibu]\]|\[color[^]]*\]|\[/color\]");
        // Regex to get the different parts of the vote. Content includes only the first line of the vote.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>-*)\s*\[\s*(?<marker>[xX+✓✔1-9])\s*\]\s*(\[\s*(?<task>(\w|\d)(\s*(\w|\d)+)*\??)\s*\])?\s*(?<content>.*)");
        // Regex to allow us to collapse a vote to a commonly comparable version.
        static readonly Regex collapseRegex = new Regex(@"\s|\.");
        // Regex to allow us to strip leading dashes from a per-line vote.
        static readonly Regex leadHyphenRegex = new Regex(@"^-+");


        public string Text { get; set; }
        public string Prefix { get; set; }
        public string Marker { get; set; }
        public string Task { get; set; }
        public string Content { get; set; }

        public string CleanedText { get; set; }
        public string MinimizedText { get; set; }
        public bool MinimizeByLine { get; }

        public VoteLine(string voteLine, IQuest quest = null)
        {
            CleanedText = CleanText(voteLine);
            GetVoteComponents();

            MinimizeByLine = quest?.PartitionMode == PartitionMode.ByLine;
            MinimizeVote();
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


        public void GetVoteComponents()
        {
            Match m = voteLineRegex.Match(CleanedText);
            if (m.Success)
            {
                Prefix = m.Groups["prefix"].Value;
                Marker = m.Groups["marker"].Value;
                Content = m.Groups["content"].Value.Trim();

                Task = m.Groups["task"].Value.Trim();

                // A task name is composed of any number of characters or digits, with an optional ending question mark.
                // The returned value will capitalize the first letter, and lowercase any following letters.

                if (Task.Length == 1)
                    Task = Task.ToUpper();

                if (Task.Length > 1)
                    Task = char.ToUpper(Task[0]) + Task.Substring(1).ToLower();
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
        public void MinimizeVote()
        {
            string collapsed = collapseRegex.Replace(CleanedText, "");
            collapsed = collapsed.ToLower();
            if (MinimizeByLine)
                collapsed = leadHyphenRegex.Replace(collapsed, "");

            MinimizedText = collapsed;
        }

    }
}
