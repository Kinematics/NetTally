using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.VoteCounting
{
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    public class RankGroupedVoters
    {
        public string VoteContent { get; set; }
        public IEnumerable<RankedVoters> Ranks { get; set; }
    }

    public class RankedVoters
    {
        public int Rank { get; set; }
        public IEnumerable<string> Voters { get; set; }
    }

    public class RankedVote
    {
        public int Rank { get; set; }
        public string Vote { get; set; }
    }

    public class VoterRankings
    {
        public string Voter { get; set; }
        public List<RankedVote> RankedVotes { get; set; }
    }

    public static class GroupRankVotes
    {
        public static IEnumerable<RankGroupedVoters> GroupByVoteAndRank(GroupedVotesByTask task)
        {
            var res = from vote in task
                      let content = VoteString.GetVoteContent(vote.Key)
                      group vote by content into votes
                      select new RankGroupedVoters
                      {
                          VoteContent = votes.Key,
                          Ranks = from v in votes
                                  group v by VoteString.GetVoteMarker(v.Key) into vr
                                  select new RankedVoters { Rank = int.Parse(vr.Key), Voters = vr.SelectMany(a => a.Value) }
                      };

            return res;
        }

        public static IEnumerable<RankGroupedVoters> GroupByVoteAndRank(IEnumerable<VoterRankings> rankings)
        {
            var q = from v in rankings
                    from w in v.RankedVotes
                    group w by w.Vote into wv
                    select new RankGroupedVoters
                    {
                        VoteContent = wv.Key,
                        Ranks = from v2 in rankings
                                let voter = v2.Voter
                                from r in v2.RankedVotes
                                where r.Vote == wv.Key
                                group voter by r.Rank into vs2
                                select new RankedVoters
                                {
                                    Rank = vs2.Key,
                                    Voters = vs2.Select(g2 => g2)
                                }
                    };

            return q;
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
                                             Rank = RankAsInt(VoteString.GetVoteMarker(v.Key)),
                                             Vote = VoteString.GetVoteContent(v.Key)
                                         }).ToList()
                      };

            return res;

        }

        private static int RankAsInt(string rank)
        {
            if (string.IsNullOrEmpty(rank))
                throw new ArgumentNullException(nameof(rank));

            int rankAsInt = int.Parse(rank);

            return rankAsInt;
        }
    }
}
