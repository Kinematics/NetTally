using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTally.Votes.Experiment
{
    /// <summary>
    /// A customized version of a List of VoteLines that allows setting
    /// the hash code and checking for equivalence between two sequences.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.List{NetTally.Votes.Experiment.VoteLine}" />
    public class VoteLineSequence : List<VoteLine>
    {
        #region Constructors
        public VoteLineSequence()
        {

        }

        public VoteLineSequence(VoteLine voteLine)
        {
            if (voteLine == null)
                throw new ArgumentNullException(nameof(voteLine));

            Add(voteLine);
        }

        public VoteLineSequence(IEnumerable<VoteLine> voteLines)
        {
            if (voteLines == null)
                throw new ArgumentNullException(nameof(voteLines));
            if (voteLines.Any(a => a == null))
                throw new ArgumentException("Cannot add null lines to vote line sequence.", nameof(voteLines));

            AddRange(voteLines);
        }
        #endregion

        #region Adjust Add and Remove commands from underlying List
        public new void Add(VoteLine item)
        {
            base.Add(item);
            UpdateHashCode();
        }

        public new void AddRange(IEnumerable<VoteLine> items)
        {
            base.AddRange(items);
            UpdateHashCode();
        }

        public new bool Remove(VoteLine item)
        {
            bool result = base.Remove(item);
            UpdateHashCode();
            return result;
        }

        public new int RemoveAll(Predicate<VoteLine> match)
        {
            int result = base.RemoveAll(match);
            UpdateHashCode();
            return result;
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            UpdateHashCode();
        }

        public new void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
            UpdateHashCode();
        }

        public new void Clear()
        {
            base.Clear();
            UpdateHashCode();
        }
        #endregion

        #region Hash code handling
        private int hashcode = 0;

        private void UpdateHashCode()
        {
            hashcode = 0;

            if (this.Any())
            {
                // Add the hash from each vote line.
                foreach (var line in this)
                    hashcode ^= line.GetHashCode();
            }
        }

        public override int GetHashCode()
        {
            return hashcode;
        }
        #endregion

        #region Equals and ToString
        public override bool Equals(object obj)
        {
            if (obj is VoteLineSequence other && Count == other.Count)
            {
                return this.SequenceEqual(other);
            }

            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            bool preBreak = false;

            foreach (var line in this)
            {
                if (preBreak)
                    sb.Append("\n");
                preBreak = true;

                sb.Append(line.ToString());
            }

            return sb.ToString();
        }
        #endregion
    }
}
