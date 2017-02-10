using System.Collections.Generic;
using System.Linq;

namespace NetTally.Votes.Experiment
{
    public enum UndoItemType
    {
        Merge,
        Join,
        RemoveVote,
        RemoveVoter,
    }

    public class UndoItem
    {
        public UndoItemType UndoType { get; }
        public VoteType VoteType { get; }

        public VotePartition Vote1 { get; }
        public VotePartition Vote2 { get; }
        public List<VotePartition> Votes1 { get; }
        public List<VotePartition> Votes2 { get; }

        public Identity Voter1 { get; }
        public Identity Voter2 { get; }
        public List<Identity> Voters1 { get; }
        public List<Identity> Voters2 { get; }


        public UndoItem(UndoItemType undoType, VoteType voteType,
            VotePartition vote1 = null, VotePartition vote2 = null, 
            Identity voter1 = null, Identity voter2 = null,
            IEnumerable<VotePartition> votes1 = null, IEnumerable<VotePartition> votes2 = null,
            IEnumerable<Identity> voters1 = null, IEnumerable<Identity> voters2 = null)
        {
            UndoType = undoType;
            VoteType = voteType;

            Vote1 = vote1;
            Vote2 = vote2;
            Votes1 = votes1?.ToList();
            Votes2 = votes2?.ToList();
            Voter1 = voter1;
            Voter2 = voter2;
            Voters1 = voters1?.ToList();
            Voters2 = voters2?.ToList();
        }
    }
}
