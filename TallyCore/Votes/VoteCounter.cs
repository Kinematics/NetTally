﻿using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

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

        #region Public Interface Properties
        public string Title { get; set; } = string.Empty;

        public HashSet<string> ReferenceVoters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> ReferenceVoterPosts { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> ReferencePlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, List<string>> ReferencePlans { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public HashSet<PostComponents> FutureReferences { get; } = new HashSet<PostComponents>();

        public HashSet<string> PlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool HasRankedVotes => RankedVotesWithSupporters.Count > 0;

        #endregion

        #region Public Class Properties
        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> RankedVoterMessageId { get; } = new Dictionary<string, string>();

        public Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Public Interface Functions
        /// <summary>
        /// Reset all tracking variables.
        /// </summary>
        public void Reset()
        {
            Title = string.Empty;

            VotesWithSupporters.Clear();
            VoterMessageId.Clear();
            RankedVotesWithSupporters.Clear();
            RankedVoterMessageId.Clear();
            PlanNames.Clear();

            ReferenceVoters.Clear();
            ReferenceVoterPosts.Clear();
            ReferencePlanNames.Clear();
            ReferencePlans.Clear();

            FutureReferences.Clear();

            cleanVoteLookup.Clear();
        }

        /// <summary>
        /// Get the dictionary collection of votes for the requested vote type.
        /// </summary>
        /// <param name="voteType">The type of vote being requested.</param>
        /// <returns>Returns a dictionary collection of the requested vote type.</returns>
        public Dictionary<string, HashSet<string>> GetVotesCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVotesWithSupporters;
            else
                return VotesWithSupporters;
        }

        /// <summary>
        /// Get the dictionary collection of voters and post IDs for the requested vote type.
        /// </summary>
        /// <param name="voteType">The type of vote being requested.</param>
        /// <returns>Returns a dictionary collection of the requested voter type.</returns>
        public Dictionary<string, string> GetVotersCollection(VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return RankedVoterMessageId;
            else
                return VoterMessageId;
        }

        /// <summary>
        /// Construct the votes Results from the provided list of HTML pages.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="pages">The web pages that have been loaded for the quest.</param>
        public void TallyVotes(IQuest quest, List<HtmlDocument> pages)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (pages == null)
                throw new ArgumentNullException(nameof(pages));

            if (pages.Count == 0)
                return;

            Reset();

            IForumAdapter forumAdapter = quest.GetForumAdapter();

            var firstPage = pages.First();

            // Use the title of the first page for the descriptive output.
            Title = forumAdapter.GetPageTitle(firstPage);

            // Set the thread author for reference.
            string threadAuthor = forumAdapter.GetAuthorOfThread(firstPage);

            var postsWithVotes = from page in pages
                                 where page != null
                                 from post in forumAdapter.GetPostsFromPage(page)
                                 where post != null
                                 let postNumber = forumAdapter.GetPostNumberOfPost(post)
                                 where postNumber >= quest.FirstTallyPost && (quest.ReadToEndOfThread || postNumber <= quest.EndPost)
                                 let postCom = GetPostComponents(post, quest)
                                 where postCom.IsVote && postCom.Author != threadAuthor
                                 select postCom;

            List<PostComponents> postsList = postsWithVotes.ToList();

            TallyPosts(quest, postsList);
        }

        /// <summary>
        /// Construct the tally results based on the provided list of posts.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="posts">The list of PostComponents that define valid vote posts.</param>
        public void TallyPosts(IQuest quest, List<PostComponents> posts)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (posts == null)
                throw new ArgumentNullException(nameof(posts));

            // Preprocessing
            foreach (var post in posts)
            {
                ReferenceVoters.Add(post.Author);
                ReferenceVoterPosts[post.Author] = post.ID;
                voteConstructor.PreprocessPlans(post, quest);
            }

            var unprocessed = posts;
            bool changed = true;

            // Main processing
            while (unprocessed.Any() && changed == true)
            {
                var processed = unprocessed.Where(p => voteConstructor.ProcessPost(p, quest) == true).ToList();

                changed = processed.Any();

                unprocessed = unprocessed.Except(processed).ToList();
            }
        }

        /// <summary>
        /// Add a vote to the vote counter.
        /// </summary>
        /// <param name="voteParts">A string list of all the parts of the vote to be added.</param>
        /// <param name="voter">The voter for this vote.</param>
        /// <param name="postID">The post ID for this vote.</param>
        /// <param name="voteType">The type of vote being added.</param>
        public void AddVote(IEnumerable<string> voteParts, string voter, string postID, VoteType voteType)
        {
            if (voteParts == null)
                throw new ArgumentNullException(nameof(voteParts));
            if (string.IsNullOrEmpty(voter))
                throw new ArgumentNullException(nameof(voter));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            var votes = GetVotesCollection(voteType);
            var voters = GetVotersCollection(voteType);

            // Store/update the post ID of the voter
            voters[voter] = postID;

            // Track plan names
            if (voteType == VoteType.Plan)
                PlanNames.Add(voter);

            // Remove the voter from any existing votes
            foreach (var vote in votes)
            {
                vote.Value.Remove(voter);
            }

            // Add/update all segments of the provided vote
            foreach (var part in voteParts)
            {
                string voteKey = GetVoteKey(part, voteType);

                // Make sure there's a hashset for the voter list available for the vote key.
                if (!votes.ContainsKey(voteKey))
                {
                    votes[voteKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                // Update the supporters list.
                votes[voteKey].Add(voter);
            }

            // Any votes that no longer have any support can be removed
            var emptyVotes = votes.Where(v => v.Value.Count == 0).ToList();
            foreach (var emptyVote in emptyVotes)
            {
                votes.Remove(emptyVote.Key);
            }
        }

        /// <summary>
        /// Merges the specified from vote into the specified to vote, assuming the votes aren't the same.
        /// Moves the voters from the from vote into the to vote list, and removes the 'from' vote's key.
        /// </summary>
        /// <param name="fromVote">Vote that is being merged.</param>
        /// <param name="toVote">Vote that is being merged into.</param>
        /// <param name="voteType">The type of vote being merged.</param>
        /// <returns>Returns true if there were any changes.</returns>
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

            if (voteType == VoteType.Rank)
                return MergeRanks(fromVote, toVote);
            else
                return MergeVotes(fromVote, toVote);
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

            var votesSet = GetVotesCollection(voteType);

            var joinVotersVotes = votesSet.Where(v => v.Value.Contains(voterToJoin));

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

            var votesSet = GetVotesCollection(voteType);

            bool removed = false;

            if (votesSet.ContainsKey(vote))
            {
                var votersToTrim = votesSet[vote];

                removed = votesSet.Remove(vote);

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

            var votesSet = GetVotesCollection(voteType);

            if (votesSet.ContainsKey(newVote))
            {
                return Merge(oldVote, newVote, voteType);
            }

            HashSet<string> votes;
            if (votesSet.TryGetValue(oldVote, out votes))
            {
                votesSet.Remove(oldVote);
                votesSet[newVote] = votes;
                return true;
            }

            return false;
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
            List<string> results = new List<string>();

            var referenceNames = VoteString.GetVoteReferenceNames(voteLine);

            string searchName = referenceNames[ReferenceType.Plan].FirstOrDefault(n => HasPlan(n));

            if (searchName == null)
            {
                searchName = referenceNames[ReferenceType.Voter].FirstOrDefault(n => HasVoter(n, VoteType.Vote));
            }

            if (searchName != null)
            {
                var planVotes = VotesWithSupporters.Where(v => v.Value.Contains(searchName));

                results.AddRange(planVotes.Select(v => v.Key));
            }

            return results;
        }

        /// <summary>
        /// Determine if the requested plan name exists in the current list of plans.
        /// Applies the plan name marker character to the provided plan name, if it
        /// doesn't already exist.
        /// </summary>
        /// <param name="planName">The name of the plan to check for.</param>
        /// <returns>Returns whether the provided plan name exists in the current PlanNames hash set.</returns>
        public bool HasPlan(string planName)
        {
            if (!planName.StartsWith(Utility.Text.PlanNameMarker))
            {
                planName = $"{Utility.Text.PlanNameMarker}{planName}";
            }

            return PlanNames.Contains(planName);
        }

        /// <summary>
        /// Check to see whether the specified vote has been recorded.
        /// Ranking votes are checked against their condensed forms.
        /// </summary>
        /// <param name="vote">The vote to check.</param>
        /// <param name="voteType">The type of vote being checked.</param>
        /// <returns>Returns true if found.</returns>
        public bool HasVote(string vote, VoteType voteType)
        {
            if (voteType == VoteType.Rank)
                return HasCondensedRankVote(vote);

            var votes = GetVotesCollection(voteType);
            return votes.ContainsKey(vote);
        }

        /// <summary>
        /// Check to see whether the specified voter has been recorded.
        /// </summary>
        /// <param name="voterName">The voter to check for.</param>
        /// <param name="voteType">The type of vote being checked.</param>
        /// <returns>Returns true if found.</returns>
        public bool HasVoter(string voterName, VoteType voteType)
        {
            var voters = GetVotersCollection(voteType);
            return voters.Keys.Contains(voterName);
        }

        /// <summary>
        /// Gets a list of ranking votes in condensed form.
        /// </summary>
        /// <returns>Returns a list of ranking votes in condensed form.</returns>
        public List<string> GetCondensedRankVotes()
        {
            var condensed = RankedVotesWithSupporters.Keys.Select(k => VoteString.CondenseVote(k)).Distinct().ToList();
            return condensed;
        }
        #endregion

        #region Private support methods
        /// <summary>
        /// Extract the components from an HTML post, and store it in a PostComponents object.
        /// </summary>
        /// <param name="post">The post to be decomposed.</param>
        /// <param name="quest">The quest being tallied.</param>
        /// <returns>Returns the extracted post components.</returns>
        private PostComponents GetPostComponents(HtmlNode post, IQuest quest)
        {
            if (post == null || quest == null)
                return null;

            IForumAdapter forumAdapter = quest.GetForumAdapter();
            string postAuthor = forumAdapter.GetAuthorOfPost(post);
            string postID = forumAdapter.GetIdOfPost(post);
            string postText = forumAdapter.GetTextOfPost(post);

            if (DebugMode.Active)
                postAuthor = postAuthor + "_" + postID;

            return new PostComponents(postAuthor, postID, postText);
        }

        /// <summary>
        /// Attempt to find any existing vote that matches with the vote we have,
        /// and can be used as a key in the VotesWithSupporters table.
        /// </summary>
        /// <param name="vote">The vote to search for.</param>
        /// <returns>Returns the string that can be used as a key in the VotesWithSupporters table.</returns>
        private string GetVoteKey(string vote, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);

            // If the vote already matches an existing key, we don't need to search again.
            if (votes.ContainsKey(vote))
            {
                return vote;
            }

            var minVote = VoteString.MinimizeVote(vote);

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
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        /// <param name="votesDict">Vote support dictionary to remove voter support from.</param>
        private void RemoveSupport(string voter, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);

            // Remove the voter from any existing votes
            foreach (var vote in votes)
            {
                vote.Value.Remove(voter);
            }

            // Any votes that no longer have any support can be removed
            var emptyVotes = votes.Where(v => v.Value.Count == 0).ToList();
            foreach (var emptyVote in emptyVotes)
            {
                votes.Remove(emptyVote.Key);
            }
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

        /// <summary>
        /// Merges the specified from vote into the specified to vote, assuming the votes aren't the same.
        /// Moves the voters from the from vote into the to vote list, and removes the from vote's key.
        /// </summary>
        /// <param name="fromVote">Vote that is being merged.</param>
        /// <param name="toVote">Vote that is being merged into.</param>
        /// <returns>Returns true if there were any changes.</returns>
        private bool MergeRanks(string fromVote, string toVote)
        {
            var votes = GetVotesCollection(VoteType.Rank);

            Dictionary<KeyValuePair<string, HashSet<string>>, string> mergedVotes = new Dictionary<KeyValuePair<string, HashSet<string>>, string>();

            foreach (var vote in votes)
            {
                if (VoteString.CondenseVote(vote.Key) == fromVote)
                {
                    string toContent = VoteString.GetVoteContent(toVote, VoteType.Rank);
                    string toTask = VoteString.GetVoteTask(toVote, VoteType.Rank);
                    string revisedKey = VoteString.ModifyVoteLine(vote.Key, task: toTask, content: toContent);

                    mergedVotes.Add(vote, revisedKey);
                }
            }

            foreach (var merge in mergedVotes)
            {
                Rename(merge.Key, merge.Value, VoteType.Rank);
            }

            return mergedVotes.Count > 0;
        }

        /// <summary>
        /// Merges the specified from vote into the specified to vote, assuming the votes aren't the same.
        /// Moves the voters from the from vote into the to vote list, and removes the from vote's key.
        /// </summary>
        /// <param name="fromVote">Vote that is being merged.</param>
        /// <param name="toVote">Vote that is being merged into.</param>
        /// <returns>Returns true if there were any changes.</returns>
        private bool MergeVotes(string fromVote, string toVote)
        {
            var votes = GetVotesCollection(VoteType.Vote);

            HashSet<string> fromVoters;
            HashSet<string> toVoters;

            if (!votes.TryGetValue(fromVote, out fromVoters))
                throw new ArgumentException(nameof(fromVote) + " does not exist.");

            if (!votes.TryGetValue(toVote, out toVoters))
            {
                return Rename(fromVote, toVote, VoteType.Vote);
            }

            toVoters.UnionWith(fromVoters);

            votes.Remove(fromVote);

            return true;
        }

        /// <summary>
        /// Rename a vote.
        /// </summary>
        /// <param name="vote">The old vote object.</param>
        /// <param name="revisedKey">The new vote text.</param>
        /// <param name="voteType">The type of vote.</param>
        /// <returns>Returns true if it renamed the vote.</returns>
        private bool Rename(KeyValuePair<string, HashSet<string>> vote, string revisedKey, VoteType voteType)
        {
            if (revisedKey == null)
                throw new ArgumentNullException(nameof(revisedKey));
            if (revisedKey == string.Empty)
                throw new ArgumentOutOfRangeException(nameof(revisedKey), "Vote string is empty.");
            if (vote.Key == revisedKey)
                return false;

            var votesSet = GetVotesCollection(voteType);
            string oldVoteKey = vote.Key;

            HashSet<string> votes;
            if (votesSet.TryGetValue(revisedKey, out votes))
            {
                votes.UnionWith(vote.Value);
            }
            else
            {
                votesSet[revisedKey] = vote.Value;
            }

            votesSet.Remove(oldVoteKey);

            return true;
        }

        /// <summary>
        /// Determines whether the provided vote string can be found in
        /// condensed form in the rank votes.
        /// </summary>
        /// <param name="rankVote">The vote to check for.</param>
        /// <returns>Returns true if found.</returns>
        private bool HasCondensedRankVote(string rankVote)
        {
            foreach (var vote in RankedVotesWithSupporters)
            {
                if (VoteString.CondenseVote(vote.Key) == rankVote)
                    return true;
            }

            return false;
        }
        #endregion
    }
}