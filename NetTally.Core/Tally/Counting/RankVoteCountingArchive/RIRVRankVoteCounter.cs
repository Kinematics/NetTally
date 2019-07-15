using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NetTally.Utility;
using NetTally.VoteCounting.RankVoteCounting.Utility;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // Vote (string), collection of voters
    using SupportedVotes = Dictionary<string, HashSet<string>>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

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
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    class RIRVRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            RankResults winningChoices = new RankResults();

            // The groupVotes are used for getting the Wilson score
            var rankedVotes = GroupRankVotes.GroupByVoteAndRank(task);
            // The voterRankings are used for the runoff
            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
            // The full choices list is just to keep track of how many we have left.
            var allChoices = GroupRankVotes.GetAllChoices(voterRankings);

            for (int i = 1; i <= 9; i++)
            {
                RankResult winner = GetWinningVote(voterRankings, rankedVotes);

                if (winner == null || winner.Option == null)
                    break;

                winningChoices.Add(winner);
                allChoices.Remove(winner.Option);

                if (!allChoices.Any())
                    break;

                voterRankings = RemoveChoiceFromVotes(voterRankings, winner.Option);
                rankedVotes = RemoveChoiceFromRanks(rankedVotes, winner.Option);
            }

            return winningChoices;
        }

        /// <summary>
        /// Gets the winning vote.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="rankedVotes">The votes, ranked.</param>
        /// <returns></returns>
        private RankResult GetWinningVote(IEnumerable<VoterRankings> voterRankings, IEnumerable<RankGroupedVoters> rankedVotes)
        {
            string debug = "";

            GetTopTwoRatedOptions(rankedVotes, out string? option1, out string? option2, ref debug);

            string winner = GetOptionWithHigherPrefCount(voterRankings, option1, option2, ref debug) ?? string.Empty;

            return new RankResult(winner, debug);
        }

        /// <summary>
        /// Gets the top two rated options.
        /// </summary>
        /// <param name="rankedVotes">The group votes.</param>
        /// <param name="option1">The top rated option. Null if there aren't any options available.</param>
        /// <param name="option2">The second rated option.  Null if there is only one option available.</param>
        private static void GetTopTwoRatedOptions(IEnumerable<RankGroupedVoters> rankedVotes,
            out string? option1, out string? option2, ref string debug)
        {
            var scoredVotes = from vote in rankedVotes
                              select new { Vote = vote.VoteContent, Rank = RankScoring.LowerWilsonScore(vote.Ranks) };

            var orderedVotes = scoredVotes.OrderByDescending(a => a.Rank);

            var topTwo = orderedVotes.Take(2);

            var v1 = topTwo.FirstOrDefault();
            var v2 = topTwo.Skip(1).FirstOrDefault();

            option1 = v1?.Vote;
            option2 = v2?.Vote;

            StringBuilder sb = new StringBuilder();

            // Output: [Option1, Option2] [Score1, Score2] [PrefCount1, PrefCount2]
            sb.Append("RIR: [");
            if (v1 != null)
                sb.Append(v1.Vote);
            sb.Append(", ");
            if (v2 != null)
                sb.Append(v2.Vote);
            sb.Append("] [");
            if (v1 != null)
                sb.Append($"{v1.Rank:f5}");
            sb.Append(", ");
            if (v2 != null)
                sb.Append($"{v2.Rank:f5}");
            sb.Append("] ");

            debug = sb.ToString();
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
        private static string? GetOptionWithHigherPrefCount(IEnumerable<VoterRankings> voterRankings,
            string? option1, string? option2, ref string debug)
        {
            if (option1 == null)
                return null;
            if (string.IsNullOrEmpty(option2))
                return option1;

            int count1 = 0;
            int count2 = 0;

            foreach (var voter in voterRankings)
            {
                var rank1 = voter.RankedVotes.FirstOrDefault(a => Agnostic.StringComparer.Equals(a.Vote, option1!));
                var rank2 = voter.RankedVotes.FirstOrDefault(a => Agnostic.StringComparer.Equals(a.Vote, option2!));

                if (rank1 == null && rank2 == null)
                    continue;

                if (rank1 == null)
                {
                    count2++;
                    continue;
                }

                if (rank2 == null)
                {
                    count1++;
                    continue;
                }

                if (rank1.Rank > rank2.Rank)
                {
                    count2++;
                }
                else if (rank2.Rank > rank1.Rank)
                {
                    count1++;
                }
            }

            // Output: [Option1, Option2] [Score1, Score2] [PrefCount1, PrefCount2]
            debug += $"[{count1}, {count2}] ";

            // If count1==count2, we use the higher scored option, which
            // will necessarily be option1.  Therefore all ties will be
            // in favor of option1, and the only thing we need to check
            // for is if option2 wins explicitly.
            if (count2 > count1)
            {
                return option2;
            }

            return option1;
        }

        /// <summary>
        /// Removes the specified vote choice from the list of ranked votes.
        /// </summary>
        /// <param name="rankedVotes">The ranked votes.</param>
        /// <param name="choice">The choice being removed.</param>
        /// <returns></returns>
        private static IEnumerable<RankGroupedVoters> RemoveChoiceFromRanks(IEnumerable<RankGroupedVoters> rankedVotes, string choice)
        {
            var res = rankedVotes.Where(a => !Agnostic.StringComparer.Equals(a.VoteContent, choice));

            return res.ToList();
        }

        /// <summary>
        /// Removes a list of choices from voter rankings.
        /// These are the choices that have already won a rank spot.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="choice">The already chosen choices.</param>
        /// <returns>Returns the results as a list.</returns>
        private static IEnumerable<VoterRankings> RemoveChoiceFromVotes(IEnumerable<VoterRankings> voterRankings, string choice)
        {
            var res = from voter in voterRankings
                      select new VoterRankings
                      (
                          voter: voter.Voter,
                          rankedVotes: voter.RankedVotes
                              .Where(v => !Agnostic.StringComparer.Equals(v.Vote, choice))
                              .ToList()
                      );

            return res.ToList();
        }

    }
}
