using System;
using System.Collections.Generic;
using System.Text;

namespace NetTally.Votes.Experiment2
{
    class Origin
    {
        public Identity Identity { get; }
        public Post Post { get; }

        public Origin(Identity identity, Post post)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Post = post ?? throw new ArgumentNullException(nameof(post));
        }
    }
}
