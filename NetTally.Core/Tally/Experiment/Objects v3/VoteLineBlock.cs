using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public class VoteLineBlock : IEnumerable<VoteLine>, IEquatable<VoteLineBlock>, IComparable<VoteLineBlock>
    {
        public string Task { get; set; }
        public string Marker { get; }
        public MarkerType MarkerType { get; }
        public int MarkerValue { get; }
        public IReadOnlyList<VoteLine> Lines { get; }

        readonly int _hash;

        public VoteLineBlock(IEnumerable<VoteLine> source)
        {
            if (!source.Any())
                throw new ArgumentOutOfRangeException(nameof(source));

            Lines = source.ToList();

            var firstLine = Lines.First();
            Task = firstLine.Task;
            Marker = firstLine.Marker;
            MarkerType = firstLine.MarkerType;
            MarkerValue = firstLine.MarkerValue;
            _hash = ComputeHash();
        }

        public VoteLineBlock(VoteLine source)
            : this(Enumerable.Repeat(source, 1))
        {
        }

        public VoteLineBlock(IEnumerable<VoteLineBlock> source)
            : this(source.SelectMany(a => a))
        {
        }

        public VoteLineBlock WithTask(string task)
        {
            var firstLine = Lines.First().WithTask(task);

            var lines = new List<VoteLine>() { firstLine };
            lines.AddRange(Lines.Skip(1));

            return new VoteLineBlock(lines);
        }

        public VoteLineBlock WithMarker(string marker, MarkerType markerType, int markerValue, bool ifSameType = false)
        {
            var firstLine = Lines.First().WithMarker(marker, markerType, markerValue, ifSameType);

            var lines = new List<VoteLine>() { firstLine };
            lines.AddRange(Lines.Skip(1));

            return new VoteLineBlock(lines);
        }

        public HashSet<string> GetAllTasks()
        {
            return Lines.Where(l => !string.IsNullOrEmpty(l.Task)).Select(l => l.Task).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            var aggregate = Lines.Select(s => s.ToString()).Aggregate((a,b) => $"{a}\n{b}");
            return aggregate ?? "";
        }

        public string ToComparableString()
        {
            var aggregate = Lines.Select(s => s.ToComparableString()).Aggregate((a, b) => $"{a}\n{b}");
            return aggregate ?? "";
        }

        public string ToStringWithMarker(string marker = "X")
        {
            var first = Lines.First();
            var aggregate = Lines.Select(s => s == first ? s.ToStringWithMarker(marker) : s.ToStringWithMarker()).Aggregate((a, b) => $"{a}\n{b}");
            return aggregate ?? "";
        }

        public IEnumerator<VoteLine> GetEnumerator()
        {
            foreach (var line in Lines)
                yield return line;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<VoteLine>)this).GetEnumerator();

#nullable disable
        public override bool Equals(object obj)
        {
            return obj switch
            {
                null => false,
                VoteLineBlock vlb => Equals(vlb),
                IEnumerable<VoteLine> lines => Equals(lines),
                _ => false
            };
        }

        /// <summary>
        /// Do an equality check with just the vote lines.
        /// This is the equivalent to having a MarkerType of None.
        /// </summary>
        /// <param name="lines">The lines to compare to the current block.</param>
        /// <returns>Returns true if the lines and the vote block match up.</returns>
        public bool Equals(IEnumerable<VoteLine> lines)
        {
            if (this.Lines.Count != lines.Count())
                return false;

            var zip = Lines.Zip(lines, (a, b) => new { a, b });

            foreach (var z in zip)
            {
                if (!(z.a == z.b))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(VoteLineBlock other)
        {
            if (other is null)
                return false;

            if (this.Lines.Count != other.Lines.Count)
                return false;

            var zip = Lines.Zip(other.Lines, (a, b) => new { a, b });

            foreach (var z in zip)
            {
                if (!(z.a == z.b))
                {
                    return false;
                }
            }

            // Lines do not compare markers for equality, but blocks do.
            // MarkerType of None matches any other marker.
            return (MarkerType == other.MarkerType || MarkerType == MarkerType.None || other.MarkerType == MarkerType.None);
        }

        private int ComputeHash()
        {
            int hash = Lines.First().GetHashCode();

            foreach (var line in Lines.Skip(1))
            {
                hash ^= line.GetHashCode();
            }

            return hash;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public int CompareTo(VoteLineBlock other)
        {
            var zip = Lines.Zip(other.Lines, (a, b) => new { a, b });
            int result;

            foreach (var z in zip)
            {
                result = Agnostic.StringComparer.Compare(z.a.Task, z.b.Task);

                if (result != 0)
                    return result;

                result = Agnostic.StringComparer.Compare(z.a.CleanContent, z.b.CleanContent);

                if (result != 0)
                    return result;
            }

            return Lines.Count.CompareTo(other.Lines.Count);
        }
#nullable enable
    }
}
