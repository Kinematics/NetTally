using System;
using System.Collections.Generic;
using System.Text;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    public class Pairwise : IRankVoteCounter2
    {
        public List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            throw new NotImplementedException();
        }
    }
}
