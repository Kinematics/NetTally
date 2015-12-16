using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public static class VoteCounter
    {
        public static IVoteCounter Instance { get; } = VoteCounter1.Instance;
    }
}
