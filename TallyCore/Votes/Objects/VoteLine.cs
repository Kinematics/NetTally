using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NetTally.Utility;

namespace NetTally.Votes
{
    public class VoteLine
    {
        // Regex to get the different parts of the vote. Only evaluates a single line.  Anything beyond a CRLF is dropped.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>[xX✓✔1-9])\s*\]\s*(?:\[\s*(?<task>[^]]*)\])?\s*(?<content>.*)");

        #region Properties
        public string Text { get; }
        public string TextWithoutBBCode { get; }

        public string Prefix { get; }
        public string Marker { get; }
        public string Task { get; }
        public string Content { get; }
        public string TrimmedContent { get; }

        public static readonly VoteLine Empty = new VoteLine();
        #endregion

        #region Constructors        
        /// <summary>
        /// Empty constructor.  Only called from Empty.
        /// Prevents a default instance of the <see cref="VoteLine"/> class from being created.
        /// </summary>
        private VoteLine()
        {
            Text = string.Empty;
            TextWithoutBBCode = string.Empty;
            Prefix = string.Empty;
            Marker = string.Empty;
            Task = string.Empty;
            Content = string.Empty;
            TrimmedContent = string.Empty;
        }

        /// <summary>
        /// General new line constructor.  Only called from Create().
        /// Initializes a new instance of the <see cref="VoteLine"/> class.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="textWithoutBBCode">The text without bb code.</param>
        /// <exception cref="System.ArgumentException">Failed to extract data from vote line.</exception>
        private VoteLine(string textLine, string textWithoutBBCode)
        {
            TextWithoutBBCode = textWithoutBBCode;

            Text = VoteString.CleanVoteLineBBCode(textLine);

            Match m = voteLineRegex.Match(textLine);
            if (m.Success)
            {
                Prefix = m.Groups["prefix"].Value.Replace(" ", string.Empty);
                Marker = m.Groups["marker"].Value;
                Task = m.Groups["task"]?.Value.Trim() ?? "";
                Content = m.Groups["content"].Value;

                TrimmedContent = VoteString.TrimExtendedTextDescriptionOfContent(Content);
            }
            else
            {
                throw new ArgumentException("Failed to extract data from vote line.", nameof(textLine));
            }
        }

        /// <summary>
        /// Copy constructor.  Only called from Copy().
        /// Initializes a new instance of the <see cref="VoteLine"/> class.
        /// </summary>
        /// <param name="voteLine">The vote line.</param>
        private VoteLine(VoteLine voteLine)
        {
            Text = voteLine.Text;
            TextWithoutBBCode = voteLine.TextWithoutBBCode;

            Prefix = voteLine.Prefix;
            Marker = voteLine.Marker;
            Task = voteLine.Task;
            Content = voteLine.Content;
            TrimmedContent = voteLine.TrimmedContent;
        }

        /// <summary>
        /// Modifying constructor.  Only called from Modify().
        /// Initializes a new instance of the <see cref="VoteLine"/> class.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="marker">The marker.</param>
        /// <param name="task">The task.</param>
        /// <param name="content">The content.</param>
        private VoteLine(string prefix, string marker, string task, string content)
        {
            Prefix = prefix ?? "";
            Marker = marker ?? "X";
            Task = task ?? "";
            Content = content ?? "";
            TrimmedContent = VoteString.TrimExtendedTextDescriptionOfContent(Content);

            Text = BuildText();
            TextWithoutBBCode = VoteString.RemoveBBCode(Text);
        }
        #endregion


        #region Public Methods        
        /// <summary>
        /// Creates a vote line object from the specified text line.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <returns>Returns a new VoteLine if it's a valid vote line.  Otherwise, null.</returns>
        public static VoteLine Create(string textLine)
        {
            string cleanTextLine = VoteString.RemoveBBCode(textLine);

            Match m = voteLineRegex.Match(cleanTextLine);
            if (m.Success)
            {
                return new VoteLine(textLine, cleanTextLine);
            }

            return null;
        }

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>Returns a copy of this instance.</returns>
        public VoteLine Copy()
        {
            return new VoteLine(this);
        }

        /// <summary>
        /// Modifies this instance.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="marker">The marker.</param>
        /// <param name="task">The task.</param>
        /// <param name="content">The content.</param>
        /// <returns>Returns a copy of this instance, with the specified modifications.</returns>
        public VoteLine Modify(string prefix = null, string marker = null, string task = null, string content = null)
        {
            return new VoteLine(
                prefix ?? this.Prefix,
                marker ?? this.Marker,
                task ?? this.Task,
                content ?? this.Content);
        }

        #endregion


        #region String Building Functions
        private string BuildText() => $"{Prefix}[{Marker}]{(string.IsNullOrEmpty(Task) ? "" : $"[{Task}]")} {Content}";
        private string BuildTrimmedText() => $"{Prefix}[{Marker}]{(string.IsNullOrEmpty(Task) ? "" : $"[{Task}]")} {TrimmedContent}";

        public string Condensed() => $"[{Task}] {Content}";

        public override string ToString() => BuildTrimmedText();
        #endregion

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is VoteLine other)
            {
                return Agnostic.StringComparer.Equals(Text, other.Text);
            }

            return false;
        }
    }
}
