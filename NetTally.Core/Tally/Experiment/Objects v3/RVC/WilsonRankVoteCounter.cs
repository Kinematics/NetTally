using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTally.VoteCounting;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.Experiment3
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
