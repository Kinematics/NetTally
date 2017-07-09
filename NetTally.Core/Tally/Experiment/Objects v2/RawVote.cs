using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class RawVote
    {
        public Identity Identity { get; }
        public Post Origin { get; }
        public VotePartition Contents { get; }

        public RawVote(Identity identity, Post origin, VotePartition contents)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            Contents = contents ?? throw new ArgumentNullException(nameof(contents));
        }
    }
}
