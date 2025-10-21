using NetTally.Models.Votes;

namespace NetTally.Models.Utility;

/// <summary>
/// Extension class for promoting (reducing the depth of the prefix) <see cref="VoteLine"/>s
/// </summary>
public static class VoteLineUtility
{
    extension(VoteLine voteLine)
    {
        public int Depth => voteLine.Prefix.Depth;

        /// <summary>
        /// Promote a <see cref="VoteLine"/> by a specified depth level.
        /// </summary>
        /// <param name="promoteDepth">The number of steps to promote the line by. Default is 1.</param>
        /// <returns>A new version of the <see cref="VoteLine"/> after being promoted,
        /// or the same instance of no change was made.</returns>
        public VoteLine Promote(int promoteDepth = 1)
        {
            if (promoteDepth == 0)
                return voteLine;

            return voteLine with { Prefix = voteLine.Prefix.Promote(promoteDepth) };
        }

        /// <summary>
        /// Maximally promote a vote line by reducing the prefix depth to 0.
        /// </summary>
        /// <returns>The fully promoted <see cref="VoteLine"/></returns>
        public VoteLine FullPromote()
        {
            return voteLine.Promote(voteLine.Depth);
        }
    }
}
