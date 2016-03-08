using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Votes
{
    public enum UndoActionType
    {
        Merge,
        Join,
        Delete
    }

    public class UndoAction
    {
        public UndoActionType ActionType { get; }
        public VoteType VoteType { get; }

        public string Vote1 { get; }
        public string Vote2 { get; }
        public HashSet<string> Voters1 { get; }
        public HashSet<string> Voters2 { get; }

        public UndoAction(UndoActionType actionType, VoteType voteType,
            string vote1 = null, HashSet<string> voters1 = null,
            string vote2 = null, HashSet<string> voters2 = null)
        {
            ActionType = actionType;
            VoteType = voteType;

            Vote1 = vote1;
            Voters1 = new HashSet<string>(voters1 ?? Enumerable.Empty<string>());

            Vote2 = vote2;
            Voters2 = new HashSet<string>(voters2 ?? Enumerable.Empty<string>());
        }
    }
}
