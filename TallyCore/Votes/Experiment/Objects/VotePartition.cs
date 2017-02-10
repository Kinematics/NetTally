using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    public class VotePartition
    {
        #region Properties and fields
        public VoteLineSequence VoteLines { get; } = new VoteLineSequence();
        public VoteType VoteType { get; private set; } = VoteType.Vote;
        public string Task { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Construct an empty <see cref="VotePartition"/>
        /// </summary>
        public VotePartition()
        {
            Task = string.Empty;
        }

        /// <summary>
        /// Construct new <see cref="VotePartition"/>
        /// </summary>
        /// <param name="voteLine">The vote line that the partition is composed of.</param>
        /// <exception cref="ArgumentNullException"/>
        public VotePartition(VoteLine voteLine, VoteType voteType)
        {
            VoteType = voteType;
            AddLine(voteLine);
        }

        /// <summary>
        /// Construct new <see cref="VotePartition"/>
        /// </summary>
        /// <param name="voteLines">The vote lines that the partition is composed of.</param>
        /// <exception cref="ArgumentNullException"/>
        public VotePartition(IEnumerable<VoteLine> voteLines, VoteType voteType)
        {
            VoteType = voteType;
            AddLines(voteLines);
        }
        #endregion

        #region Public modification methods
        /// <summary>
        /// Add a vote line to the current partition.
        /// This function changes the hash code.
        /// </summary>
        /// <param name="line">The line to add.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddLine(VoteLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            if (!VoteLines.Any())
                Task = line.Task;

            VoteLines.Add(line);
        }

        /// <summary>
        /// Add a list of vote lines to the current partition.
        /// This function changes the hash code.
        /// </summary>
        /// <param name="line">The lines to add.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddLines(IEnumerable<VoteLine> lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));
            if (!lines.Any())
                return;
            if (lines.Any(a => a == null))
                throw new ArgumentException("Null vote line contained in enumeration.", nameof(lines));

            if (!VoteLines.Any())
                Task = lines.First().Task;

            VoteLines.AddRange(lines);
        }

        /// <summary>
        /// Modifies the task of the current vote partition, and returns a new version with the changes.
        /// </summary>
        /// <param name="task">The new task.</param>
        /// <returns>Returns a new VotePartition with the task modified.</returns>
        /// <exception cref="System.ArgumentNullException">task</exception>
        public VotePartition ModifyTask(string task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (VoteLines.Any())
            {
                var revise = VoteLines.First().Modify(task: task);
                var revLines = VoteLines.Skip(1).ToList();

                List<VoteLine> newList = new List<VoteLine> { revise };
                newList.AddRange(revLines);

                return new VotePartition(newList, VoteType);
            }

            return new VotePartition() { VoteType = VoteType };
        }
        #endregion

        #region Object overrides
        public override int GetHashCode()
        {
            return VoteLines.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is VotePartition other)
                return Agnostic.StringComparer.Equals(Task, other.Task) && VoteLines.Equals(other.VoteLines);

            return false;
        }

        public bool Matches(VotePartition other)
        {
            return VoteLines.Equals(other.VoteLines);
        }
        #endregion

        public static List<VotePartition> GetPartitionsFrom(Vote vote, PartitionMode partitionMode)
        {
            List<VotePartition> partitions = new List<VotePartition>();


            return partitions;
        }
    }
}
