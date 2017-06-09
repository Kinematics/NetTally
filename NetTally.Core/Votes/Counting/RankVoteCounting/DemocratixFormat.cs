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
    /// Formats votes for http://democratix.dbai.tuwien.ac.at/
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    public class DemocratixFormat : BaseVoteFormat
    {
        /// <summary>
        /// Implementation to generate input for Democratix
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list where the first entry is the input for Democratix.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            // The voterRankings are used for the runoff
            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
            // The full choices list is just to keep track of how many we have left.
            var allChoices = GroupRankVotes.GetAllChoices(voterRankings);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Warning: At this time (2017-06-09) Democratix does not support equality. If equal votes exist, they are symboliced by \"=\" instead of \",\".");
            sb.AppendLine("Copy the following into the \"Profile\" field:\n");

            sb.AppendLine(allChoices.Count.ToString());

            Dictionary<String, int> voteNum = new Dictionary<string, int>();
            List<int> allNum = new List<int>(Enumerable.Range(1, allChoices.Count));
            for(int k = 1; k <= allChoices.Count; k++)
            {
                String app = allChoices.ElementAt(k-1);
                voteNum.Add(app, k);
                sb.Append(k);
                sb.Append(",");
                sb.AppendLine(app);
            }

            Dictionary<String, int> collect = new Dictionary<string, int>();
            foreach (VoterRankings vr in voterRankings)
            {
                StringBuilder temp = new StringBuilder();
                HashSet<int> allTemp = new HashSet<int>(allNum);
                vr.RankedVotes.Sort((x, y) => x.Rank - y.Rank);
                int app;
                int r = 0;

                foreach (RankedVote rv in vr.RankedVotes)
                {
                    if (!voteNum.TryGetValue(rv.Vote, out app))
                        continue;

                    if (rv.Rank > r)
                        temp.Append(",");
                    else
                        temp.Append("=");
                    r = rv.Rank;
                    temp.Append(app);
                    allTemp.Remove(app);
                }
                if (allTemp.Count > 0)
                {
                    temp.Append(",");
                    foreach (int v in allTemp)
                    {
                        temp.Append(v);
                        temp.Append("=");
                    }
                    temp.Length--;
                }
                String s = temp.ToString();
                if(collect.TryGetValue(s, out app))
                    collect[s] = app + 1;
                else
                    collect.Add(s, 1);
            }

            int all = 0;
            StringBuilder votes = new StringBuilder();
            foreach(KeyValuePair<String, int> kv in collect)
            {
                votes.Append(kv.Value);
                votes.Append(kv.Key);
                votes.AppendLine("");
                all += kv.Value;
            }
            sb.Append(all);
            sb.Append(",");
            sb.Append(all);
            sb.Append(",");
            sb.Append(collect.Count);
            sb.AppendLine("");
            sb.Append(votes);

            RankResults output = new RankResults();
            output.Add(new RankResult(sb.ToString(), "CondorcetVote Format Output"));
            return output;
        }
    }
}
