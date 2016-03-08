using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Votes
{
    public class UndoAction
    {
        public VoteType VoteType { get; }
        public string FromVote { get; }
        public string ToVote { get; }
        public string DeletedVote { get; }
        HashSet<string> FromVoters { get; }
        HashSet<string> ToVoters { get; }
        HashSet<string> DeletedVoters { get; }

        public UndoAction(VoteType voteType, string fromVote = null, string toVote = null, string deletedVote = null,
            HashSet<string> fromVoters = null, HashSet<string> toVoters = null, HashSet<string> deletedVoters = null)
        {
            VoteType = voteType;

            FromVote = fromVote;
            ToVote = toVote;
            DeletedVote = deletedVote;

            FromVoters = new HashSet<string>(fromVoters ?? Enumerable.Empty<string>());
            ToVoters = new HashSet<string>(toVoters ?? Enumerable.Empty<string>());
            DeletedVoters = new HashSet<string>(deletedVoters ?? Enumerable.Empty<string>());
        }
    }
}
