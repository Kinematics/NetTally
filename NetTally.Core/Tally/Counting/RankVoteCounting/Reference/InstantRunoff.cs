using System.Collections.Generic;
using System.Linq;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    /// <summary>
    /// Implement ranking votes using the standard instant runoff method.
    /// Each round, the least liked of the top-ranked choices is removed.
    /// </summary>
    public class InstantRunoff : InstantRunoffBase
    {
        /// <summary>
        /// Gets the least preferred choice.
        /// In the standard Instant Runoff, this is the vote with the fewest
        /// number of top-ranked votes.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <returns>Returns the vote string for the least preferred vote.</returns>
        protected override VoteLineBlock GetLeastPreferredChoice(Dictionary<Origin, List<VoteLineBlock>> voterPreferences)
        {
            var highestRankings = voterPreferences.GroupBy(v => v.Value.First());

            var leastPreferred = highestRankings.MinObject(r => r.Count()).Key;

            return leastPreferred;
        }
    }
}
