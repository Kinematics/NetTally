using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Experiment3;
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




        /// <summary>
        /// Gets the line break text from the quest's forum adapter, since some
        /// can show hard rules, and some need to just use manual text.
        /// </summary>
        public string LineBreak => forumAdapter.LineBreak;
        /// <summary>
        /// Get the double line break.  There are no alternate versions right now.
        /// </summary>
        public string DoubleLineBreak => "<==========================================================>";

        /// <summary>
        /// Get a permalink for the vote post by the specified voter.
        /// </summary>
        /// <param name="voter">The voter that we need a permalink for.</param>
        /// <returns>Returns the permalink string, and whether the voter was a plan name.</returns>
        public (string permalink, bool plan) GetVoterPostPermalink(string voter)
        {
            string permalink = string.Empty;
            bool plan = false;

            int postID = voteCounter.GetVoterReferencePostId(voter);

            if (postID == 0)
            {
                postID = voteCounter.GetPlanReferencePostId(voter);

                if (postID != 0)
                    plan = true;
            }

            if (postID != 0)
            {
                permalink = forumAdapter.GetPermalinkForId(postID.ToString()) ?? string.Empty;
            }

            return (permalink, plan);
        }




        /// <summary>
        /// Gets an ordered version of the provided voters.
        /// The first voter was the first voter to support the given plan, and
        /// the rest of the voters are alphabatized.
        /// </summary>
        /// <param name="voters">The voters being ordered.</param>
        /// <returns>Returns an ordered list of the voters.</returns>
        public List<KeyValuePair<string, VoteLineBlock>> GetOrderedStandardVoterList(Dictionary<string, VoteLineBlock> voters)
        {
            var voterList = new List<KeyValuePair<string, VoteLineBlock>>();

            if (voters == null || voters.Count == 0)
            {
                return voterList;
            }

            if (voters.Count == 1)
            {
                voterList.AddRange(voters);
                return voterList;
            }

            voterList.AddRange(voters.OrderBy(v => v.Key));

            var firstVoter = GetFirstVoter(voters);

            voterList.Remove(firstVoter);
            voterList.Insert(0, firstVoter);

            return voterList;
        }

        /// <summary>
        /// Get the first voter from the provided list of voters.
        /// </summary>
        /// <param name="voters"></param>
        /// <returns></returns>
        private KeyValuePair<string, VoteLineBlock> GetFirstVoter(Dictionary<string, VoteLineBlock> voters)
        {
            var planVoters = voters.Where(v => voteCounter.HasPlan(v.Key));

            if (planVoters.Any())
            {
                return planVoters.Select(p => new { vote = p, id = voteCounter.GetPlanReferencePostId(p.Key) }).MinObject(a => a.id).vote;
            }

            return voters.Select(p => new { vote = p, id = voteCounter.GetVoterReferencePostId(p.Key) }).MinObject(a => a.id).vote;
        }


        public List<KeyValuePair<string, VoteLineBlock>> GetOrderedRankedVoterList(Dictionary<string, VoteLineBlock> voters)
        {
            var result = new List<KeyValuePair<string, VoteLineBlock>>();

            var ranksOnly = voters.Where(v => v.Value.MarkerType == MarkerType.Rank).OrderBy(v => v.Value.MarkerValue).ThenBy(v => v.Key);
            var others = voters.Where(v => v.Value.MarkerType != MarkerType.Rank).OrderBy(v => v.Key);

            result.AddRange(ranksOnly);
            result.AddRange(others);

            return result;
        }



        private bool IsPlan(string name)
        {
            return name[0] == Strings.PlanNameMarkerChar;
        }

        /// <summary>
        /// Get the full supporting count for the given vote among the voters in the support section.
        /// This is calculated for the votes of <seealso cref="MarkerType.Vote"/>.
        /// </summary>
        /// <param name="vote">A vote and its associated supporters.</param>
        /// <returns>Returns a count</returns>
        public int GetVoteVoterCount(KeyValuePair<VoteLineBlock, Dictionary<string, VoteLineBlock>> vote)
        {
            var nonPlanVoters = vote.Value.Where(v => !IsPlan(v.Key));
            var groupByMarker = nonPlanVoters.GroupBy(v => v.Value.MarkerType);

            int count = 0;

            // Approval and Score votes contribute if their value is greater than 50.
            foreach (var group in groupByMarker)
            {
                count += group.Key switch
                {
                    MarkerType.Vote => group.Count(),
                    MarkerType.Approval => group.Count(v => v.Value.MarkerValue > 50),
                    MarkerType.Score => group.Count(v => v.Value.MarkerValue > 50),
                    _ => 0
                };
            }

            return count;
        }

        public (int simpleScore, double limitScore) GetVoteScoreResult(KeyValuePair<VoteLineBlock, Dictionary<string, VoteLineBlock>> vote)
        {
            var nonPlanVoters = vote.Value.Where(v => !IsPlan(v.Key));
            var groupByMarker = nonPlanVoters.GroupBy(v => v.Value.MarkerType);

            int count = 0;
            int accum = 0;

            // TODO: Do a statistical margin of error calculation here.

            // Approval and Score votes contribute if their value is greater than 50.
            foreach (var group in groupByMarker)
            {
                accum += group.Key switch
                {
                    MarkerType.Vote => group.Sum(s => s.Value.MarkerValue),
                    MarkerType.Approval => group.Sum(s => s.Value.MarkerValue),
                    MarkerType.Score => group.Sum(s => s.Value.MarkerValue),
                    _ => 0
                };
            }

            foreach (var group in groupByMarker)
            {
                count += group.Key switch
                {
                    MarkerType.Vote => group.Count(),
                    MarkerType.Approval => group.Count(),
                    MarkerType.Score => group.Count(),
                    _ => 0
                };
            }

            if (count == 0)
                return (0, 0);

            double limitScore = (double)accum / count;
            int simpleScore = (int)limitScore;

            return (simpleScore, limitScore);
        }



        public (int positive, int negative) GetVoteApprovalResult(KeyValuePair<VoteLineBlock, Dictionary<string, VoteLineBlock>> vote)
        {
            var nonPlanVoters = vote.Value.Where(v => !IsPlan(v.Key));
            var groupByMarker = nonPlanVoters.GroupBy(v => v.Value.MarkerType);

            int positive = 0;
            int negative = 0;

            foreach (var group in groupByMarker)
            {
                var marker = group.Key;

                if (marker == MarkerType.Approval)
                {
                    positive += group.Count(v => v.Value.MarkerValue > 50);
                    negative += group.Count(v => v.Value.MarkerValue <= 50);
                }
                else if (marker == MarkerType.Score)
                {
                    positive += group.Count(v => v.Value.MarkerValue > 50);
                    negative += group.Count(v => v.Value.MarkerValue <= 50);
                }
                else if (marker == MarkerType.Vote)
                {
                    positive += group.Count(v => v.Value.MarkerValue > 50);
                }
            }

            return (positive, negative);
        }



        public int GetStandardVotersCount(KeyValuePair<VoteLineBlock, Dictionary<string, VoteLineBlock>> vote)
        {
            var nonPlanVoters = vote.Value.Where(v => !IsPlan(v.Key) &&
                    (v.Value.MarkerType == MarkerType.Vote || v.Value.MarkerType == MarkerType.Score || v.Value.MarkerType == MarkerType.Approval));

            return nonPlanVoters.Count();
        }

        public int GetAllVotersCount(KeyValuePair<VoteLineBlock, Dictionary<string, VoteLineBlock>> vote)
        {
            var nonPlanVoters = vote.Value.Where(v => !IsPlan(v.Key));

            return nonPlanVoters.Count();
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
            Dictionary<string, string> voters = voteCounter.GetVotersCollection(voteType);

            if (voters.TryGetValue(voter, out string voteID))
                return forumAdapter.GetPermalinkForId(voteID) ?? string.Empty;

            return string.Empty;
        }


        
        /// <summary>
        /// Property to get the total number of ranked voters in the tally.
        /// </summary>
        public int RankedVoterCount => voteCounter.GetVotersCollection(VoteType.Rank).Count;

        /// <summary>
        /// Property to get the total number of normal voters in the tally.
        /// </summary>
        public int NormalVoterCount => voteCounter.GetVotersCollection(VoteType.Vote).Count(voter => !voter.Key.IsPlanName());

        /// <summary>
        /// Calculate the number of non-plan voters in the provided vote object.
        /// </summary>
        /// <param name="vote">The vote containing a list of voters.</param>
        /// <returns>Returns how many of the voters in this vote were users (rather than plans).</returns>
        public int CountVote(KeyValuePair<string, HashSet<string>> vote) =>
            vote.Value?.Count(vc => voteCounter.HasPlan(vc) == false) ?? 0;

        /// <summary>
        /// Get a list of voters, ordered alphabetically, except the first voter,
        /// who is the 'earliest' of the provided voters (ie: the first person to
        /// vote for this vote or plan).
        /// </summary>
        /// <param name="voters">A set of voters.</param>
        /// <returns>Returns an organized, sorted list.</returns>
        public IEnumerable<string> GetOrderedVoterList(HashSet<string> voters)
        {
            if (voters == null || voters.Count == 0)
                return new List<string>();

            var firstVoter = GetFirstVoter(voters);
            var voterList = new List<string>();
            if (firstVoter != null)
                voterList.Add(firstVoter);
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
        public string? GetFirstVoter(HashSet<string> voters)
        {
            var planVoters = voters.Where(v => voteCounter.HasPlan(v));
            var votersCollection = voteCounter.GetVotersCollection(VoteType.Vote);

            if (planVoters.Any())
            {
                return planVoters.MinObject(v => votersCollection[v]);
            }

            var nonFutureVoters = voters.Except(voteCounter.FutureReferences.Select(p => p.Author));

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
        public IOrderedEnumerable<IGrouping<string, KeyValuePair<string, HashSet<string>>>> GroupVotesByTask(Dictionary<string, HashSet<string>> allVotes)
        {
            var grouped = allVotes.GroupBy(v => VoteString.GetVoteTask(v.Key.GetFirstLine()), StringComparer.OrdinalIgnoreCase).OrderBy(v => v.Key);

            grouped = grouped.OrderBy(v => voteCounter.TaskList.IndexOf(v.Key));

            return grouped;
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

            return nodeList.OrderByDescending(v => v.VoterCount).ThenBy(v => LastVoteID(v.Voters));
        }

        /// <summary>
        /// Gets the ID number of the last vote made among the provided voters.
        /// Returns 0 if there are no voters passed in, or no valid post ID values.
        /// </summary>
        /// <param name="voters">The voters for a given vote.</param>
        /// <returns>Returns the last vote ID made for this vote.</returns>
        public int LastVoteID(HashSet<string> voters)
        {
            if (voters.Count == 0)
                return 0;

            var votersCollection = voteCounter.GetVotersCollection(VoteType.Vote);

            Dictionary<string, int> voteCollInt = new Dictionary<string, int>();

            var ids = from voter in voters
                    where votersCollection.ContainsKey(voter)
                    select int.Parse(votersCollection[voter]);

            if (!ids.Any())
                return 0;

            return ids.Max();
        }

        #endregion
    }
}
