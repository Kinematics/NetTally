using System.Collections.Generic;
using System.Linq;
using NetTally.VoteCounting.RankVoteCounting.Utility;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    /// <summary>
    /// Wilson vote scoring uses the lower bounds of a Bournoulli analysis of the vote
    /// rankings to get the 95% minimum confidence interval.
    /// This means that a voted item with only a few supporters will have a low score
    /// due to a high error margin, while a score with more supporters will have a
    /// higher relative confidence rating.
    /// This improves on the Borda scoring, which has no means of compensating for
    /// votes that are ranked by less than 100% of the voter base.
    /// </summary>
    public class WilsonRankVoteCounter : IRankVoteCounter2
    {
        public List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            var results = from vote in taskVotes
                          let wilsonScore = RankScoring.LowerWilsonRankingScore(vote)
                          select new { vote, score = wilsonScore };

            var orderedResults = results.OrderByDescending(a => a.score.score)
                                        .ThenByDescending(a => a.score.count);

            int r = 1;

            List<((int rank, double rankScore) ranking, VoteStorageEntry vote)> resultList
                = new List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>();

            foreach (var res in orderedResults)
            {
                resultList.Add(((r++, res.score.score), res.vote));
            }

            return resultList;
        }
    }
}
