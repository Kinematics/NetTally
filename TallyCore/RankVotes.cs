using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public static class RankVotes
    {
        public static void Rank(IVoteCounter voteCounter)
        {
            if (voteCounter == null)
                throw new ArgumentNullException(nameof(voteCounter));

            if (voteCounter.HasRankedVotes == false)
                throw new InvalidOperationException("There are no votes to rank.");



        }
    }
}
