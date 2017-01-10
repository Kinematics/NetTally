﻿using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;
using NetTally.ViewModels;

namespace NetTally.Output
{
    public static class VoteInfo
    {
        /// <summary>
        /// Get the URL for the post made by the specified voter.
        /// </summary>
        /// <param name="voter">The voter to look up.</param>
        /// <param name="voteType">The type of vote being checked.</param>
        /// <returns>Returns the permalink URL for the voter.  Returns an empty string if not found.</returns>
        public static string GetVoterUrl(string voter, VoteType voteType)
        {
            Dictionary<string, string> voters = VoteCounter.Instance.GetVotersCollection(voteType);

            string voteID;
            if (voters.TryGetValue(voter, out voteID))
                return VoteCounter.Instance.Quest.ForumAdapter.GetPermalinkForId(voteID);

            return string.Empty;
        }
        
        /// <summary>
        /// Property to get the total number of ranked voters in the tally.
        /// </summary>
        public static int RankedVoterCount => VoteCounter.Instance.GetVotersCollection(VoteType.Rank).Count;

        /// <summary>
        /// Property to get the total number of normal voters in the tally.
        /// </summary>
        public static int NormalVoterCount => VoteCounter.Instance.GetVotersCollection(VoteType.Vote).Count(voter => StringUtility.IsPlanName(voter.Key) == false);

        /// <summary>
        /// Calculate the number of non-plan voters in the provided vote object.
        /// </summary>
        /// <param name="vote">The vote containing a list of voters.</param>
        /// <returns>Returns how many of the voters in this vote were users (rather than plans).</returns>
        public static int CountVote(KeyValuePair<string, HashSet<string>> vote) => vote.Value?.Count(vc => VoteCounter.Instance.PlanNames.Contains(vc) == false) ?? 0;

        /// <summary>
        /// Get a list of voters, ordered alphabetically, except the first voter,
        /// who is the 'earliest' of the provided voters (ie: the first person to
        /// vote for this vote or plan).
        /// </summary>
        /// <param name="voters">A set of voters.</param>
        /// <returns>Returns an organized, sorted list.</returns>
        public static IEnumerable<string> GetOrderedVoterList(HashSet<string> voters)
        {
            if (voters == null || voters.Count == 0)
                return new List<string>();

            var voterList = new List<string> { GetFirstVoter(voters) };
            var otherVoters = voters.Except(voterList);

            var orderedVoters = voterList.Concat(otherVoters.OrderBy(v => v));
            return orderedVoters;
        }

        /// <summary>
        /// Determine which of the provided voters was the 'first'.  That is,
        /// the earliest voter with an actual vote, rather than a reference to
        /// a future vote.
        /// </summary>
        /// <param name="voters">A set of voters to check.</param>
        /// <returns>Returns which one of them is considered the first real poster.</returns>
        public static string GetFirstVoter(HashSet<string> voters)
        {
            var planVoters = voters.Where(v => VoteCounter.Instance.PlanNames.Contains(v));
            var votersCollection = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);

            if (planVoters.Any())
            {
                return planVoters.MinObject(v => votersCollection[v]);
            }

            var nonFutureVoters = voters.Except(VoteCounter.Instance.FutureReferences.Select(p => p.Author));

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
        /// Group votes by task.
        /// </summary>
        /// <param name="allVotes">A list of all votes.</param>
        /// <returns>Returns all the votes, grouped by task (case-insensitive).</returns>
        public static IOrderedEnumerable<IGrouping<string, KeyValuePair<string, HashSet<string>>>> GroupVotesByTask(Dictionary<string, HashSet<string>> allVotes)
        {
            var grouped = allVotes.GroupBy(v => VoteString.GetVoteTask(StringUtility.GetFirstLine(v.Key)), StringComparer.OrdinalIgnoreCase).OrderBy(v => v.Key);
            if(VoteCounter.Instance.OrderedTaskList != null)
            {

                grouped = grouped.OrderBy(v => VoteCounter.Instance.OrderedTaskList.IndexOf(v.Key));
            }
            
            return grouped;
        }

        /// <summary>
        /// Given a group of votes (grouped by task), create and return
        /// a list of VoteNodes that collapse together votes that are 
        /// sub-votes of each other.
        /// </summary>
        /// <param name="taskGroup">A set of votes with the same task value.</param>
        /// <returns>Returns a list of VoteNodes that collapse similar votes.</returns>
        public static IEnumerable<VoteNode> GetVoteNodes(IGrouping<string, KeyValuePair<string, HashSet<string>>> taskGroup)
        {
            var groupByFirstLine = taskGroup.GroupBy(v => StringUtility.GetFirstLine(v.Key), StringUtility.AgnosticStringComparer);

            List<VoteNode> nodeList = new List<VoteNode>();
            VoteNode parent;

            foreach (var voteGroup in groupByFirstLine)
            {
                parent = null;

                if (voteGroup.Count() == 1)
                {
                    string planname = VoteString.GetPlanName(voteGroup.Key);
                    if (planname != null && VoteCounter.Instance.HasPlan(planname))
                    {
                        var vote = voteGroup.First();
                        parent = new VoteNode(vote.Key, vote.Value);
                        nodeList.Add(parent);
                        continue;
                    }
                }

                foreach (var vote in voteGroup)
                {
                    var lines = StringUtility.GetStringLines(vote.Key);

                    if (parent == null)
                    {
                        var voters = lines.Count == 1 ? vote.Value : null;
                        parent = new VoteNode(lines[0], voters);
                    }

                    if (lines.Count == 1)
                    {
                        parent.AddVoters(vote.Value);
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

    }
}
