using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class Vote
    {
        public Origin Origin { get; }
        public VotePartition Contents { get; }

        public Vote(Origin origin, VotePartition contents)
        {
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            Contents = contents ?? throw new ArgumentNullException(nameof(contents));
        }
    }
}
