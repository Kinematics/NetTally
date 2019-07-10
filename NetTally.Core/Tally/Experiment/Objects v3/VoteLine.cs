using System;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Experiment3
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

        private int _hash;

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
        /// Creates a copy of this vote line, but with the specified task value.
        /// </summary>
        /// <param name="task">The new task for the vote line.</param>
        /// <returns>Returns a new <see cref="VoteLine"/> with the task changed.</returns>
        public VoteLine WithTask(string task)
        {
            return new VoteLine(Prefix, Marker, task, Content, MarkerType, MarkerValue);
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
        /// Creates a copy of this vote line, but with the specified marker information.
        /// If the <paramref name="ifSameType"/> value is specified, but the markers are
        /// different, keep the original value.
        /// If this uses a MarkerType of None, then it will always update
        /// with the new values.
        /// </summary>
        /// <param name="marker">The marker to substitute.</param>
        /// <param name="markerType">The marker type to substitute.</param>
        /// <param name="markerValue">The marker value to substitute.</param>
        /// <param name="ifSameType">Flag whether it will modify the markers only when they are the same type.</param>
        /// <returns>Returns a new <see cref="VoteLine"/> with the marker parameters changed.</returns>
        public VoteLine WithMarker(string marker, MarkerType markerType, int markerValue, bool ifSameType = false)
        {
            if (!ifSameType || MarkerType == markerType || MarkerType == MarkerType.None)
            {
                return new VoteLine(Prefix, marker, Task, Content, markerType, markerValue);
            }
            else
            {
                return new VoteLine(Prefix, Marker, Task, Content, MarkerType, MarkerValue);
            }
        }
        #endregion

        #region ToString variations
        /// <summary>
        /// Formats the current object as a string.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public override string ToString()
        {
            string marker = Marker == "☒" || Marker == "☑" ? Marker : $"[{Marker}]";
            string task = Task.Length > 0 ? $"[{Task}]" : "";
            return $"{Prefix}{marker}{task} {Content}";
        }

        /// <summary>
        /// Creates a string that displays the cleaned content, and without any particular marker.
        /// </summary>
        /// <returns>Returns a string representing the current object.</returns>
        public string ToComparableString()
        {
            string task = Task.Length > 0 ? $"[{Task}]" : "";
            return $"{Prefix}[]{task} {CleanContent}";
        }

        /// <summary>
        /// Creates a string that displays the full vote line content, using the specified marker
        /// instead of the intrinsic vote line's.
        /// </summary>
        /// <param name="marker">The marker to use in the generated output.</param>
        /// <returns>Returns a string representing the current object.</returns>
        public string ToStringWithReplacement(string? marker = null, string? task = null)
        {
            marker ??= Marker;
            task ??= Task;
            task = task.Length > 0 ? $"[{task}]" : "";
            return $"{Prefix}[{marker}]{task} {Content}";
        }

        public string ManageVotesString => ToStringWithReplacement(marker: "");
        #endregion

        #region IComparable and IEquatable interface implementations.
#nullable disable
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