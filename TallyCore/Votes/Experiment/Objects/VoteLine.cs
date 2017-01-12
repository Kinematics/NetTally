using System;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    public class VoteLine
    {
        // Regex to get the different parts of the vote. Only evaluates a single line.  Anything beyond a CRLF is dropped.
        static readonly Regex voteLineRege1 = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>[xX✓✔1-9])\s*\]\s*(?:\[\s*(?<task>[^]]+)\])?\s*(?<content>.*)");
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>(?<vote>[xX✓✔])|(?:(?<rank>#)|(?<score>+))?(?<value>[1-9])|(?<approval>[+-]))\s*\]\s*(?:\[\s*(?<task>[^]]+)\])?\s*(?<content>.*)");

        #region Properties
        public string Text { get; }
        public string TextWithoutBBCode { get; }

        public string Prefix { get; private set; }
        public string Marker { get; private set; }
        public MarkerType MarkerType { get; private set; }
        public int MarkerValue { get; private set; }
        public string Task { get; private set; }
        public string Content { get; private set; }
        public string CleanContent { get; private set; }
        public string TrimmedContent { get; private set; }

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
            MarkerType = MarkerType.None;
            Task = string.Empty;
            Content = string.Empty;
            TrimmedContent = string.Empty;
        }

        /// <summary>
        /// General new line constructor.  Only called from Create().
        /// Initializes a new instance of the <see cref="VoteLine"/> class.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <exception cref="System.ArgumentException">Failed to extract data from vote line.</exception>
        public VoteLine(string textLine)
            : this(textLine, VoteString.RemoveBBCode(textLine))
        {
        }

        /// <summary>
        /// General new line constructor.  Only called from Create().
        /// Initializes a new instance of the <see cref="VoteLine"/> class.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <param name="textWithoutBBCode">The text without bb code.</param>
        /// <exception cref="ArgumentNullException">Parameter is null.</exception>
        public VoteLine(string textLine, string textWithoutBBCode)
        {
            if (string.IsNullOrEmpty(textLine))
                throw new ArgumentNullException(nameof(textLine));
            if (string.IsNullOrEmpty(textWithoutBBCode))
                throw new ArgumentNullException(nameof(textWithoutBBCode));

            Text = VoteString.CleanVoteLineBBCode(textLine);

            TextWithoutBBCode = textWithoutBBCode;

            ParseLine(Text);
        }

        /// <summary>
        /// Run the provided vote line through the vote line regex and extract out the
        /// known components.
        /// </summary>
        /// <param name="text">The vote line to parse.</param>
        private void ParseLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            Match m = voteLineRegex.Match(text);
            if (m.Success)
            {
                Prefix = m.Groups["prefix"].Value.Replace(" ", string.Empty);
                Task = m.Groups["task"]?.Value.Trim() ?? "";

                Content = m.Groups["content"].Value;
                CleanContent = VoteString.DeUrlContent(VoteString.RemoveBBCode(Content));

                Marker = m.Groups["marker"].Value;

                if (m.Groups["vote"].Success)
                    MarkerType = MarkerType.Vote;
                else if (m.Groups["rank"].Success)
                    MarkerType = MarkerType.Rank;
                else if (m.Groups["score"].Success)
                    MarkerType = MarkerType.Score;
                else if (m.Groups["value"].Success)
                    MarkerType = MarkerType.Rank;
                else if (m.Groups["approval"].Success)
                    MarkerType = MarkerType.Approval;
                else
                    MarkerType = MarkerType.None;

                if (m.Groups["value"].Success)
                {
                    MarkerValue = int.Parse(m.Groups["value"].Value);
                }

                TrimmedContent = VoteString.TrimExtendedTextDescriptionOfContent(Content);
            }
            else
            {
                throw new ArgumentException("Failed to extract data from vote line.", nameof(text));
            }
        }

        /// <summary>
        /// Copy constructor.  Only called from Copy().
        /// Initializes a new instance of the <see cref="VoteLine"/> class.
        /// </summary>
        /// <param name="voteLine">The vote line.</param>
        public VoteLine Copy()
        {
            var copy = new VoteLine(Text, TextWithoutBBCode);

            copy.Content = Content;
            copy.Marker = Marker;
            copy.MarkerType = MarkerType;
            copy.MarkerValue = MarkerValue;
            copy.Prefix = Prefix;
            copy.Task = Task;
            copy.TrimmedContent = TrimmedContent;

            return copy;
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

        #region Overrides
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
        #endregion
    }
}
