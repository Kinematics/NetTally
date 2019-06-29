using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.Utility;

namespace NetTally.Votes.Experiment2
{
    class VoteLine : IEquatable<VoteLine>
    {
        public string Prefix { get; }
        public string Marker { get; }
        public string Task { get; }
        public string Content { get; }
        public MarkerType MarkerType { get; }
        public int MarkerValue { get; }

        string toString = "";
        int hash;

        public static readonly VoteLine Empty = new VoteLine("", "", "", "", MarkerType.None, 0);

        public VoteLine(string prefix, string marker, string task, string content, MarkerType markerType, int markerValue)
        {
            if (string.IsNullOrEmpty(marker))
                throw new ArgumentNullException(nameof(marker));
            if (markerType == MarkerType.None)
                throw new ArgumentOutOfRangeException(nameof(markerType), $"Invalid marker type {markerType} for vote line. (Marker: {marker??""}, content: {content??""}");

            Prefix = prefix ?? "";
            Marker = marker.Trim();
            Task = task?.Trim() ?? "";
            Content = content?.Trim() ?? "";
            MarkerType = markerType;
            MarkerValue = markerValue;

            MakeString();
            MakeHash();
        }

        public VoteLine Modify(string? prefix = null, string? task = null, string? content = null)
        {
            return new VoteLine(prefix ?? Prefix, Marker, task ?? Task, content ?? Content, MarkerType, MarkerValue);
        }

        public bool Matches(VoteLine other)
        {
            if (other == null)
                return false;

            return (MarkerType == other.MarkerType && Agnostic.StringComparer.Equals(Task, other.Task) && Agnostic.StringComparer.Equals(Content, other.Content));
        }

        public bool ContentMatches(VoteLine other)
        {
            if (other == null)
                return false;

            return (MarkerType == other.MarkerType && Agnostic.StringComparer.Equals(Content, other.Content));
        }

        public override bool Equals(object obj)
        {
            if (obj is VoteLine other)
            {
                return this.Equals(other);
            }

            return false;
        }

        public bool Equals(VoteLine other)
        {
            return (Matches(other) && Prefix == other.Prefix && MarkerValue == other.MarkerValue);
        }

        private void MakeHash()
        {
            var minified = Content.RemoveDiacritics().ToLowerInvariant().Where(a => a != ' ');

            StringBuilder sb = new StringBuilder();
            foreach (var c in minified)
            {
                sb.Append(c);
            }

            hash = sb.ToString().GetHashCode();
        }

        public override int GetHashCode()
        {
            return hash;
        }

        private void MakeString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Prefix);
            sb.Append($"[{Marker}]");

            if (Task.Length > 0)
                sb.Append($"[{Task}]");

            sb.Append(" ");
            sb.Append(Content);

            toString = sb.ToString();
        }

        public override string ToString()
        {
            return toString;
        }
    }
}
