using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NetTally.Utility;

namespace NetTally
{
    public class VoteCounter : IVoteCounter
    {
        readonly VoteConstructor voteConstructor;
        readonly Dictionary<string, string> cleanVoteLookup = new Dictionary<string, string>();

        /// <summary>
        /// Constructor
        /// </summary>
        public VoteCounter()
        {
            voteConstructor = new VoteConstructor(this);
        }

        #region Public Interface
        /// <summary>
        /// Reset all tracking variables.
        /// </summary>
        public void Reset()
        {
            VotesWithSupporters.Clear();
            VoterMessageId.Clear();
            RankedVotesWithSupporters.Clear();
            RankedVoterMessageId.Clear();
            PlanNames.Clear();
            FloatingReferences.Clear();
            cleanVoteLookup.Clear();
            Title = string.Empty;
        }

        public string Title { get; set; } = string.Empty;

        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> RankedVoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> PlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public List<PostComponents> VotePosts { get; private set; } = new List<PostComponents>();

        public List<PostComponents> FloatingReferences { get; } = new List<PostComponents>();

        public bool HasRankedVotes => RankedVotesWithSupporters.Count > 0;

        public Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVotesWithSupporters;
            else
                return VotesWithSupporters;
        }

        public Dictionary<string, string> GetVotersCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVoterMessageId;
            else
                return VoterMessageId;
        }

        /// <summary>
        /// Construct the votes Results from the provide list of HTML pages.
        /// </summary>
        /// <param name="pages"></param>
        public void TallyVotes(IQuest quest, List<HtmlDocument> pages)
        {
            if (pages == null)
                throw new ArgumentNullException(nameof(pages));

            if (pages.Count == 0)
                return;

            IForumAdapter forumAdapter = quest.GetForumAdapter();

            Reset();

            // Set the thread author for reference.
            string threadAuthor = forumAdapter.GetAuthorOfThread(pages.First());

            var posts = from page in pages
                        where page != null
                        from post in forumAdapter.GetPostsFromPage(page)
                        where post != null
                        let postNumber = forumAdapter.GetPostNumberOfPost(post)
                        where postNumber >= quest.FirstTallyPost && (quest.ReadToEndOfThread || postNumber <= quest.EndPost)
                        let postCom = GetPostComponents(post, quest)
                        where postCom.IsVote && postCom.Author != threadAuthor
                        select postCom;

            VotePosts = posts.ToList();

            // Process all votes, except floating references (votes solely for another username).
            foreach (var post in VotePosts.OrderBy(p => p))
            {
                voteConstructor.ProcessPost(post, quest, true);
            }

            // Process any floating references (votes solely for another username) that exist in the list.

            // Verify that the floating references were the last vote made by each individual.
            var finalReferences = FloatingReferences.Where(r => r == VotePosts.Where(v => v.Author == r.Author).OrderBy(o => o).Last());

            foreach (var post in finalReferences)
            {
                voteConstructor.ProcessPost(post, quest, false);
            }
        }

        /// <summary>
        /// Extract the components from an HTML post, and store it in a PostComponents object.
        /// </summary>
        /// <param name="post">The post to be decomposed.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns the extracted post components.</returns>
        public PostComponents GetPostComponents(HtmlNode post, IQuest quest)
        {
            if (post == null || quest == null)
                return null;

            IForumAdapter forumAdapter = quest.GetForumAdapter();
            string postAuthor = forumAdapter.GetAuthorOfPost(post);
            string postID = forumAdapter.GetIdOfPost(post);
            string postText = forumAdapter.GetTextOfPost(post);

            if (DebugMode.Instance.Active)
                postAuthor = postAuthor + "_" + postID;

            return new PostComponents(postAuthor, postID, postText);
        }

        /// <summary>
        /// Merges the specified from vote into the specified to vote, assuming the votes aren't the same.
        /// Moves the voters from the from vote into the to vote list, and removes the from vote's key.
        /// </summary>
        /// <param name="fromVote">Vote that is being merged.</param>
        /// <param name="toVote">Vote that is being merged into.</param>
        public bool Merge(string fromVote, string toVote, VoteType voteType)
        {
            if (fromVote == null)
                throw new ArgumentNullException(nameof(fromVote));
            if (toVote == null)
                throw new ArgumentNullException(nameof(toVote));
            if (fromVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(fromVote), "Vote string is empty.");
            if (toVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(toVote), "Vote string is empty.");
            if (fromVote == toVote)
                return false;

            Dictionary<string, HashSet<string>> votesSet;

            if (voteType == VoteType.Rank)
                votesSet = RankedVotesWithSupporters;
            else
                votesSet = VotesWithSupporters;

            HashSet<string> fromVoters;
            HashSet<string> toVoters;

            if (!votesSet.TryGetValue(fromVote, out fromVoters))
                throw new ArgumentException(nameof(fromVote) + " does not exist.");
            if (!votesSet.TryGetValue(toVote, out toVoters))
                throw new ArgumentException(nameof(toVote) + " does not exist.");

            toVoters.UnionWith(fromVoters);

            votesSet.Remove(fromVote);

            return true;
        }

        /// <summary>
        /// Merges voter support.
        /// All of the list of provided voters are adjusted to support the same votes
        /// as those supported by the voterToJoin.
        /// </summary>
        /// <param name="voters">List of voters that are being adjusted.</param>
        /// <param name="voterToJoin">Voter that all specified voters will be joining.</param>
        /// <param name="voteType">The type of vote being manipulated.</param>
        /// <returns>Returns true if adjustments were made.</returns>
        public bool Join(List<string> voters, string voterToJoin, VoteType voteType)
        {
            if (voters == null)
                throw new ArgumentNullException(nameof(voters));
            if (voterToJoin == null)
                throw new ArgumentNullException(nameof(voterToJoin));
            if (voterToJoin == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(voterToJoin), "Voter string is empty.");
            if (voters.Count == 0)
                return false;

            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;

            var joinVotersVotes = votesDict.Where(v => v.Value.Contains(voterToJoin));

            foreach (string voter in voters)
            {
                if (voter != voterToJoin)
                {
                    RemoveSupport(voter, voteType);

                    foreach (var vote in joinVotersVotes)
                    {
                        vote.Value.Add(voter);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Delete a vote from the vote list specified.
        /// </summary>
        /// <param name="vote">The vote to remove.</param>
        /// <param name="voteType">The type of vote to remove.</param>
        /// <returns>Returns true if a vote was removed.</returns>
        public bool Delete(string vote, VoteType voteType)
        {
            if (vote == null && vote == string.Empty)
                return false;

            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;

            bool removed = false;

            if (votesDict.ContainsKey(vote))
            {
                var votersToTrim = votesDict[vote];

                removed = votesDict.Remove(vote);

                foreach (var voter in votersToTrim)
                    TrimVoter(voter, voteType);
            }

            return removed;
        }

        /// <summary>
        /// Rename a vote.
        /// </summary>
        /// <param name="oldVote">The old vote text.</param>
        /// <param name="newVote">The new vote text.</param>
        /// <param name="voteType">The type of vote.</param>
        /// <returns>Returns true if it renamed the vote.</returns>
        public bool Rename(string oldVote, string newVote, VoteType voteType)
        {
            if (oldVote == null)
                throw new ArgumentNullException(nameof(oldVote));
            if (newVote == null)
                throw new ArgumentNullException(nameof(newVote));
            if (oldVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(oldVote), "Vote string is empty.");
            if (newVote == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(newVote), "Vote string is empty.");
            if (oldVote == newVote)
                return false;

            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;

            if (votesDict.ContainsKey(newVote))
            {
                return Merge(oldVote, newVote, voteType);
            }

            HashSet<string> votes;
            if (votesDict.TryGetValue(oldVote, out votes))
            {
                votesDict.Remove(oldVote);
                votesDict[newVote] = votes;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a supporter to the supplied vote.
        /// Adds the vote to the vote list if it didn't already exist.
        /// </summary>
        /// <param name="vote">The vote being supported.</param>
        /// <param name="voter">The voter supporting the vote.</param>
        /// <param name="voteType">The type of vote.</param>
        /// <param name="quest">The quest attached to the vote being adjusted.</param>
        public void AddVoteSupport(string vote, string voter, VoteType voteType, IQuest quest)
        {
            var votes = GetVotesCollection(voteType);

            // Find any existing vote that matches the current vote partition.
            string voteKey = GetVoteKey(vote, quest, voteType);

            // Make sure there's a hashset for the voter list available for the vote key.
            if (!votes.ContainsKey(voteKey))
            {
                votes[voteKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            // Update the supporters list.
            votes[voteKey].Add(voter);
        }

        /// <summary>
        /// Add or update the supplied voter's post ID.
        /// If the vote type is a plan, add the voter to the plan names list as well.
        /// </summary>
        /// <param name="voter">The voter.</param>
        /// <param name="postID">The ID of their post.</param>
        /// <param name="voteType">The type of vote.</param>
        public void AddVoterPostID(string voter, string postID, VoteType voteType)
        {
            var voters = GetVotersCollection(voteType);

            voters[voter] = postID;

            if (voteType == VoteType.Plan)
                PlanNames.Add(voter);
        }

        /// <summary>
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        /// <param name="votesDict">Vote support dictionary to remove voter support from.</param>
        public void RemoveSupport(string voter, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);

            List<string> emptyVotes = new List<string>();

            foreach (var vote in votes)
            {
                if (vote.Value.Remove(voter))
                {
                    if (vote.Value.Count == 0)
                    {
                        emptyVotes.Add(vote.Key);
                    }
                }
            }

            foreach (var vote in emptyVotes)
            {
                votes.Remove(vote);
            }
        }

        /// <summary>
        /// Find all votes tied to a given vote line.
        /// The "plan name" (possibly user name) is checked with the
        /// standard and alternate extractions (adding a special marker character
        /// depending on whether the word "plan" is used, and whether it's 
        /// standard or alt) in order to look up votes that said (possible) voter
        /// supports.
        /// </summary>
        /// <param name="voteLine">The vote line to be checked.</param>
        /// <returns>Returns a list of all votes supported by the user or plan
        /// specified in the vote line, if found.  Otherwise returns an
        /// empty list.</returns>
        public List<string> GetVotesFromReference(string voteLine)
        {
            string planName = VoteLine.GetVoteReferenceName(voteLine);

            var planVotes = VotesWithSupporters.Where(v => v.Value.Contains(planName));

            if (planVotes.Count() > 0)
                return planVotes.Select(v => v.Key).ToList();

            // Get alternate version.
            planName = VoteLine.GetVoteReferenceName(voteLine, true);

            planVotes = VotesWithSupporters.Where(v => v.Value.Contains(planName));

            if (planVotes.Count() > 0)
                return planVotes.Select(v => v.Key).ToList();

            return new List<string>();
        }

        #endregion

        #region Private support methods
        /// <summary>
        /// Attempt to find any existing vote that matches with the vote we have,
        /// and can be used as a key in the VotesWithSupporters table.
        /// </summary>
        /// <param name="vote">The vote to search for.</param>
        /// <returns>Returns the string that can be used as a key in the VotesWithSupporters table.</returns>
        private string GetVoteKey(string vote, IQuest quest, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);

            // If the vote already matches an existing key, we don't need to search again.
            if (votes.ContainsKey(vote))
            {
                return vote;
            }

            var minVote = VoteLine.MinimizeVote(vote, quest);

            // If it matches a lookup value, return the lookup key
            string lookupVote;
            if (cleanVoteLookup.TryGetValue(minVote, out lookupVote))
            {
                return lookupVote;
            }

            // If it's not in the lookup table, add it.
            cleanVoteLookup[minVote] = vote;

            return vote;
        }

        /// <summary>
        /// Will remove the specified voter from the voter ID list if there are no
        /// votes that they are currently supporting.
        /// </summary>
        /// <param name="voter">The voter to trim.</param>
        /// <param name="voteType">The type of vote to check.</param>
        private void TrimVoter(string voter, VoteType voteType)
        {
            var votesDict = voteType == VoteType.Rank ? RankedVotesWithSupporters : VotesWithSupporters;
            var votersDict = voteType == VoteType.Rank ? RankedVoterMessageId : VoterMessageId;

            if (!votesDict.Values.Any(v => v.Contains(voter)))
            {
                votersDict.Remove(voter);
            }
        }
        #endregion
    }
}
