using System.Collections.Generic;
using System.Linq;
using NetTally.VoteCounting.RankVotes.Reference;
using NetTally.Votes;
using NetTally.Types.Enums;

namespace NetTally.VoteCounting.RankVotes
{
    using VoteStorageEntry = KeyValuePair<VoteLineBlock, VoterStorage>;

    /// <summary>
    /// Rated Instant Runoff voting scores all vote options, taking the top two,
    /// and does an instant runoff between them.
    /// This uses the Wilson score for scoring votes.
    /// The scoring aspect allows relative preference to be taken into account,
    /// but relative preference is ignored for the runoff phase, which merely
    /// checks for which of A or B is most often preferred over the other.
    /// This avoids the flaws of standard instant runoff voting by incorporating
    /// score ratings into the evaluation.
    /// </summary>
    public class RatedInstantRunoff : IRankVoteCounter2
    {
        public List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>
            CountVotesForTask(VoteStorage taskVotes)
        {
            int r = 1;

            List<((int rank, double rankScore) ranking, VoteStorageEntry vote)> resultList
                = new List<((int rank, double rankScore) ranking, VoteStorageEntry vote)>();

            var workingVotes = new VoteStorage(taskVotes);

            while (workingVotes.Count > 0)
            {
                var (vote, score) = GetWinningVote(workingVotes);

                resultList.Add(((r++, score), vote));

                workingVotes.Remove(vote.Key);
            }

            return resultList;
        }

        /// <summary>
        /// Gets the winning vote.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="rankedVotes">The votes, ranked.</param>
        /// <returns></returns>
        private (VoteStorageEntry vote, double score)
            GetWinningVote(VoteStorage votes)
        {
            var options = GetTopTwoRatedOptions(votes);

            if (options.Count == 1)
            {
                return options[0];
            }

            VoteStorageEntry winner =
                GetOptionWithHigherPrefCount(options[0].option, options[1].option);

            return (winner, winner.Key == options[0].option.Key ? options[0].score : options[1].score);
        }

        /// <summary>
        /// Gets the top two rated options.
        /// </summary>
        /// <param name="rankedVotes">The group votes.</param>
        /// <param name="option1">The top rated option. Null if there aren't any options available.</param>
        /// <param name="option2">The second rated option.  Null if there is only one option available.</param>
        private List<(VoteStorageEntry option, double score)>
            GetTopTwoRatedOptions(VoteStorage votes)
        {
            var scoredVotes = from vote in votes
                              select new { vote, score = RankingCalculations.LowerWilsonRankingScore(vote) };

            var orderedVotes = scoredVotes.OrderByDescending(a => a.score);

            var topTwo = orderedVotes.Take(2).Select(o => (o.vote, o.score.score)).ToList();

            return topTwo;
        }

        /// <summary>
        /// Gets the option with higher preference count.
        /// This is the runoff portion of the vote evaluation.  Whichever option has more
        /// people that prefer it over the other, wins.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.  This allows seeing which option each voter preferred.</param>
        /// <param name="option1">The first option up for consideration.</param>
        /// <param name="option2">The second option up for consideration.</param>
        /// <returns>Returns the winning option.</returns>
        private VoteStorageEntry GetOptionWithHigherPrefCount(
            VoteStorageEntry option1,
            VoteStorageEntry option2)
        {
            var voters1 = option1.Value;
            var voters2 = option2.Value;

            var allVoters = voters1.Keys
                .Concat(voters2.Keys)
                .Distinct()
                .Where(v => v.AuthorType == IdentityType.User)
                .ToList();

            int count1 = 0;
            int count2 = 0;

            foreach (var voter in allVoters)
            {
                if (!voters1.TryGetValue(voter, out var support1))
                {
                    if (voters2.ContainsKey(voter))
                        count2++;
                    continue;
                }

                if (!voters2.TryGetValue(voter, out var support2))
                {
                    if (voters1.ContainsKey(voter))
                        count1++;
                    continue;
                }

                if (support1.MarkerValue < support2.MarkerValue)
                {
                    count1++;
                }
                else if (support2.MarkerValue < support1.MarkerValue)
                {
                    count2++;
                }
            }

            // If count1==count2, we use the higher scored option, which
            // will necessarily be option1.  Therefore all ties will be
            // in favor of option1, and the only thing we need to check
            // for is if option2 wins explicitly.
            return count2 > count1 ? option2 : option1;
        }
    }
}
