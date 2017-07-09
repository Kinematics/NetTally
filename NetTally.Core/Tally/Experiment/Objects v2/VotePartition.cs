using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class VotePartition
    {
        public List<VoteLine> VoteLines { get; }
        public MarkerType PartitionType { get; private set; } = MarkerType.None;

        public VotePartition()
        {
            VoteLines = new List<VoteLine>();
        }

        public VotePartition(VoteLine voteLine)
            : this(new List<VoteLine> { voteLine })
        {
        }

        public VotePartition(IEnumerable<VoteLine> voteLines)
        {
            if (voteLines == null)
                throw new ArgumentNullException(nameof(voteLines));

            VoteLines = new List<VoteLine>(voteLines);

            if (VoteLines.Count > 0)
            {
                PartitionType = VoteLines[0].MarkerType;

                if (!VoteLines.All(v => v.MarkerType == PartitionType))
                    throw new ArgumentException($"Not all provided vote lines were of the same vote type. Expected: {PartitionType}");
            }
        }

        public void Add(VoteLine voteLine)
        {
            if (voteLine == null)
                throw new ArgumentNullException(nameof(voteLine));

            if (VoteLines.Count == 0)
                PartitionType = voteLine.MarkerType;
            else if (voteLine.MarkerType != PartitionType)
                throw new ArgumentException($"Attempted to add a vote line of type {voteLine.MarkerType} to a partition of type {PartitionType}.");

            VoteLines.Add(voteLine);
        }

        public void ChangeTask(string task)
        {
            if (VoteLines.Count > 0)
            {
                VoteLines[0] = VoteLines[0].Modify(task: task);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var line in VoteLines)
            {
                sb.AppendLine(line.ToString());
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is VotePartition other)
            {
                if (VoteLines.Count != other.VoteLines.Count)
                    return false;

                return VoteLines.Zip(other.VoteLines, (a, b) => a.Equals(b)).All(z => z);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            foreach (var line in VoteLines)
            {
                hash ^= line.GetHashCode();
            }

            return hash;
        }
    }
}
