using System;
using System.Text.RegularExpressions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// Class to encapsulate an individual vote line.
    /// </summary>
    public class VoteLine
    {
        #region Regexes
        // Regex to get the different parts of the vote. Only evaluates a single line.  Anything beyond a CRLF is dropped.
        static readonly Regex voteLineRegex = new Regex(@"^(?<prefix>[-\s]*)\[\s*(?<marker>(?<vote>[xX✓✔])|(?:(?<rank>[#])|(?<score>[+]))?(?<value>[1-9])|(?<approval>[-+])|(?<continuation>[*]))\s*\]\s*(?:\[(?<task>[^]]*)\])?\s*(?<content>.*)");
        // Regex for just the marker portion of a vote line.
        static readonly Regex markerRegex = new Regex(@"(?<marker>(?<vote>[xX✓✔])|(?:(?<rank>[#])|(?<score>[+]))?(?<value>[1-9])|(?<approval>[-+])|(?<continuation>[*]))");
        // Regex for stripping out whitespace, punctuation, and symbols.
        static readonly Regex symbolRegex = new Regex(@"[\s\p{S}\p{P}]");
        #endregion

        #region Properties        

        private string _content = null;
        private string _simplifiedContent = null;
        private string _marker = null;


        public string Text { get; private set; }

        public string Prefix { get; private set; }

        public string Marker
        {
            get
            {
                return _marker;
            }
            private set
            {
                if (value != _marker)
                {
                    _marker = value;
                    IdentifyMarker();
                }
            }
        }
        public MarkerType MarkerType { get; private set; } = MarkerType.None;
        public int MarkerValue { get; private set; }

        public string Task { get; private set; }

        /// <summary>
        /// The basic content is all the content of the vote, including BBCode and URLs,
        /// in the format it was originally submitted.
        /// </summary>
        public string Content
        {
            get
            {
                return _content;
            }
            set
            {
                if (value != _content)
                {
                    _content = value;
                    DecomposeContent();
                }
            }
        }
        /// <summary>
        /// The trimmed content is the content after it has (optionally) been
        /// shortened based on analysis heuristics.
        /// </summary>
        public string TrimmedContent { get; private set; }
        /// <summary>
        /// The display content is all the content of the vote, including BBCode and URLs,
        /// in the format it was originally submitted.
        /// </summary>
        public string DisplayContent { get; private set; }
        /// <summary>
        /// The comparable content is the content of the vote without any BBCode or URLs,
        /// but with non-latin characters still in their original form (eg: Æsir).
        /// </summary>
        public string ComparableContent { get; private set; }
        /// <summary>
        /// The simplified content is the comparable content text after decomposing
        /// diacriticals or other non-latin characters (eg: AEsir).
        /// The simplified content defines the hashcode for the VoteLine.
        /// </summary>
        public string SimplifiedContent
        {
            get
            {
                return _simplifiedContent;
            }
            set
            {
                if (value != _simplifiedContent)
                {
                    _simplifiedContent = value;
                    hashcode = _simplifiedContent.GetHashCode();
                }
            }
        }

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
            Prefix = string.Empty;
            Marker = string.Empty;
            Task = string.Empty;
            Content = string.Empty;
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

            DecomposeVoteLine(Text);
        }

        /// <summary>
        /// Constructor that takes the components of a vote line as arguments.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <param name="marker">The marker.</param>
        /// <param name="task">The task.</param>
        /// <param name="content">The content.</param>
        public VoteLine(string prefix, string marker, string task, string content)
        {
            Prefix = prefix ?? "";
            Marker = marker ?? "X";
            Task = task ?? "";
            Content = content ?? "";

            Text = Format();
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
            if (prefix == null && marker == null && task == null && content == null)
                return this;

            return new VoteLine(
                prefix ?? Prefix,
                marker ?? Marker,
                task ?? Task,
                content ?? Content);
        }

        #endregion

        #region Setup
        /// <summary>
        /// Run the provided vote line through the vote line regex and extract out the
        /// known components.
        /// </summary>
        /// <param name="text">The vote line to process.</param>
        private void DecomposeVoteLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            Match m = voteLineRegex.Match(text);
            if (m.Success)
            {
                // Remove all extra spacing from the prefix.
                Prefix = m.Groups["prefix"].Value.Replace(" ", string.Empty);

                // Marker is stored as-is.
                Marker = m.Groups["marker"].Value;

                // If no Task, use an empty string.
                Task = m.Groups["task"]?.Value.Trim() ?? "";

                // Content is stored as-is.
                Content = m.Groups["content"].Value;
            }
            else
            {
                throw new ArgumentException("Failed to extract data from vote line.", nameof(text));
            }
        }

        #region Marker-specific processing
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
            else
            {
                MarkerType = MarkerType.None;
            }
        }
        #endregion

        #region Content-specific processing        
        /// <summary>
        /// Decomposes the content into versions that may be used in other
        /// parts of the program.
        /// </summary>
        private void DecomposeContent()
        {
            TrimmedContent = GetTrimmedContent(Content);

            DisplayContent = GetDisplayContent(TrimmedContent);

            ComparableContent = GetComparableContent(TrimmedContent);

            SimplifiedContent = SimplifyContent(ComparableContent);
        }

        /// <summary>
        /// Gets the content after (possibly) trimming it down, based
        /// on global trimming options.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>Returns the content after trimming it.</returns>
        private string GetTrimmedContent(string content)
        {
            if (ViewModels.ViewModelService.MainViewModel.SelectedQuest.TrimExtendedText)
            {
                return VoteString.TrimExtendedTextDescriptionOfContent(content);
            }
            else
            {
                return content;
            }
        }

        /// <summary>
        /// Trims the provided content string if TrimExtendedText option is set.
        /// Otherwise returns the original string.
        /// </summary>
        /// <param name="content">The content that is the basis for the display content.</param>
        /// <returns>Returns vote content suitable for display.</returns>
        private string GetDisplayContent(string content)
        {
            return VoteString.FormatBBCodeForOutput(content);
        }

        /// <summary>
        /// Cleans the provided content string of problematic components that would
        /// make comparing two strings difficult, by removing excess BBCode.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>Returns vote content suitable for comparison.</returns>
        private string GetComparableContent(string content)
        {
            string clean = VoteString.RemoveBBCode(content);
            clean = VoteString.DeUrlContent(clean);

            return clean;
        }

        /// <summary>
        /// Simplifies the provided content by removing and normalizing diacriticals,
        /// and removing insignificant whitespace and punctuation.
        /// </summary>
        /// <param name="comparableContent"></param>
        /// <returns>Returns vote content suitable for comparison.</returns>
        private string SimplifyContent(string comparableContent)
        {
            // Strip all diacritical variants down to basic latin form.
            string stripped = comparableContent.RemoveDiacritics();

            // Strip all whitespace and punctuation if it's not significant.
            if (!ViewModels.ViewModelService.MainViewModel.SelectedQuest.WhitespaceAndPunctuationIsSignificant)
            {
                stripped = symbolRegex.Replace(stripped, "");
            }

            return stripped;
        }
        #endregion

        #endregion

        #region Overrides
        private string Format() => $"{Prefix}[{Marker}]{(string.IsNullOrEmpty(Task) ? "" : $"[{Task}]")} {DisplayContent}";

        public override string ToString()
        {
            return Format();
        }

        public string Condensed() => $"[{Task}] {DisplayContent}";

        public override int GetHashCode()
        {
            return hashcode;
        }

        public override bool Equals(object obj)
        {
            if (obj is VoteLine other)
            {
                // Check the ComparableContent first, then SimplifiedContent as a backup.
                return Agnostic.StringComparer.Equals(ComparableContent, other.ComparableContent) ||
                    Agnostic.StringComparer.Equals(SimplifiedContent, other.SimplifiedContent);
            }

            return false;
        }
        #endregion
    }
}
