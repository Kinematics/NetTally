using System.Diagnostics;
using System.Linq;
using NetTally.Extensions;
using NetTally.VoteCounting.RankVoteCounting.Utility;
using NetTally.VoteCounting.RankVotes.Reference;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVotes
{
    /// <summary>
    /// Implement ranking votes using the Baldwin method.
    /// It's an instant runoff that uses Wilson scoring to determine
    /// which vote to remove each round.
    /// </summary>
    public class BaldwinRankVoteCounter : InstantRunoffBase
    {
        protected override bool leastPreferredChecksFullVotes { get; } = true;

        /// <summary>
        /// Gets the least preferred choice.
        /// With the Baldwin method, this is the vote with the lowest Wilson Score.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <returns>Returns the vote string for the least preferred vote.</returns>
        protected override VoteLineBlock GetLeastPreferredChoice(VoteStorage votes)
        {
            var rankedVotes = from vote in votes
                              select new { rating = (vote, RankScoring.LowerWilsonRankingScore(vote)) };

            var worstVote = rankedVotes.MinObject(a => a.rating.Item2);

            Debug.Write($"({worstVote.rating.Item2:f5})");

            return worstVote.rating.vote.Key;
        }
    }
}

