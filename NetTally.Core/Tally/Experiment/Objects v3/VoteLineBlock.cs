using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Experiment3
{
    public class VoteLineBlock : IEnumerable<VoteLine>, IEquatable<VoteLineBlock>
    {
        public string Task { get; set; }
        public string Marker { get; }
        public MarkerType MarkerType { get; }
        public int MarkerValue { get; }

        public List<VoteLine> Lines { get; }

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
        }

        public VoteLineBlock(VoteLine source)
        {
            Lines = new List<VoteLine>() { source };

            Task = source.Task;
            Marker = source.Marker;
            MarkerType = source.MarkerType;
            MarkerValue = source.MarkerValue;
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
            return Equals(obj as VoteLineBlock);
        }

        public bool Equals(VoteLineBlock other)
        {
            if (other is null)
                return false;

            if (this.Lines.Count != other.Lines.Count)
                return false;

            if (this.Lines.Count == 0 && other.Lines.Count == 0)
                return true;

            if (this.Lines.First() != other.Lines.First())
                return false;

            var zip = Lines.Zip(other.Lines, (a, b) => new { a, b });

            foreach (var z in zip)
            {
                if (!Agnostic.StringComparer.Equals(z.a.Content, z.b.Content))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = int.MaxValue;

            foreach (var line in Lines)
            {
                hash ^= line.GetHashCode();
            }

            return hash;
        }
#nullable enable
    }
}
