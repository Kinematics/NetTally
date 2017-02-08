﻿using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    public class VotePartition
    {
        #region Properties and fields
        private readonly List<VoteLine> voteLines = new List<VoteLine>();
        public IReadOnlyList<VoteLine> VoteLines { get { return voteLines; } }
        public bool IsEmpty => !voteLines.Any();

        public string Task { get; set; }

        private int hashcode = 0;
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
        public VotePartition(VoteLine voteLine)
        {
            AddLine(voteLine);
        }

        /// <summary>
        /// Construct new <see cref="VotePartition"/>
        /// </summary>
        /// <param name="voteLines">The vote lines that the partition is composed of.</param>
        /// <exception cref="ArgumentNullException"/>
        public VotePartition(IEnumerable<VoteLine> voteLines)
        {
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

            if (!voteLines.Any())
                Task = line.Task;

            voteLines.Add(line);

            UpdateHashCode();
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

            if (!voteLines.Any())
                Task = lines.First().Task;

            voteLines.AddRange(lines);

            UpdateHashCode();
        }
        #endregion

        #region Object overrides
        public override bool Equals(object obj)
        {
            if (obj is VotePartition other)
            {
                if (voteLines.Count != other.voteLines.Count)
                    return false;

                return voteLines.SequenceEqual(other.voteLines);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return hashcode;
        }

        private void UpdateHashCode()
        {
            if (voteLines.Any())
            {
                // Start from the task's hashcode.
                hashcode = Task.GetHashCode();

                // Then add the hash from each vote line.
                foreach (var line in voteLines)
                    hashcode ^= line.GetHashCode();
            }
            else
            {
                hashcode = 0;
            }
        }
        #endregion


        public static List<VotePartition> GetPartitionsFrom(Vote vote, PartitionMode partitionMode)
        {
            List<VotePartition> partitions = new List<VotePartition>();


            return partitions;
        }
    }
}