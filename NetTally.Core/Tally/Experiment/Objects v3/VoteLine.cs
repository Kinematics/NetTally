using System;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public class VoteLine : IComparable, IComparable<VoteLine>, IEquatable<VoteLine>
    {
        public string Prefix { get; }
        public string Marker { get; }
        public string Task { get; }
        public string Content { get; }
        public string CleanContent { get; }
        public MarkerType MarkerType { get; }
        public int MarkerValue { get; }

        private int _hash;

        public static VoteLine Empty = new VoteLine("", "", "", "", MarkerType.None, 0);

        /// <summary>
        /// How many steps deep the prefix indicator places this line at.
        /// </summary>
        public int Depth { get; }

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

        public override string ToString()
        {
            string marker = Marker == "☒" || Marker == "☑" ? Marker : $"[{Marker}]";
            string task = Task.Length > 0 ? $"[{Task}]" : "";
            return $"{Prefix}{marker}{task} {Content}";
        }

        public string ToComparableString()
        {
            string task = Task.Length > 0 ? $"[{Task}]" : "";
            return $"{Prefix}[]{task} {CleanContent}";
        }

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
                    return string.Compare(left.Task, right.Task);
                }
            }
            else
            {
                return string.Compare(left.CleanContent, right.CleanContent);
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

    }
}