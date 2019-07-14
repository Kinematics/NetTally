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
        /// Gets an ordered version of the provided voters.
        /// The first voter was the first voter to support the given plan, and
        /// the rest of the voters are alphabatized.
        /// </summary>
        /// <param name="voters">The voters being ordered.</param>
        /// <returns>Returns an ordered list of the voters.</returns>
        public List<KeyValuePair<Origin, VoteLineBlock>> GetOrderedStandardVoterList(VoterStorage voters)
        {
            var voterList = new List<KeyValuePair<Origin, VoteLineBlock>>();

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

            var firstVoter = GetFirstVoter(voterList.Select(v => v.Key));
            var firstVoterEntry = voterList.First(v => v.Key == firstVoter);
            voterList.Remove(firstVoterEntry);
            voterList.Insert(0, firstVoterEntry);

            return voterList;
        }

        /// <summary>
        /// Get the first voter from the provided list of voters.
        /// </summary>
        /// <param name="voters"></param>
        /// <returns></returns>
        private Origin GetFirstVoter(IEnumerable<Origin> voters)
        {
            if (!voters.Any())
                throw new InvalidOperationException("No voters to process");

            Origin firstVoter = voters.First();

            foreach (var voter in voters)
            {
                // Plans have priority in determining first voter.
                if (voter.AuthorType == IdentityType.Plan)
                {
                    if (firstVoter.AuthorType != IdentityType.Plan)
                    {
                        firstVoter = voter;
                    }
                    else if (voter.ID < firstVoter.ID)
                    {
                        firstVoter = voter;
                    }
                }
                // If the firstVoter is already a plan, don't overwrite with a user.
                // Otherwise update if the new vote is earlier than the existing one.
                else if (firstVoter.AuthorType != IdentityType.Plan && voter.ID < firstVoter.ID)
                {
                    firstVoter = voter;
                }
            }

            return firstVoter;
        }


        public List<KeyValuePair<Origin, VoteLineBlock>> GetOrderedRankedVoterList(VoterStorage voters)
        {
            var result = new List<KeyValuePair<Origin, VoteLineBlock>>();

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
        public int GetVoteVoterCount(KeyValuePair<VoteLineBlock, VoterStorage> vote)
        {
            var users = vote.Value.Where(a => a.Key.AuthorType == IdentityType.User);
            var usersByMarker = users.GroupBy(v => v.Value.MarkerType);

            int count = 0;

            // Approval and Score votes contribute if their value is greater than 50.
            foreach (var group in usersByMarker)
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

        public (int simpleScore, double limitScore) GetVoteScoreResult(KeyValuePair<VoteLineBlock, VoterStorage> vote)
        {
            var users = vote.Value.Where(a => a.Key.AuthorType == IdentityType.User);
            var usersByMarker = users.GroupBy(v => v.Value.MarkerType);

            int count = 0;
            int accum = 0;

            // TODO: Do a statistical margin of error calculation here.

            // Approval and Score votes contribute if their value is greater than 50.
            foreach (var group in usersByMarker)
            {
                accum += group.Key switch
                {
                    MarkerType.Vote => group.Sum(s => s.Value.MarkerValue),
                    MarkerType.Approval => group.Sum(s => s.Value.MarkerValue),
                    MarkerType.Score => group.Sum(s => s.Value.MarkerValue),
                    _ => 0
                };

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



        public (int positive, int negative) GetVoteApprovalResult(KeyValuePair<VoteLineBlock, VoterStorage> vote)
        {
            var users = vote.Value.Where(a => a.Key.AuthorType == IdentityType.User);
            var usersByMarker = users.GroupBy(v => v.Value.MarkerType);

            int positive = 0;
            int negative = 0;

            foreach (var group in usersByMarker)
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



        public int GetStandardVotersCount(KeyValuePair<VoteLineBlock, VoterStorage> vote)
        {
            var nonPlanVoters = vote.Value.Where(v => v.Key.AuthorType == IdentityType.User &&
                    (v.Value.MarkerType == MarkerType.Vote || v.Value.MarkerType == MarkerType.Score || v.Value.MarkerType == MarkerType.Approval));

            return nonPlanVoters.Count();
        }

        public int GetAllVotersCount(KeyValuePair<VoteLineBlock, VoterStorage> vote)
        {
            var nonPlanVoters = vote.Value.Where(v => v.Key.AuthorType == IdentityType.User);

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
