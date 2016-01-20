using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally.Utility;

namespace NetTally.Votes
{
    public class Vote
    {
        public VoteLineSequence VoteLines { get; }

        public Vote(IEnumerable<VoteLine> voteLines)
        {
            VoteLines = new VoteLineSequence(voteLines);
        }

        public IEnumerable<VoteLineSequence> VoteBlocks
        {
            get
            {
                var voteBlocks = VoteLines.GroupAdjacentBySub(SelectSubLines, NonNullSelectSubLines);

                foreach (var block in voteBlocks)
                    yield return block as VoteLineSequence;
            }
        }

        /// <summary>
        /// Utility function to determine whether adjacent lines should
        /// be grouped together.
        /// Creates a grouping key for the provided line.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>Returns the line as the key if it's not a sub-vote line.
        /// Otherwise returns null.</returns>
        private static VoteLine SelectSubLines(VoteLine line)
        {
            if (string.IsNullOrEmpty(line.Prefix))
                return line;

            return null;
        }

        /// <summary>
        /// Supplementary function for line grouping, in the event that the first
        /// line of the vote is indented (and thus would normally generate a null key).
        /// </summary>
        /// <param name="line">The line to generate a key for.</param>
        /// <returns>Returns the line, or "Key", as the key for a line.</returns>
        private static VoteLine NonNullSelectSubLines(VoteLine line) => line ?? VoteLine.Empty;

    }
}
