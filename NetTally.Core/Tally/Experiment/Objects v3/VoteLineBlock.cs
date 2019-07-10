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
        #region Construction and public properties
        public string Task { get; set; }
        public string Marker { get; }
        public MarkerType MarkerType { get; }
        public int MarkerValue { get; }
        public IReadOnlyList<VoteLine> Lines { get; }

        readonly int _hash;

        /// <summary>
        /// Construct a vote block based on a list of VoteLines.
        /// </summary>
        /// <param name="source">The source to create this VoteLineBlock from.</param>
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

        /// <summary>
        /// Construct a vote block based on a single VoteLine.
        /// </summary>
        /// <param name="source">The source to create this VoteLineBlock from.</param>
        public VoteLineBlock(VoteLine source)
            : this(Enumerable.Repeat(source, 1))
        {
        }

        /// <summary>
        /// Construct a vote block based on a list of VoteLineBlocks,
        /// decomposing each of their original vote line collections 
        /// to add to this one.
        /// </summary>
        /// <param name="source">The source to create this VoteLineBlock from.</param>
        public VoteLineBlock(IEnumerable<VoteLineBlock> source)
            : this(source.SelectMany(a => a))
        {
        }
        #endregion

        #region Creation of new VoteLineBlock instances based on the current one
        public VoteLineBlock WithTask(string task)
        {
            var firstLine = Lines.First().WithTask(task);

            var lines = new List<VoteLine>() { firstLine };
            lines.AddRange(Lines.Skip(1));

            return new VoteLineBlock(lines);
        }

        public VoteLineBlock WithMarker(string marker, MarkerType markerType, int markerValue, bool ifSameType = false, bool allLines = false)
        {
            var firstLine = Lines.First().WithMarker(marker, markerType, markerValue, ifSameType);

            var lines = new List<VoteLine>() { firstLine };

            var remaining = Lines.Skip(1);

            if (allLines)
            {
                lines.AddRange(remaining.Select(a => a.WithMarker(marker, markerType, markerValue)));
            }
            else
            {
                lines.AddRange(remaining);
            }

            return new VoteLineBlock(lines);
        }
        #endregion

        public HashSet<string> GetAllTasks()
        {
            return Lines.Where(l => !string.IsNullOrEmpty(l.Task)).Select(l => l.Task).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        #region ToString variations
        public override string ToString()
        {
            var first = Lines.First();
            string firstString = first.ToStringWithReplacement(task: Task);

            var aggregate = Lines.Select(s => s == first ? firstString : s.ToString()).Aggregate((a, b) => $"{a}\n{b}");

            return aggregate + "\n";
        }

        public string ToComparableString()
        {
            var aggregate = Lines.Select(s => s.ToComparableString()).Aggregate((a, b) => $"{a}\n{b}");
            return aggregate ?? "";
        }

        public string ToStringWithMarker(string marker = "X")
        {
            var first = Lines.First();
            string firstString = first.ToStringWithReplacement(marker: marker, task: Task);

            var aggregate = Lines.Select(s => s == first ? firstString : s.ToStringWithReplacement(marker: "X")).Aggregate((a, b) => $"{a}\n{b}");

            return aggregate ?? "";
        }
        #endregion

        #region IEnumerable, IComparable, and IEquatable interface implementations.

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
            return Compare(this, lines) == 0;
        }

        public bool Equals(VoteLineBlock other)
        {
            return Compare(this, other) == 0;
        }

        public int CompareTo(VoteLineBlock other)
        {
            return Compare(this, other);
        }

        public static int Compare(VoteLineBlock left, VoteLineBlock right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            int result = Agnostic.StringComparer.Compare(left.Task, right.Task);

            if (result == 0)
            {
                var zip = left.Lines.Zip(right.Lines, (a, b) => new { a, b });

                var first = zip.First();

                result = Agnostic.StringComparer.Compare(first.a.CleanContent, first.b.CleanContent);

                if (result == 0)
                {
                    foreach (var z in zip.Skip(1))
                    {
                        result = Agnostic.StringComparer.Compare(z.a.Task, z.b.Task);

                        if (result != 0)
                            return result;

                        result = Agnostic.StringComparer.Compare(z.a.CleanContent, z.b.CleanContent);

                        if (result != 0)
                            return result;
                    }

                    result = left.Lines.Count.CompareTo(right.Lines.Count);

                    // Lines do not compare markers for equality, but blocks do.
                    // MarkerType of None matches any other marker.
                    if (result == 0 && (left.MarkerType != MarkerType.None && right.MarkerType != MarkerType.None))
                    {
                        result = left.MarkerType.CompareTo(right.MarkerType);
                    }
                }
            }

            return result;
        }

        public static int Compare(VoteLineBlock left, IEnumerable<VoteLine> right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            if (!right.Any())
                return 1;

            var zip = left.Lines.Zip(right, (a, b) => new { a, b });

            var firstz = zip.First();
            var first = zip.First().b;

            int result = Agnostic.StringComparer.Compare(left.Task, first.Task);

            if (result == 0)
            {
                result = Agnostic.StringComparer.Compare(firstz.a.CleanContent, firstz.b.CleanContent);

                if (result == 0)
                {
                    foreach (var z in zip.Skip(1))
                    {
                        result = Agnostic.StringComparer.Compare(z.a.Task, z.b.Task);

                        if (result != 0)
                            return result;

                        result = Agnostic.StringComparer.Compare(z.a.CleanContent, z.b.CleanContent);

                        if (result != 0)
                            return result;
                    }

                    result = left.Lines.Count.CompareTo(right.Count());

                    // Lines do not compare markers for equality, but blocks do.
                    // MarkerType of None matches any other marker.
                    if (result == 0 && (left.MarkerType != MarkerType.None && first.MarkerType != MarkerType.None))
                    {
                        result = left.MarkerType.CompareTo(first.MarkerType);
                    }
                }
            }

            return result;
        }

        public static bool operator >(VoteLineBlock first, VoteLineBlock second) => Compare(first, second) == 1;
        public static bool operator <(VoteLineBlock first, VoteLineBlock second) => Compare(first, second) == -1;
        public static bool operator >=(VoteLineBlock first, VoteLineBlock second) => Compare(first, second) >= 0;
        public static bool operator <=(VoteLineBlock first, VoteLineBlock second) => Compare(first, second) <= 0;
        public static bool operator ==(VoteLineBlock first, VoteLineBlock second) => Compare(first, second) == 0;
        public static bool operator !=(VoteLineBlock first, VoteLineBlock second) => Compare(first, second) != 0;

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

        public IEnumerator<VoteLine> GetEnumerator()
        {
            foreach (var line in Lines)
                yield return line;
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<VoteLine>)this).GetEnumerator();
#nullable enable
        #endregion
    }
}
