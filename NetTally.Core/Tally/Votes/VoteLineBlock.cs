using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;

namespace NetTally.Votes
{
    public class VoteLineBlock : IEnumerable<VoteLine>, IEquatable<VoteLineBlock>, IComparable<VoteLineBlock>, IComparable
    {
        #region Construction and public properties
        public string Task { get; set; }
        public string Marker { get; private set; }
        public MarkerType MarkerType { get; private set; }
        public int MarkerValue { get; private set; }
        public IReadOnlyList<VoteLine> Lines { get; }
        public MarkerType Category { get; set; } = MarkerType.None;

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
        public VoteLineBlock Clone()
        {
            var copy = new VoteLineBlock(Lines);
            copy.Task = Task;
            copy.Marker = Marker;
            copy.MarkerType = MarkerType;
            copy.MarkerValue = MarkerValue;

            return copy;
        }

        public VoteLineBlock WithTask(string task)
        {
            VoteLineBlock partition = new VoteLineBlock(Lines);
            partition.Task = task;

            return partition;
        }

        /// <summary>
        /// Create a new instance of this vote block using a different marker.
        /// </summary>
        /// <param name="marker">Marker text.</param>
        /// <param name="markerType">Marker type.</param>
        /// <param name="markerValue">Marker value.</param>
        /// <returns>Returns a copy of the current VoteLineBlock with the markers changed.</returns>
        public VoteLineBlock WithMarker(string marker, MarkerType markerType, int markerValue)
        {
            /*
             * Scenarios:
             * 
             * 1) Merge votes. The voter's new vote needs to keep the same vote marker as the old vote.
             * -- Can change at the VoteLineBlock level with no issue. No options needed.
             * 
             * 2) When a vote is put into storage, the key vote is stripped of marker info
             * -- Can change at the VoteLineBlock level with no issue. No options needed.
             * 
             * 3) When normalizing a plan, the first line's content may be changed (eg: proposed plan ⇒ plan).
             *    The remaining lines are copied over.
             * -- The remaining lines do not need to be changed. The VoteLineBlock as a whole is set to MarkerType.None.
             * -- Can change at the VoteLineBlock level with no issue. No options needed.
             * 
             * 4) When extracting a reference plan to insert into a vote, it needs to use the
             *    user's marker information.
             * -- Can change at the VoteLineBlock level with no issue. No options needed.
             * 
             */

            VoteLineBlock partition = new VoteLineBlock(Lines);

            partition.Marker = marker;
            partition.MarkerType = markerType;
            partition.MarkerValue = markerValue;

            return partition;
        }
        #endregion

        #region ToString variations
        public override string ToString()
        {
            var first = Lines.First();
            string firstString = first.ToOverrideString(displayMarker: Marker, displayTask: Task);

            var aggregate = Lines.Select(s => s == first ? firstString : s.ToString()).Aggregate((a, b) => $"{a}\n{b}");

            return aggregate;
        }

        public string ToComparableString()
        {
            var first = Lines.First();
            string firstString = first.ToOverrideString(displayMarker: "", displayTask: Task);

            string aggregate = Lines.Select(s => s == first ? firstString : s.ToComparableString()).Aggregate((a, b) => $"{a}\n{b}");

            return aggregate;
        }

        public string ToOutputString(string mainDisplayMarker = "X", string subDisplayMarker = "X")
        {
            var first = Lines.First();
            string firstString = first.ToOutputString(displayMarker: mainDisplayMarker, displayTask: Task);

            var aggregate = Lines.Select(s => s == first ? firstString : s.ToOutputString(displayMarker: subDisplayMarker)).Aggregate((a, b) => $"{a}\n{b}");

            return aggregate ?? "";
        }

        public string ManageVotesDisplay
        {
            get
            {
                return ToOutputString(mainDisplayMarker: "", subDisplayMarker: "");
            }
        }

        #endregion

        #region IEnumerable, IComparable, and IEquatable interface implementations.

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case null:
                    return false;
                case VoteLineBlock vlb:
                    return Equals(vlb);
                case IEnumerable<VoteLine> lines:
                    return Equals(lines);
                default:
                    return false;
            }
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

        public int CompareTo(object obj)
        {
            switch (obj)
            {
                case null:
                    return 1;
                case VoteLineBlock vlb:
                    return Compare(this, vlb);
                case IEnumerable<VoteLine> lines:
                    return Compare(this, lines);
                default:
                    return 1;
            }
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
                    // MarkerType of None or Plan matches any other marker.
                    //if (result == 0 && (left.MarkerType != MarkerType.None && right.MarkerType != MarkerType.None)
                    //    && (left.MarkerType != MarkerType.Plan && right.MarkerType != MarkerType.Plan))
                    //{
                    //    result = left.MarkerType.CompareTo(right.MarkerType);
                    //}
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
                    if (result == 0 && (left.MarkerType != MarkerType.None && first.MarkerType != MarkerType.None)
                        && (left.MarkerType != MarkerType.Plan && first.MarkerType != MarkerType.Plan))
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
        #endregion
    }
}
