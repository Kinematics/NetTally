using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Votes
{
    public class VoteLineSequence : List<VoteLine>
    {
        public VoteLineSequence()
        {

        }

        public VoteLineSequence(IEnumerable<string> voteLines)
        {
            var linesAsVoteLines = voteLines.Select(a => VoteLine.Create(a));
            AddRange(linesAsVoteLines.Where(a => a != null));
        }

        public VoteLineSequence(IEnumerable<VoteLine> voteLines)
        {
            AddRange(voteLines.Where(a => a != null));
        }

        public override string ToString() => string.Join("\r\n", this.Select(x => x.Text).ToArray());
    }
}
