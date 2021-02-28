using System;
using System.Text.RegularExpressions;
using NetTally.Utility.Comparers;
using NetTally.Types.Enums;

namespace NetTally.Votes
{
    /// <summary>
    /// Immutable class storing data on a vote line.
    /// </summary>
    public class VoteLine : IComparable, IComparable<VoteLine>, IEquatable<VoteLine>
    {
        #region Construction and public properties
        public string Prefix { get; }
        public string Marker { get; }
        public string Task { get; }
        public string Content { get; }
        public string CleanContent { get; }
        public MarkerType MarkerType { get; }
        public int MarkerValue { get; }

        private readonly int _hash;

        /// <summary>
        /// How many steps deep the prefix indicator places this line at.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Default empty vote line.
        /// </summary>
        public static VoteLine Empty = new VoteLine("", "", "", "", MarkerType.None, 0);

        /// <summary>
        /// Constructor for the <see cref="VoteLine"/> class.
        /// </summary>
        /// <param name="prefix">Optional indention prefix for the vote line.</param>
        /// <param name="marker">The vote marker used for the vote line.</param>
        /// <param name="task">The task specified for the vote line.</param>
        /// <param name="content">The content of the vote line.</param>
        /// <param name="markerType">The classification of the marker used for the vote.</param>
        /// <param name="markerValue">The value of the marker used for the vote.</param>
        public VoteLine(string prefix, string marker, string task, string content, MarkerType markerType, int markerValue)
        {
            Prefix = prefix;
            Marker = marker;
            Task = task.Trim();
            Content = content.Trim();
            MarkerType = markerType;
            MarkerValue = markerValue;

            Depth = Prefix.Length;
            CleanContent = VoteLineParser.StripBBCode(Content);
            _hash = Agnostic.InsensitiveComparer.GetHashCode(CleanContent);
        }
        #endregion

        #region Creation of new VoteLine instances based on the current one
        /// <summary>
        /// Return a new version of the current instance, with a specified number of
        /// indention levels removed.  Default is 1.
        /// </summary>
        /// <param name="level">The number of indention levels to remove.</param>
        /// <returns>Returns a new vote line at the new indention level, or
        /// the current vote line if nothing was changed.</returns>
        public VoteLine GetPromotedLine(int level = 1)
        {
            if (level == 0)
                return this;

            if (level < 1)
                level = 1;

            if (level > Depth)
                level = Depth;

            string prefix = Depth > 0 ? Prefix.Substring(level) : "";

            return new VoteLine(prefix, Marker, Task, Content, MarkerType, MarkerValue);
        }

        /// <summary>
        /// Creates a copy of this vote line, but with the specified prefix depth.
        /// </summary>
        /// <param name="prefixDepth">The prefix depth we want this vote line at.</param>
        /// <returns>Returns a new vote line if the prefix changed, or this vote line if it stayed the same.</returns>
        public VoteLine WithPrefixDepth(int prefixDepth)
        {
            if (prefixDepth < 0)
                prefixDepth = 0;

            if (prefixDepth == Depth)
                return this;

            string adjPrefix = new string('-', prefixDepth);

            return new VoteLine(adjPrefix, Marker, Task, Content, MarkerType, MarkerValue);
        }

        /// <summary>
        /// Creates a copy of this vote line, but with the specified marker information.
        /// TODO: Obsolete comments
        /// If the <paramref name="ifSameType"/> value is specified, but the markers are
        /// different, keep the original value.
        /// If this uses a MarkerType of None, then it will always update
        /// with the new values.
        /// </summary>
        /// <param name="marker">The marker to substitute.</param>
        /// <param name="markerType">The marker type to substitute.</param>
        /// <param name="markerValue">The marker value to substitute.</param>
        /// <returns>Returns a new <see cref="VoteLine"/> with the marker parameters changed.</returns>
        public VoteLine WithMarker(string marker, MarkerType markerType, int markerValue)
        {
            if (Marker == marker && MarkerType == markerType && MarkerValue == markerValue)
                return this;

            return new VoteLine(Prefix, marker, Task, Content, markerType, markerValue);
        }

        /// <summary>
        /// Creates a copy of this vote line, but with the specified task value.
        /// </summary>
        /// <param name="task">The new task for the vote line.</param>
        /// <returns>Returns a new <see cref="VoteLine"/> with the task changed.</returns>
        public VoteLine WithTask(string task)
        {
            if (Task == task)
                return this;

            return new VoteLine(Prefix, Marker, task, Content, MarkerType, MarkerValue);
        }

        /// <summary>
        /// Creates a copy of this vote line, but with the specified marker and task values.
        /// </summary>
        /// <param name="task">The new task for the vote line.</param>
        /// <returns>Returns a new <see cref="VoteLine"/> with the task changed.</returns>
        public VoteLine WithMarkerAndTask(string marker, MarkerType markerType, int markerValue, string task)
        {
            if (Marker == marker && MarkerType == markerType && MarkerValue == markerValue && Task == task)
                return this;

            return new VoteLine(Prefix, marker, task, Content, markerType, markerValue);
        }

        /// <summary>
        /// Creates a copy of this vote line, but with the specified content.
        /// </summary>
        /// <param name="content">The new content for the vote line.</param>
        /// <returns>Returns a new <see cref="VoteLine"/> with the content changed.</returns>
        public VoteLine WithContent(string content)
        {
            return new VoteLine(Prefix, Marker, Task, content, MarkerType, MarkerValue);
        }

        /// <summary>
        /// Creates a copy of this vote line, but with content trimmed
        /// by the rules of trimming extended content.
        /// </summary>
        /// <returns>Returns a new version of this vote line with trimmed content.</returns>
        public VoteLine WithTrimmedContent()
        {
            string trimmedContent = GetTrimmedContent();
            return new VoteLine(Prefix, Marker, Task, trimmedContent, MarkerType, MarkerValue);
        }
        #endregion

        #region ToString variations
        /// <summary>
        /// Formats the current object as a string.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public override string ToString()
        {
            string task = Task.Length > 0 ? $"[{Task}]" : "";
            return $"{Prefix}[{Marker}]{task} {Content}";
        }

        /// <summary>
        /// Creates a string that displays the cleaned content, and without any particular marker.
        /// May display a provided task instead of the innate one.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public string ToComparableString(string? displayTask = null)
        {
            displayTask ??= Task;
            displayTask = displayTask.Length > 0 ? $"[{displayTask}]" : "";
            return $"{Prefix}[]{displayTask} {CleanContent}";
        }

        /// <summary>
        /// Creates a string that displays the full vote line content, using the specified marker
        /// and task instead of the intrinsic ones.
        /// </summary>
        /// <param name="displayMarker">The marker to use in the generated output.</param>
        /// <param name="displayTask">The task to use in the generated output.</param>
        /// <returns>Returns a string representing the current object.</returns>
        public string ToOverrideString(string? displayMarker = null, string? displayTask = null)
        {
            displayMarker ??= Marker;
            displayTask ??= Task;
            displayTask = displayTask.Length > 0 ? $"[{displayTask}]" : "";
            return $"{Prefix}[{displayMarker}]{displayTask} {Content}";
        }

        /// <summary>
        /// Formats a vote line for output, with optional override marker and task.
        /// </summary>
        /// <param name="displayMarker">The marker to use when displaying the vote line as a string.</param>
        /// <param name="displayTask">The task to use when displaying the vote line as a string.
        /// Will use the default task if left null.</param>
        /// <returns>Returns a string representing the current vote line.</returns>
        public string ToOutputString(string displayMarker = "X", string? displayTask = null)
        {
            displayMarker ??= Marker;
            displayTask ??= Task;
            displayTask = displayTask.Length > 0 ? $"[{displayTask}]" : "";
            string displayContent = FormatBBCodeForOutput(Content);
            return $"{Prefix}[{displayMarker}]{displayTask} {displayContent}";
        }

        /// <summary>
        /// Function to convert placeholder symbols in a vote line to BBCode brackets.
        /// </summary>
        /// <param name="input">An input string.</param>
        /// <returns>The string with any 『』 brackets converted to [].</returns>
        private static string FormatBBCodeForOutput(string input)
        {
            string output = input.Replace('『', '[');
            output = output.Replace('』', ']');

            return output;
        }

        #endregion

        #region Trimming Content
        private string GetTrimmedContent()
        {
            int trimIndex = GetTrimIndexForContent();

            if (trimIndex > 0)
                return CleanContent.Substring(0, trimIndex);
            else
                return CleanContent;
        }

        static readonly Regex extendedTextRegex = new Regex(@"(?<!\([^)]*)(((?<![pP][lL][aA][nN]\s*):(?!//))|—|(-(-+|\s+|\s*[^\p{Ll}])))");
        static readonly Regex extendedTextSentenceRegex = new Regex(@"(?<!\([^)]*)(?<![pP][lL][aA][nN]\b.+)(((?<=\S{4,})|(?<=\s[\p{Ll}]\S+))([.?!])(?:\s+[^\p{Ll}]))");

        /// <summary>
        /// Gets the index to trim from for a given content line.
        /// Determines the trim point as the last valid separation
        /// character that fits under the length limit.  If there are
        /// multiple separation points on the line, the untrimmed portion
        /// of the line must have more than one word in it.
        /// </summary>
        /// <param name="lineContent">Content of the vote line.</param>
        /// <returns>Returns the index that marks where to remove further text, or 0 if none.</returns>
        private int GetTrimIndexForContent()
        {
            // If content is less than about 8 words long, don't try to trim it.
            if (CleanContent.Length < 50)
                return 0;

            // The furthest into the content area that we're going to allow a
            // separator to be placed is 30% into the line length.
            int separatorLimit = CleanContent.Length * 3 / 10;

            // Colons are always allowed as separators, though it needs
            // to run through a regex to be sure it's not part of a plan
            // definition line, or part of an absolute path on Windows.

            // Em dashes are always allowed as separators.

            // Search for any instances of hyphens in the content.
            // Only counts if there's a space after the hyphen, or if
            // the next word starts with a capital letter.

            // Select the one that comes closest to, without passing,
            // the separator limit.


            MatchCollection matches = extendedTextRegex.Matches(CleanContent);

            // If there is only one separator, use it as long as it's within the limit.
            if (matches.Count == 1)
            {
                Match m = matches[0];
                if (m.Success && m.Index > 0 && m.Index < separatorLimit)
                {
                    return m.Index;
                }
            }
            // If there's more than one separator, take the last one that fits, but
            // only if there's more than one word before it.
            else if (matches.Count > 1)
            {
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Match m = matches[i];
                    if (m.Success && m.Index > 0 && m.Index < separatorLimit)
                    {
                        string partial = CleanContent.Substring(0, m.Index);

                        if (CountWords(partial) > 1)
                            return m.Index;
                    }
                }
            }

            // Alternate trimming that reduces the vote to only the first sentence.
            matches = extendedTextSentenceRegex.Matches(CleanContent);
            // Sentences may be taken up to half the line length.
            separatorLimit = CleanContent.Length / 2;

            if (matches.Count > 0)
            {
                Match m = matches[0];
                if (m.Success && m.Index > 0 && m.Index < separatorLimit)
                {
                    return m.Index + 1;
                }
            }

            // If no proper matches were found, return 0.
            return 0;
        }

        static readonly Regex wordCountRegex = new Regex(@"\S+\b");

        /// <summary>
        /// Counts the words in the provided string.
        /// </summary>
        /// <param name="partial">Part of a content line that we're going to count the words of.</param>
        /// <returns>Returns the number of words found in the provided string.</returns>
        private static int CountWords(string partial)
        {
            var matches = wordCountRegex.Matches(partial);
            return matches.Count;
        }
        #endregion

        #region IComparable and IEquatable interface implementations.
#nullable disable
        /// <summary>
        /// Compare two VoteLines with each other.  
        /// Task and Content are compared agnostically. Prefix depth matters.  Marker does not matter.
        /// We only care that the vote itself is the same, not whether the vote types or user preference levels are the same.
        /// </summary>
        /// <param name="left">A VoteLine to compare.</param>
        /// <param name="right">A VoteLine to compare.</param>
        /// <returns>Returns how the two vote lines compare to each other.</returns>
        public static int Compare(VoteLine left, VoteLine right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            if (Agnostic.StringComparer.Equals(left.CleanContent, right.CleanContent))
            {
                if (Agnostic.StringComparer.Equals(left.Task, right.Task))
                {
                    return 0 - left.Depth.CompareTo(right.Depth);
                }
                else
                {
                    return Agnostic.StringComparer.Compare(left.Task, right.Task);
                }
            }
            else
            {
                return Agnostic.StringComparer.Compare(left.CleanContent, right.CleanContent);
            }
        }

        public int CompareTo(VoteLine other)
        {
            return Compare(this, other);
        }

        public int CompareTo(object obj)
        {
            return Compare(this, obj as VoteLine);
        }

        public bool Equals(VoteLine other)
        {
            return Compare(this, other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Compare(this, obj as VoteLine) == 0;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public static bool operator >(VoteLine first, VoteLine second) => Compare(first, second) == 1;
        public static bool operator <(VoteLine first, VoteLine second) => Compare(first, second) == -1;
        public static bool operator >=(VoteLine first, VoteLine second) => Compare(first, second) >= 0;
        public static bool operator <=(VoteLine first, VoteLine second) => Compare(first, second) <= 0;
        public static bool operator ==(VoteLine first, VoteLine second) => Compare(first, second) == 0;
        public static bool operator !=(VoteLine first, VoteLine second) => Compare(first, second) != 0;
#nullable enable
        #endregion
    }
}