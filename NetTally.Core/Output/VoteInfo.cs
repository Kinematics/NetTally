using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Extensions;
using NetTally.Forums;
using NetTally.Utility;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Output
{
    /// <summary>
    /// Class to handle calculating and extracting vote information.
    /// </summary>
    public class VoteInfo
    {
        readonly IVoteCounter voteCounter;
        readonly IForumAdapter forumAdapter;

        public VoteInfo(IVoteCounter counter, ForumAdapterFactory forumAdapterFactory)
        {
            voteCounter = counter;
            IQuest quest = voteCounter.Quest ?? throw new InvalidOperationException("Vote counter's quest is null.");

            forumAdapter = forumAdapterFactory.CreateForumAdapter(quest.ForumType, quest.ThreadUri!);
        }

        #region deprecated
        /// <summary>
        /// Get the URL for the post made by the specified voter.
        /// </summary>
        /// <param name="voter">The voter to look up.</param>
        /// <param name="voteType">The type of vote being checked.</param>
        /// <returns>Returns the permalink URL for the voter.  Returns an empty string if not found.</returns>
        public string GetVoterUrl(string voter, VoteType voteType)
        {
            Dictionary<string, string> voters = new Dictionary<string, string>(); // voteCounter.GetVotersCollection(voteType);

            if (voters.TryGetValue(voter, out string voteID))
                return forumAdapter.GetPermalinkForId(voteID) ?? string.Empty;

            return string.Empty;
        }

        /// <summary>
        /// Determine which of the provided voters was the 'first'.  That is,
        /// the earliest voter with an actual vote, rather than a reference to
        /// a future vote.
        /// </summary>
        /// <param name="voters">A set of voters to check.</param>
        /// <returns>Returns which one of them is considered the first real poster.</returns>
        public string? GetFirstVoter(HashSet<string> voters)
        {
            var planVoters = voters.Where(v => voteCounter.HasPlan(v));
            Dictionary<string, string> votersCollection = new Dictionary<string, string>(); // voteCounter.GetVotersCollection(voteType);

            if (planVoters.Any())
            {
                return planVoters.MinObject(v => votersCollection[v]);
            }

            var nonFutureVoters = voters.Except(voteCounter.FutureReferences.Select(p => p.Origin.Author));

            if (nonFutureVoters.Any())
            {
                return nonFutureVoters.MinObject(v => votersCollection[v]);
            }

            if (voters.Any())
            {
                return voters.MinObject(v => votersCollection[v]);
            }

            return null;
        }

        /// <summary>
        /// Given a group of votes (grouped by task), create and return
        /// a list of VoteNodes that collapse together votes that are 
        /// sub-votes of each other.
        /// </summary>
        /// <param name="taskGroup">A set of votes with the same task value.</param>
        /// <returns>Returns a list of VoteNodes that collapse similar votes.</returns>
        public IEnumerable<VoteNode> GetVoteNodes(IGrouping<string, KeyValuePair<string, HashSet<string>>> taskGroup)
        {
            var groupByFirstLine = taskGroup.GroupBy(v => v.Key.GetFirstLine(), Agnostic.StringComparer);

            List<VoteNode> nodeList = new List<VoteNode>();
            VoteNode? parent;

            foreach (var voteGroup in groupByFirstLine)
            {
                parent = null;

                if (voteGroup.Count() == 1)
                {
                    string? planname = VoteString.GetPlanName(voteGroup.Key);
                    if (planname != null && voteCounter.HasPlan(planname))
                    {
                        var vote = voteGroup.First();
                        parent = new VoteNode(vote.Key, vote.Value, this);
                        nodeList.Add(parent);
                        continue;
                    }
                }

                foreach (var vote in voteGroup)
                {
                    var lines = vote.Key.GetStringLines();

                    if (parent == null)
                    {
                        var voters = lines.Count == 1 ? vote.Value : null;
                        parent = new VoteNode(lines[0], voters, this);
                    }

                    if (lines.Count == 1)
                    {
                        parent.AddVoters(vote.Value);
                    }
                    else if (lines.Skip(1).All(a => VoteString.GetVotePrefix(a).Length == 1))
                    {
                        foreach (var line in lines.Skip(1))
                        {
                            parent.AddChild(line, vote.Value);
                        }
                    }
                    else if (lines.Count == 2 && !string.IsNullOrEmpty(VoteString.GetVotePrefix(lines[1])))
                    {
                        parent.AddChild(lines[1], vote.Value);
                    }
                    else
                    {
                        parent.AddChild(vote.Key, vote.Value);
                    }
                }

                if (parent != null)
                {
                    nodeList.Add(parent);
                }
            }

            return nodeList.OrderByDescending(v => v.VoterCount);
        }


        #endregion
    }
}
