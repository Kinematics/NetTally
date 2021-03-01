using System.Collections.Generic;
using System.Linq;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Votes;
using NetTally.Types.Components;

namespace NetTally.VoteCounting.RankVotes.Reference
{
    /// <summary>
    /// Implement ranking votes using the Coombs method.
    /// This is an instant runoff that removes the most disliked
    /// option each round, instead of the least liked.
    /// </summary>
    public class Coombs : InstantRunoffBase
    {
        /// <summary>
        /// Gets the least preferred choice.
        /// Using Coombs Method, this is the option that has the greatest
        /// number of bottom-ranked votes.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <returns>Returns the vote string for the least preferred vote.</returns>
        protected override VoteLineBlock GetLeastPreferredChoice(Dictionary<Origin, List<VoteLineBlock>> voterPreferences)
        {
            var lowestRankings = voterPreferences.GroupBy(v => v.Value.Last());

            var leastPreferred = lowestRankings.MaxObject(r => r.Count()).Key;

            return leastPreferred;
        }
    }
}
