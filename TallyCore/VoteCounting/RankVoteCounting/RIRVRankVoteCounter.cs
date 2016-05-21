using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.VoteCounting
{
    // List of preference results ordered by winner
    using RankResults = List<string>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;


    public class RIRVRankVoteCounter : BaseRankVoteCounter
    {
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            throw new NotImplementedException();
        }
    }
}
