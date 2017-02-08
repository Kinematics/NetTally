using System;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate an individual vote lines.
    /// </summary>
    public class VoteLine
    {
        #region Regexes
        // Regex to get the different parts of the vote. Only evaluates a single line.  Anything beyond a CRLF is dropped.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>(?<vote>[xX✓✔])|(?:(?<rank>[#])|(?<score>[+]))?(?<value>[1-9])|(?<approval>[-+])|(?<continuation>[*]))\s*\]\s*(?:\[\s*(?<task>[^]]+)\])?\s*(?<content>.*)");
        // Regex for just the marker portion of a vote line.
        static readonly Regex markerRegex = new Regex(@"(?<marker>(?<vote>[xX✓✔])|(?:(?<rank>[#])|(?<score>[+]))?(?<value>[1-9])|(?<approval>[-+])|(?<continuation>[*]))");
        // Regex for stripping out whitespace, punctuation, and symbols.
        static readonly Regex symbolRegex = new Regex(@"[\s\p{S}\p{P}]");
        #endregion

        #region Properties
        public string Text { get; private set; }
        public string TextWithoutBBCode { get; private set; }

        public string Prefix { get; private set; }
        public string Marker { get; private set; }
        public MarkerType MarkerType { get; private set; }
        public int MarkerValue { get; private set; }
        public string Task { get; private set; }
        public string Content { get; private set; }
        public string CleanContent { get; private set; }
        public string StrippedContent { get; private set; }
        public string TrimmedContent { get; private set; }

        public static readonly VoteLine Empty = new VoteLine();
        private int hashcode = 0;
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
            CleanContent = string.Empty;
            StrippedContent = string.Empty;
            TrimmedContent = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoteLine"/> class.
        /// Constructs the components out of the provided text line.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <exception cref="ArgumentNullException">Parameter is null.</exception>
        public VoteLine(string textLine)
        {
            if (string.IsNullOrEmpty(textLine))
                throw new ArgumentNullException(nameof(textLine));

            Text = VoteString.CleanVoteLineBBCode(textLine);
            TextWithoutBBCode = VoteString.RemoveBBCode(Text);

            ParseLine(Text);

            SetHashCode();
        }

        /// <summary>
        /// Constructor that takes the components of a vote line as arguments.
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
            CleanContent = VoteString.DeUrlContent(VoteString.RemoveBBCode(Content));
            TrimmedContent = VoteString.TrimExtendedTextDescriptionOfContent(Content);

            IdentifyMarker();
            StripContent();

            Text = BuildText();
            TextWithoutBBCode = VoteString.RemoveBBCode(Text);

            SetHashCode();
        }
        #endregion

        #region Setup
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
                // Remove all extra spacing from the prefix.
                Prefix = m.Groups["prefix"].Value.Replace(" ", string.Empty);
                // If no Task, use an empty string.
                Task = m.Groups["task"]?.Value.Trim() ?? "";

                Content = m.Groups["content"].Value;
                CleanContent = VoteString.DeUrlContent(VoteString.RemoveBBCode(Content));
                TrimmedContent = VoteString.TrimExtendedTextDescriptionOfContent(Content);

                Marker = m.Groups["marker"].Value;

                IdentifyMarker();
                StripContent();
            }
            else
            {
                throw new ArgumentException("Failed to extract data from vote line.", nameof(text));
            }
        }

        /// <summary>
        /// Examine the Marker to set the MarkerType and MarkerValue.
        /// </summary>
        private void IdentifyMarker()
        {
            Match m = markerRegex.Match(Marker);
            if (m.Success)
            {
                if (m.Groups["vote"].Success)
                    MarkerType = MarkerType.Vote;
                else if (m.Groups["rank"].Success)
                    MarkerType = MarkerType.Rank;
                else if (m.Groups["score"].Success)
                    MarkerType = MarkerType.Score;
                else if (m.Groups["value"].Success)
                    MarkerType = MarkerType.Rank; // Insert default value type here.
                else if (m.Groups["approval"].Success)
                    MarkerType = MarkerType.Approval;
                else if (m.Groups["continuation"].Success)
                    MarkerType = MarkerType.Continuation;
                else
                    throw new ArgumentException($"Unknown vote marker: {Marker}.");

                if (m.Groups["value"].Success)
                {
                    MarkerValue = int.Parse(m.Groups["value"].Value);
                }
            }
        }

        /// <summary>
        /// Strip the clean content version of the vote line of all diacriticals,
        /// and (if option is set to ignore) all whitespace, punctuation, and symbols.
        /// </summary>
        private void StripContent()
        {
            // Strip all diacritical variants down to basic latin form.
            string stripped = CleanContent.RemoveDiacritics();

            // Strip all whitespace and punctuation if it's not significant.
            if (!AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant)
            {
                stripped = symbolRegex.Replace(stripped, "");
            }

            StrippedContent = stripped;
        }

        #endregion

        #region Public Create/Copy/Modify Methods        
        /// <summary>
        /// Creates a vote line object from the specified text line.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <returns>Returns a new VoteLine if it's a valid vote line.  Otherwise, null.</returns>
        public static VoteLine Create(string textLine)
        {
            try
            {
                return new VoteLine(textLine);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Copy the current vote line into a new instance.
        /// </summary>
        public VoteLine Copy()
        {
            return new VoteLine()
            {
                Text = Text,
                TextWithoutBBCode = TextWithoutBBCode,
                Content = Content,
                CleanContent = CleanContent,
                StrippedContent = StrippedContent,
                TrimmedContent = TrimmedContent,
                Marker = Marker,
                MarkerType = MarkerType,
                MarkerValue = MarkerValue,
                Prefix = Prefix,
                Task = Task,
                hashcode = hashcode
            };
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
                prefix ?? Prefix,
                marker ?? Marker,
                task ?? Task,
                content ?? Content);
        }

        #endregion

        #region String Building Functions
        private string BuildText() => $"{Prefix}[{Marker}]{(string.IsNullOrEmpty(Task) ? "" : $"[{Task}]")} {Content}";
        private string BuildTrimmedText() => $"{Prefix}[{Marker}]{(string.IsNullOrEmpty(Task) ? "" : $"[{Task}]")} {TrimmedContent}";

        public string Condensed() => $"[{Task}] {Content}";
        #endregion

        #region Overrides
        public override string ToString()
        {
            return AdvancedOptions.Instance.TrimExtendedText ? BuildTrimmedText() : BuildText();
        }

        private void SetHashCode()
        {
            hashcode = StrippedContent.GetHashCode();
        }

        public override int GetHashCode()
        {
            return hashcode;
        }

        public override bool Equals(object obj)
        {
            if (obj is VoteLine other)
            {
                return Agnostic.StringComparer.Equals(CleanContent, other.CleanContent);
            }

            return false;
        }
        #endregion
    }
}
