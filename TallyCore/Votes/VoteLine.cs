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
        // The minimal indicator that a text string is a vote line.
        static readonly Regex minimumVoteLineRegex = new Regex(@"^[-\s]*\[\s*[xX+✓✔1-9]\s*\]");
        // Regex to get the different parts of the vote. Only evaluates a single line.  Anything beyond a CRLF is dropped.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>[xX✓✔1-9])\s*\]\s*(\[\s*(?![bui]\]|color=|url=)(?<task>[^]]*?)\])?\s*(?<content>.*)");

        public string Text { get; }
        public string CleanedText { get; }

        public string Prefix { get; }
        public string Marker { get; }
        public string Task { get; }
        public string Content { get; }

        public static readonly VoteLine Empty = new VoteLine();

        private VoteLine()
        {
            Text = string.Empty;
            CleanedText = string.Empty;
            Prefix = string.Empty;
            Marker = string.Empty;
            Task = string.Empty;
            Content = string.Empty;
        }

        public VoteLine(string text)
        {
            string stripText = VoteString.RemoveBBCode(text);

            Match m = minimumVoteLineRegex.Match(stripText);
            if (!m.Success)
            {
                throw new ArgumentException("Not a valid vote line.", nameof(text));
            }

            Text = VoteString.CleanVoteLineBBCode(text);
            CleanedText = stripText;

            m = voteLineRegex.Match(Text);

            if (m.Success)
            {
                Prefix = m.Groups["prefix"].Value.Replace(" ", string.Empty);
                Marker = m.Groups["marker"].Value;
                Task = m.Groups["task"]?.Value.Trim() ?? "";
                Content = m.Groups["content"].Value;
            }
            else
            {
                throw new ArgumentException("Failed to extract data from vote line.", nameof(text));
            }
        }

        public VoteLine(string prefix, string marker, string task, string content)
        {
            Prefix = prefix ?? "";
            Marker = marker ?? "X";
            Task = task ?? "";
            Content = content ?? "";

            Text = BuildText();
            CleanedText = VoteString.RemoveBBCode(Text);
        }

        public VoteLine ModifyVoteLine(string prefix = null, string marker = null, string task = null, string content = null)
        {
            prefix = prefix ?? Prefix;
            marker = marker ?? Marker;
            task = task ?? Task;
            content = content ?? Content;

            return new VoteLine(prefix, marker, task, content);
        }


        private string BuildText() => $"{Prefix}[{Marker}]{(string.IsNullOrEmpty(Task) ? "" : $"[{Task}]")} {Content}";


    }
}
