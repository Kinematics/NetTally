using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NetTally.Adapters;

namespace NetTally
{
    public class VoteCounter : IVoteCounter
    {
        readonly VoteConstructor voteConstructor;
        readonly Dictionary<string, string> cleanVoteLookup = new Dictionary<string, string>();
        readonly Dictionary<string, string> cleanedKeys = new Dictionary<string, string>();
        public List<PostComponents> PostsList { get; private set; } = new List<PostComponents>();


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
        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> RankedVoterMessageId { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; } = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Public Interface Functions
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

            ReferenceVoters.Clear();
            ReferenceVoterPosts.Clear();
            ReferencePlanNames.Clear();
            ReferencePlans.Clear();

            FutureReferences.Clear();

            cleanVoteLookup.Clear();
            cleanedKeys.Clear();
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
        public async Task TallyVotes(IQuest quest, ThreadStartInfo startInfo, List<Task<HtmlDocument>> pages)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));
            if (pages == null)
                throw new ArgumentNullException(nameof(pages));

            if (pages.Count == 0)
                return;

            DebugMode.Update();

            var firstPage = await pages.First();

            // Use the title of the first page for the descriptive output.
            ThreadInfo threadInfo = quest.ForumAdapter.GetThreadInfo(firstPage);
            Title = threadInfo.Title;

            PostsList = new List<PostComponents>();

            while (pages.Count > 0)
            {
                var finishedPage = await Task.WhenAny(pages);
                pages.Remove(finishedPage);

                if (finishedPage.IsFaulted)
                {
                    var canceled = finishedPage.Exception.InnerExceptions.FirstOrDefault(e => e is OperationCanceledException);
                    if (canceled != null)
                        throw canceled;

                    throw new ApplicationException("Not all pages loaded.  Rerun tally.");
                }

                var page = await finishedPage;

                if (page == null)
                    throw new ApplicationException("Not all pages loaded.  Rerun tally.");

                var posts = from post in quest.ForumAdapter.GetPosts(page)
                            where post != null && post.IsVote && post.Author != threadInfo.Author &&
                                post.IsAfterStart(startInfo) &&
                                (quest.ReadToEndOfThread || post.Number <= quest.EndPost)
                            select post;

                PostsList.AddRange(posts);
            }

            PostsList = PostsList.OrderBy(p => p.Number).ToList();

            TallyPosts(quest);
        }

        /// <summary>
        /// Construct the tally results based on the provided list of posts.
        /// </summary>
        /// <param name="quest">The quest being tallied.</param>
        /// <param name="PostsList">The list of PostComponents that define valid vote posts.</param>
        public void TallyPosts(IQuest quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            Reset();

            if (PostsList == null || PostsList.Count == 0)
                return;

            // Preprocessing
            foreach (var post in PostsList)
            {
                ReferenceVoters.Add(post.Author);
                ReferenceVoterPosts[post.Author] = post.ID;
                voteConstructor.PreprocessPlans(post, quest);
            }

            // Once all the plans are in place, set the working votes for each post.
            foreach (var post in PostsList)
            {
                post.SetWorkingVote(voteConstructor.GetWorkingVote);
            }

            var unprocessed = PostsList;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                // Get the list of the ones that were processed.
                var processed = unprocessed.Where(p => voteConstructor.ProcessPost(p, quest) == true).ToList();

                // As long as some got processed, remove those from the unprocessed list
                // and let the loop run again.
                if (processed.Any())
                {
                    unprocessed = unprocessed.Except(processed).ToList();
                }
                else
                {
                    // If none got processed (and there must be at least some waiting on processing),
                    // Set the ForceProcess flag on them to avoid pending FutureReference waits.
                    foreach (var p in unprocessed)
                    {
                        p.ForceProcess = true;
                    }
                }
            }
        }

        /// <summary>
        /// Add a vote to the vote counter.
        /// </summary>
        /// <param name="voteParts">A string list of all the parts of the vote to be added.</param>
        /// <param name="voter">The voter for this vote.</param>
        /// <param name="postID">The post ID for this vote.</param>
        /// <param name="voteType">The type of vote being added.</param>
        public void AddVotes(IEnumerable<string> voteParts, string voter, string postID, VoteType voteType)
        {
            if (voteParts == null)
                throw new ArgumentNullException(nameof(voteParts));
            if (string.IsNullOrEmpty(voter))
                throw new ArgumentNullException(nameof(voter));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            if (voteParts.Count() == 0)
                return;

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
                throw new ArgumentException("No target voter provided.", nameof(voterToJoin));
            if (voters.Count == 0)
                return false;

            var votes = GetVotesCollection(voteType);

            var joinVotersVotes = votes.Where(v => v.Value.Contains(voterToJoin));

            int count = 0;

            foreach (string voter in voters)
            {
                if (voter != voterToJoin)
                {
                    count++;
                    RemoveSupport(voter, voteType);

                    foreach (var vote in joinVotersVotes)
                    {
                        vote.Value.Add(voter);
                    }
                }
            }

            return count > 0;
        }

        /// <summary>
        /// Delete a vote from the vote list specified.
        /// </summary>
        /// <param name="vote">The vote to remove.</param>
        /// <param name="voteType">The type of vote to remove.</param>
        /// <returns>Returns true if a vote was removed.</returns>
        public bool Delete(string vote, VoteType voteType)
        {
            if (string.IsNullOrEmpty(vote))
                return false;

            var votes = GetVotesCollection(voteType);

            bool removed = false;

            if (votes.ContainsKey(vote))
            {
                var votersToTrim = votes[vote];

                removed = votes.Remove(vote);

                foreach (var voter in votersToTrim)
                    TrimVoter(voter, voteType);
            }

            return removed;
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
        public List<string> GetVotesFromReference(string voteLine, string author)
        {
            if (voteLine == null)
                throw new ArgumentNullException(nameof(voteLine));
            if (author == null)
                throw new ArgumentNullException(nameof(author));

            List<string> results = new List<string>();

            var referenceNames = VoteString.GetVoteReferenceNames(voteLine);

            string searchName = referenceNames[ReferenceType.Plan].FirstOrDefault(HasPlan);

            if (searchName == null)
            {
                searchName = referenceNames[ReferenceType.Voter].FirstOrDefault(n => n != author && HasVoter(n, VoteType.Vote));
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
            if (!planName.StartsWith(Utility.Text.PlanNameMarker, StringComparison.Ordinal))
            {
                planName = Utility.Text.PlanNameMarker + planName;
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
            return voters.ContainsKey(voterName);
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
                return vote;

            // Store a lookup of the cleaned version of the vote so we don't have to repeat the processing.
            if (!cleanedKeys.ContainsKey(vote))
                cleanedKeys[vote] = VoteString.RemoveBBCode(vote);

            // Find any vote that matches using an agnostic string comparison, that ignores
            // case, spacing, and most punctuation.
            string agVote = votes.Keys.FirstOrDefault(k => 
                Utility.Text.AgnosticStringComparer.Equals(cleanedKeys[vote], cleanedKeys[k]));

            // If we found a match, return that; otherwise this is a new vote, so return it unchanged.
            return agVote ?? vote;
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
            var votes = GetVotesCollection(voteType);
            var voters = GetVotersCollection(voteType);

            if (!votes.Values.Any(v => v.Contains(voter)))
            {
                voters.Remove(voter);
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
        private bool MergeVotes(string fromVote, string toVote) => Rename(fromVote, toVote, VoteType.Vote);

        /// <summary>
        /// Rename a vote.
        /// </summary>
        /// <param name="oldVote">The old vote text.</param>
        /// <param name="newVote">The new vote text.</param>
        /// <param name="voteType">The type of vote.</param>
        /// <returns>Returns true if it renamed the vote.</returns>
        private bool Rename(string oldVote, string newVote, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);

            var oldVoteObj = votes.FirstOrDefault(v => v.Key == oldVote);

            return Rename(oldVoteObj, newVote, voteType);
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
            if ((string.IsNullOrEmpty(vote.Key)) || (vote.Value == null))
                throw new ArgumentNullException(nameof(vote));
            if (revisedKey == null)
                throw new ArgumentNullException(nameof(revisedKey));
            if (revisedKey == string.Empty)
                throw new ArgumentException("New vote key is empty.", nameof(revisedKey));
            if (vote.Key == revisedKey)
                return false;

            var votes = GetVotesCollection(voteType);

            if (votes.ContainsKey(revisedKey))
            {
                votes[revisedKey].UnionWith(vote.Value);
            }
            else
            {
                votes[revisedKey] = vote.Value;
            }

            votes.Remove(vote.Key);

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
