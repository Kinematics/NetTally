using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;

namespace NetTally.VoteCounting
{
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    public class RankedVote
    {
        public string Vote { get; set; }
        public int Rank { get; set; }
    }

    public class RatedVote
    {
        public string Vote { get; set; }
        public double Rating { get; set; }
    }

    public class VoterRankings
    {
        public string Voter { get; set; }
        public List<RankedVote> RankedVotes { get; set; }
    }

    public class RankedVoters
    {
        public int Rank { get; set; }
        public IEnumerable<string> Voters { get; set; }
    }

    public class RankGroupedVoters
    {
        public string VoteContent { get; set; }
        public IEnumerable<RankedVoters> Ranks { get; set; }
    }

    /// <summary>
    /// Static class to take known input lists and convert them to an
    /// enumerable list of one of the above types.
    /// </summary>
    public static class GroupRankVotes
    {
        public static IEnumerable<RankGroupedVoters> GroupByVoteAndRank(GroupedVotesByTask task)
        {

            var res = task.GroupBy(vote => VoteString.GetVoteContent(vote.Key), Agnostic.StringComparer)
                .Select(votes => new RankGroupedVoters {
                    VoteContent = votes.Key,
                    Ranks = from v in votes
                            group v by VoteString.GetVoteMarker(v.Key) into vr
                            select new RankedVoters { Rank = int.Parse(vr.Key), Voters = vr.SelectMany(a => a.Value) }
                });

            return res;
        }

        public static IEnumerable<RankGroupedVoters> GroupByVoteAndRank(IEnumerable<VoterRankings> rankings)
        {
            var r1 = rankings.SelectMany(va => va.RankedVotes).GroupBy(vb => vb.Vote, Agnostic.StringComparer)
                .Select(vc => new RankGroupedVoters
                {
                    VoteContent = vc.Key,
                    Ranks = from v2 in rankings
                            let voter = v2.Voter
                            from r in v2.RankedVotes
                            where Agnostic.StringComparer.Equals(r.Vote, vc.Key)
                            group voter by r.Rank into vs2
                            select new RankedVoters
                            {
                                Rank = vs2.Key,
                                Voters = vs2.Select(g2 => g2)
                            }
                });

            return r1;
        }

        public static IEnumerable<VoterRankings> GroupByVoterAndRank(GroupedVotesByTask task)
        {
            var res = from vote in task
                      from voter in vote.Value
                      group vote by voter into voters
                      select new VoterRankings
                      {
                          Voter = voters.Key,
                          RankedVotes = (from v in voters
                                         select new RankedVote
                                         {
                                             Rank = int.Parse(VoteString.GetVoteMarker(v.Key)),
                                             Vote = VoteString.GetVoteContent(v.Key)
                                         }).ToList()
                      };

            return res;

        }

        /// <summary>
        /// Gets all choices from all user votes.
        /// </summary>
        /// <param name="rankings">The collection of user votes.</param>
        /// <returns>Returns a list of all the choices in the task.</returns>
        public static List<string> GetAllChoices(IEnumerable<VoterRankings> rankings)
        {
            var res = rankings.SelectMany(r => r.RankedVotes).Select(r => r.Vote).Distinct(Agnostic.StringComparer);

            return res.ToList();
        }

        /// <summary>
        /// Gets all choices from all user votes.
        /// </summary>
        /// <param name="task">The collection of user votes.</param>
        /// <returns>Returns a list of all the choices in the task.</returns>
        public static List<string> GetAllChoices(GroupedVotesByTask task)
        {
            var res = task.GroupBy(vote => VoteString.GetVoteContent(vote.Key), Agnostic.StringComparer).Select(vg => vg.Key);

            return res.ToList();
        }

        /// <summary>
        /// Gets an indexer lookup for the list of choices, so it doesn't have to do
        /// sequential lookups each time..
        /// </summary>
        /// <param name="listOfChoices">The list of choices.</param>
        /// <returns>Returns a dictionary of choices vs list index.</returns>
        public static Dictionary<string, int> GetChoicesIndexes(List<string> listOfChoices)
        {
            Dictionary<string, int> choiceIndexes = new Dictionary<string, int>(Agnostic.StringComparer);
            var distinctChoices = listOfChoices.Distinct(Agnostic.StringComparer);

            int index = 0;
            foreach (var choice in distinctChoices)
            {
                choiceIndexes[choice] = index++;
            }

            return choiceIndexes;
        }

    }

    public class DistanceData
    {
        public int[,] Paths { get; set; }
    }

}
