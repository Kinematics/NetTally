using System;
using System.Collections.Generic;
using System.Text;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    public class VotePartition
    {
        public PartitionMode PartitionMode { get; private set; }
        private readonly List<VoteLine> voteLines = new List<VoteLine>();
        public IReadOnlyList<VoteLine> VoteLines { get { return voteLines; } }

        public VotePartition()
        {

        }


        public static List<VotePartition> GetPartitionsFrom(Vote vote, PartitionMode partitionMode)
        {
            List<VotePartition> partitions = new List<VotePartition>();


            return partitions;
        }
    }
}
