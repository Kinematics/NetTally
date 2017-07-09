using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class Supporter
    {
        public Identity Identity { get; }
        public MarkerType VoteType { get; }
        public int VoteValue { get; }

        public Supporter(Identity identity, MarkerType voteType, int voteValue)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            VoteType = voteType;
            VoteValue = voteValue;
        }
    }
}
