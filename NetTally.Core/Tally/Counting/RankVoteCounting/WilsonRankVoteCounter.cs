using System.Collections.Generic;
using System.Linq;
using NetTally.VoteCounting.RankVoteCounting.Utility;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes
{
    public class WilsonRankVoteCounter : IRankVoteCounter2
    {
        public List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            var results = from vote in taskVotes
                          let wilsonScore = RankScoring.LowerWilsonScore(vote)
                          select new { vote, score = wilsonScore };

            var orderedResults = results.OrderByDescending(a => a.score.score)
                                        .ThenByDescending(a => a.score.count);

            int r = 1;

            List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)> resultList
                = new List<((int rank, double rankScore) ranking, KeyValuePair<VoteLineBlock, VoterStorage> vote)>();

            foreach (var res in orderedResults)
            {
                resultList.Add(((r++, res.score.score), res.vote));
            }

            return resultList;
        }
    }
}
