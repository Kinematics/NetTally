﻿using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.VoteCounting.RankVoteCounting.Utility
{
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    class RankedVote
    {
        public string Vote { get; }
        public int Rank { get; }

        public RankedVote(string vote, int rank)
        {
            Vote = vote;
            Rank = rank;
        }
    }

    class RatedVote
    {
        public string Vote { get; }
        public double Rating { get; }

        public RatedVote(string vote, double rating)
        {
            Vote = vote;
            Rating = rating;
        }
    }

    class VoterRankings
    {
        public string Voter { get; }
        public List<RankedVote> RankedVotes { get; }

        public VoterRankings(string voter, List<RankedVote> rankedVotes)
        {
            Voter = voter;
            RankedVotes = rankedVotes;
        }
    }

    class RankedVoters
    {
        public int Rank { get; }
        public IEnumerable<string> Voters { get; }

        public RankedVoters(int rank, IEnumerable<string> voters)
        {
            Rank = rank;
            Voters = voters;
        }
    }

    class RankGroupedVoters
    {
        public string VoteContent { get; set; }
        public IEnumerable<RankedVoters> Ranks { get; set; }

        public RankGroupedVoters(string voteContent, IEnumerable<RankedVoters> ranks)
        {
            VoteContent = voteContent;
            Ranks = ranks;
        }
    }

    class CountedChoice
    {
        public string Choice { get; }
        public int Count { get; }

        public CountedChoice(string choice, int count)
        {
            Choice = choice;
            Count = count;
        }

        public override string ToString() => $"{Choice}: {Count}";
    }


    /// <summary>
    /// Static class to take known input lists and convert them to an
    /// enumerable list of one of the above types.
    /// </summary>
    static class GroupRankVotes
    {
        public static IEnumerable<RankGroupedVoters> GroupByVoteAndRank(GroupedVotesByTask task)
        {

            var res = task.GroupBy(vote => VoteString.GetVoteContent(vote.Key), Agnostic.StringComparer)
                .Select(votes => new RankGroupedVoters (
                    voteContent: votes.Key,
                    ranks:  from v in votes
                            group v by VoteString.GetVoteMarker(v.Key) into vr
                            select new RankedVoters ( rank: int.Parse(vr.Key), voters: vr.SelectMany(a => a.Value) )
                ));

            return res;
        }

        public static IEnumerable<RankGroupedVoters> GroupByVoteAndRank(IEnumerable<VoterRankings> rankings)
        {
            var r1 = rankings.SelectMany(va => va.RankedVotes).GroupBy(vb => vb.Vote, Agnostic.StringComparer)
                .Select(vc => new RankGroupedVoters
                (
                    voteContent: vc.Key,
                    ranks:  from v2 in rankings
                            let voter = v2.Voter
                            from r in v2.RankedVotes
                            where Agnostic.StringComparer.Equals(r.Vote, vc.Key)
                            group voter by r.Rank into vs2
                            select new RankedVoters
                            (
                                rank: vs2.Key,
                                voters: vs2.Select(g2 => g2)
                            )
                ));

            return r1;
        }

        public static IEnumerable<VoterRankings> GroupByVoterAndRank(GroupedVotesByTask task)
        {
            var res = from vote in task
                      from voter in vote.Value
                      group vote by voter into voters
                      select new VoterRankings
                      (
                          voter: voters.Key,
                          rankedVotes: (from v in voters
                                         select new RankedVote(VoteString.GetVoteContent(v.Key), int.Parse(VoteString.GetVoteMarker(v.Key)))
                                         ).ToList()
                      );

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

    class DistanceData
    {
        public int[,] Paths { get; }

        public DistanceData(int rows, int cols)
        {
            Paths = new int[rows, cols];
        }
    }

}
