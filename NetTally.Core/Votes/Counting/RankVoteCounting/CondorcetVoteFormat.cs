using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NetTally.Utility;
using NetTally.VoteCounting.RankVoteCounting.Utility;
using System.Text.RegularExpressions;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // Vote (string), collection of voters
    using SupportedVotes = Dictionary<string, HashSet<string>>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    /// <summary>
    /// Formats votes for http://democratix.dbai.tuwien.ac.at/
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    public class CondorcetVoteFormat : BaseVoteFormat
    {
        /// <summary>
        /// Implementation to generate input for Condorcet.Vote
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list where the first entry is the input for Condorcet.Vote.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            // The voterRankings are used for the runoff
            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
            // The full choices list is just to keep track of how many we have left.
            var allChoices = GroupRankVotes.GetAllChoices(voterRankings);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Warning: Candidate names are stripped of non-alphanumeric characters and reduced to 30 characters. If this results in multiple idendical candidates, Condorcet.Vote might not work.");
            sb.AppendLine("Copy the following into the \"Add Candidates\" field:\n");

            List<String> allTrunc = new List<string>();
            foreach(String s in allChoices)
            {
                String app = TruncateVote(s);
                allTrunc.Add(app);
                sb.Append(app);
                sb.Append(" ; ");
            }
            sb.Length -= 3;
            sb.AppendLine("\n");


            sb.AppendLine("\nCopy the following into the \"Add Vote(s)\" field:\n");

            foreach(VoterRankings vr in voterRankings)
            {
                HashSet<String> allTemp = new HashSet<string>(allTrunc);
                vr.RankedVotes.Sort((x, y) => x.Rank - y.Rank);
                RankedVote first = vr.RankedVotes.First();
                int r = first.Rank;
                String app = TruncateVote(first.Vote);
                sb.Append(app);
                vr.RankedVotes.RemoveAt(0);
                allTemp.Remove(app);

                foreach(RankedVote rv in vr.RankedVotes)
                {
                    if (rv.Rank > r)
                        sb.Append(" > ");
                    else
                        sb.Append(" = ");
                    r = rv.Rank;

                    app = TruncateVote(rv.Vote);
                    sb.Append(app);
                    allTemp.Remove(app);
                }
                if (allTemp.Count > 0)
                {
                    sb.Append(" > ");
                    foreach (String v in allTemp)
                    {
                        sb.Append(v);
                        sb.Append(" = ");
                    }
                    sb.Length -= 3;
                }
                sb.AppendLine("");
            }

            RankResults output = new RankResults();
            output.Add(new RankResult(sb.ToString(), "CondorcetVote Format Output"));
            return output;
        }

        private String TruncateVote(String Vote)
        {
            String tr = Regex.Replace(Vote, @"[^A-Za-z0-9]+", "");
            if (tr.Length > 30)
                tr = tr.Substring(0, 30);
            return tr;
        }
    }
}
