using System.Collections.Generic;
using System.Linq;

namespace NetTally.Votes.Experiment
{
    public class VoteLineSequence : List<VoteLine>
    {
        public VoteLineSequence()
        {

        }

        public VoteLineSequence(IEnumerable<VoteLine> voteLines)
        {
            if (voteLines == null)
                return;

            AddRange(voteLines.Where(a => a != null));
        }

        public VoteLineSequence(VoteLine voteLine)
        {
            if (voteLine == null)
                return;

            Add(voteLine);
        }

        public override string ToString() => string.Join("\r\n", this.Select(x => x.Text).ToArray());
    }
}
