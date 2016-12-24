using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.VoteCounting
{
    public class VoteCounterImpl : IVoteCounter
    {
        #region Lazy singleton creation
        static readonly Lazy<VoteCounterImpl> lazy = new Lazy<VoteCounterImpl>(() => new VoteCounterImpl());

        public static VoteCounterImpl Instance => lazy.Value;

        VoteCounterImpl()
        {
        }
        #endregion

        readonly Dictionary<string, string> cleanVoteLookup = new Dictionary<string, string>();
        readonly Dictionary<string, string> cleanedKeys = new Dictionary<string, string>();
        public List<PostComponents> PostsList { get; private set; } = new List<PostComponents>();

        #region Implement INotifyPropertyChanged interface
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Public Interface Properties
        public IQuest Quest { get; set; } = null;

        public string Title { get; set; } = string.Empty;

        public HashSet<string> ReferenceVoters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> ReferenceVoterPosts { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> ReferencePlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, List<string>> ReferencePlans { get; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public HashSet<PostComponents> FutureReferences { get; } = new HashSet<PostComponents>();

        public HashSet<string> PlanNames { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public bool HasRankedVotes => RankedVotesWithSupporters.Count > 0;

        public HashSet<string> UserDefinedTasks { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Stack<UndoAction> UndoBuffer { get; } = new Stack<UndoAction>();

        public bool HasUndoActions => UndoBuffer.Count > 0;

        bool voteCounterIsTallying = false;

        /// <summary>
        /// Flag whether the tally is currently running.
        /// </summary>
        public bool VoteCounterIsTallying
        {
            get { return voteCounterIsTallying; }
            set
            {
                if (voteCounterIsTallying != value)
                {
                    voteCounterIsTallying = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Public Class Properties
        public Dictionary<string, string> VoterMessageId { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, HashSet<string>> VotesWithSupporters { get; private set; } = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);

        public Dictionary<string, string> RankedVoterMessageId { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, HashSet<string>> RankedVotesWithSupporters { get; private set; } = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);

        #endregion

        #region Basic reset & tally
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

            UndoBuffer.Clear();

            cleanVoteLookup.Clear();
            cleanedKeys.Clear();

            if (VotesWithSupporters.Comparer != Agnostic.StringComparer)
                VotesWithSupporters = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);
            if (RankedVotesWithSupporters.Comparer != Agnostic.StringComparer)
                RankedVotesWithSupporters = new Dictionary<string, HashSet<string>>(Agnostic.StringComparer);

            OnPropertyChanged("VoteCounter");
        }

        public void ResetUserDefinedTasks(string forQuestName)
        {
            if (Quest == null || Quest.DisplayName != forQuestName)
            {
                UserDefinedTasks.Clear();
            }
        }

        /// <summary>
        /// Run the tally using the provided posts, for the selected quest.
        /// </summary>
        /// <param name="posts">The posts to be tallied.</param>
        /// <param name="quest">The quest being tallied.</param>
        public async Task TallyPosts(IEnumerable<PostComponents> posts, IQuest quest)
        {
            Quest = quest;
            PostsList.Clear();
            PostsList.AddRange(posts);
            await TallyPosts().ConfigureAwait(false);
        }

        /// <summary>
        /// Construct the tally results based on the stored list of posts.
        /// Run async so that it doesn't cause UI jank.
        /// </summary>
        public async Task TallyPosts()
        {
            try
            {
                VoteCounterIsTallying = true;

                Reset();

                if (PostsList == null || PostsList.Count == 0)
                    return;

                await Task.Run(() => PreprocessPlans()).ConfigureAwait(false);
                await Task.Run(() => ProcessPosts()).ConfigureAwait(false);
            }
            finally
            {
                VoteCounterIsTallying = false;
            }
        }

        /// <summary>
        /// The first half of tallying posts involves doing the preprocessing
        /// work on the plans in the post list.
        /// </summary>
        private void PreprocessPlans()
        {
            // Preprocessing Phase 1 (Only plans with contents are counted as plans.)
            foreach (var post in PostsList)
            {
                ReferenceVoters.Add(post.Author);
                ReferenceVoterPosts[post.Author] = post.ID;
                VoteConstructor.PreprocessPlansWithContent(post, Quest);
            }

            // Preprocessing Phase 2 (Full-post plans may be named (ie: where the plan name has no contents).)
            // Total vote must have multiple lines.
            foreach (var post in PostsList)
            {
                VoteConstructor.PreprocessPlanLabelsWithContent(post, Quest);
            }

            // Preprocessing Phase 3 (Full-post plans may be named (ie: where the plan name has no contents).)
            // Total vote may be only one line.
            foreach (var post in PostsList)
            {
                VoteConstructor.PreprocessPlanLabelsWithoutContent(post, Quest);
            }

            // Once all the plans are in place, set the working votes for each post.
            foreach (var post in PostsList)
            {
                post.SetWorkingVote(p => VoteConstructor.GetWorkingVote(p));
            }
        }

        /// <summary>
        /// The second half of tallying the posts involves cycling through for
        /// as long as future references need to be handled.
        /// </summary>
        private void ProcessPosts()
        {
            var unprocessed = PostsList;

            // Loop as long as there are any more to process.
            while (unprocessed.Any())
            {
                // Get the list of the ones that were processed.
                var processed = unprocessed.Where(p => VoteConstructor.ProcessPost(p, Quest) == true).ToList();

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

        #endregion

        #region Query on collection stuff
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

        private HashSet<string> GetVoters(string vote, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);

            if (votes.ContainsKey(vote))
                return votes[vote];

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
        /// <param name="author">The author of the vote. Prevent circular references.</param>
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

            if (!referenceNames[ReferenceType.Any].Any())
                return results;

            string proxyName = null;

            if (referenceNames[ReferenceType.Label].Any())
            {
                // If there is a "plan" prefix, then if it's a user reference,
                // check for a ◈plan before checking for the user's base vote.

                // If the reference exists as a plan, use it.
                if (referenceNames[ReferenceType.Plan].Any() && HasPlan(referenceNames[ReferenceType.Plan].First()))
                {
                    // If this is not a user name, get the plan name as the proxy reference.
                    proxyName = PlanNames.First(p => referenceNames[ReferenceType.Plan].Contains(p, Agnostic.StringComparer));
                }
                else if (ReferenceVoters.Contains(referenceNames[ReferenceType.Voter].First(), Agnostic.StringComparer))
                {
                    // If it doesn't exist as a plan, then we can check for users.
                    if (!AdvancedOptions.Instance.DisableProxyVotes)
                    {
                        proxyName = ReferenceVoters.First(n => referenceNames[ReferenceType.Voter].Contains(n, Agnostic.StringComparer));

                        if (proxyName == author)
                            proxyName = null;
                    }
                }
            }
            else
            {
                // If there is no "plan" prefix, and if the plan name is a user
                // reference, it may only refer to that user's vote as a whole.

                // If this matches a user name, get that user name as the proxy reference.
                if (ReferenceVoters.Contains(referenceNames[ReferenceType.Voter].First(), Agnostic.StringComparer))
                {
                    if (!AdvancedOptions.Instance.DisableProxyVotes)
                    {
                        proxyName = ReferenceVoters.First(n => referenceNames[ReferenceType.Voter].Contains(n, Agnostic.StringComparer));

                        if (proxyName == author)
                            proxyName = null;
                    }
                }
                else if (referenceNames[ReferenceType.Plan].Any() && HasPlan(referenceNames[ReferenceType.Plan].First()))
                {
                    // If this is not a user name, get the plan name as the proxy reference.
                    proxyName = PlanNames.First(p => referenceNames[ReferenceType.Plan].Contains(p, Agnostic.StringComparer));
                }
            }

            if (!string.IsNullOrEmpty(proxyName))
            {
                var planVotes = VotesWithSupporters.Where(v => v.Value.Contains(proxyName));

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
            if (string.IsNullOrEmpty(planName))
                return false;

            return PlanNames.Contains(planName, Agnostic.StringComparer);
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

        public bool HasUserEnteredVoter(string voterName, VoteType voteType)
        {
            var voters = GetVotersCollection(voteType);
            return voters.Keys.Contains(voterName, Agnostic.StringComparer);
        }

        /// <summary>
        /// Determines whether the authof of this post has made a newer vote
        /// submission.
        /// </summary>
        /// <param name="post">The post being checked.</param>
        /// <returns>Returns true if the voter has a newer vote
        /// already submitted to the counter.</returns>
        public bool HasNewerVote(PostComponents post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            if (!HasVoter(post.Author, VoteType.Vote))
                return false;

            int submittedID = 0;
            if (!int.TryParse(GetVotersCollection(VoteType.Vote)[post.Author], out submittedID))
            {
                return string.CompareOrdinal(GetVotersCollection(VoteType.Vote)[post.Author], post.ID) > 0;
            }

            return (submittedID > post.IDValue);
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

        #region Modifying Votes
        /// <summary>
        /// Add a collection of votes to the vote counter.
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

            if (!voteParts.Any())
                return;

            // Store/update the post ID of the voter
            AddVoterPostID(voter, postID, voteType);

            // Track plan names
            if (voteType == VoteType.Plan)
                PlanNames.Add(voter);

            var votes = GetVotesCollection(voteType);

            // Remove the voter from any existing votes
            if (RemoveSupport(voter, voteType))
                OnPropertyChanged("Voters");

            // Add/update all segments of the provided vote
            foreach (var part in voteParts)
            {
                AddVote(part, voter, voteType);
            }

            // Cleanup any votes that no longer have any support
            if (CleanupEmptyVotes(voteType))
                OnPropertyChanged("Votes");

        }

        /// <summary>
        /// Adds an individual vote.
        /// </summary>
        /// <param name="vote">The vote that is being added to.</param>
        /// <param name="voter">The voter that is supporting the vote.</param>
        /// <param name="voteType">Type of the vote.</param>
        /// <exception cref="System.ArgumentNullException">vote and voter must not be null or empty.</exception>
        private void AddVote(string vote, string voter, VoteType voteType)
        {
            if (string.IsNullOrEmpty(vote))
                throw new ArgumentNullException(nameof(vote));
            if (string.IsNullOrEmpty(voter))
                throw new ArgumentNullException(nameof(voter));

            string voteKey = GetVoteKey(vote, voteType);
            var votes = GetVotesCollection(voteType);

            // Make sure there's a hashset for the voter list available for the vote key.
            if (votes.ContainsKey(voteKey) == false)
            {
                votes[voteKey] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                OnPropertyChanged("Votes");
            }

            // Update the supporters list if the voter isn't already in it.
            if (votes[voteKey].Contains(voter))
                return;

            votes[voteKey].Add(voter);

            OnPropertyChanged("Voters");
        }

        /// <summary>
        /// Adds the voter post identifier.
        /// </summary>
        /// <param name="voter">The voter.</param>
        /// <param name="postID">The post identifier.</param>
        /// <param name="voteType">Type of the vote.</param>
        /// <exception cref="System.ArgumentNullException">voter and postID may not be null or empty.</exception>
        private void AddVoterPostID(string voter, string postID, VoteType voteType)
        {
            if (string.IsNullOrEmpty(voter))
                throw new ArgumentNullException(nameof(voter));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            // Store/update the post ID of the voter
            var voters = GetVotersCollection(voteType);
            voters[voter] = postID;
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
            if (string.IsNullOrEmpty(fromVote))
                throw new ArgumentNullException(nameof(fromVote));
            if (string.IsNullOrEmpty(toVote))
                throw new ArgumentNullException(nameof(toVote));
            if (fromVote == toVote)
                return false;

            bool merged = false;

            if (voteType == VoteType.Rank)
                merged = MergeRanks(fromVote, toVote);
            else
                merged = MergeVotes(fromVote, toVote);

            if (merged)
            {
                OnPropertyChanged("VoteCounter");
            }

            return merged;
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

            if (mergedVotes.Count > 0)
            {
                var voters = GetVotersCollection(VoteType.Rank);
                UndoBuffer.Push(new UndoAction(UndoActionType.Merge, VoteType.Rank, voters, mergedVotes));

                foreach (var merge in mergedVotes)
                {
                    Rename(merge.Key, merge.Value, VoteType.Rank);
                }

                return true;
            }

            return false;
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
            if (revisedKey.Length == 0)
                throw new ArgumentException("New vote key is empty.", nameof(revisedKey));
            if (vote.Key == revisedKey)
                return false;

            if (voteType != VoteType.Rank)
            {
                var voters = GetVotersCollection(voteType);
                UndoBuffer.Push(new UndoAction(UndoActionType.Merge, voteType, voters,
                    vote.Key, vote.Value, revisedKey, GetVoters(revisedKey, voteType)));
            }


            var votes = GetVotesCollection(voteType);

            string voteKey = GetVoteKey(revisedKey, voteType);

            if (votes.ContainsKey(voteKey))
            {
                bool isRevisedSameAsVote = Agnostic.StringComparer.Equals(vote.Key, voteKey);

                if (isRevisedSameAsVote)
                {
                    var priorVotes = vote.Value;
                    votes.Remove(vote.Key);
                    votes[voteKey] = priorVotes;
                }
                else
                {
                    votes[voteKey].UnionWith(vote.Value);
                    votes.Remove(vote.Key);
                }
            }
            else
            {
                votes[voteKey] = vote.Value;
                votes.Remove(vote.Key);
            }

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
            if (string.IsNullOrEmpty(voterToJoin))
                throw new ArgumentNullException(nameof(voterToJoin));
            if (voters.Count == 0)
                return false;

            var votes = GetVotesCollection(voteType);

            var joinVotersVotes = votes.Where(v => v.Value.Contains(voterToJoin));

            int count = 0;

            var priorVotes = votes.Where(v => v.Value.Any(u => voters.Contains(u)));
            var voterIDList = GetVotersCollection(voteType);
            UndoBuffer.Push(new UndoAction(UndoActionType.Join, voteType, voterIDList, voters, priorVotes));

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

            CleanupEmptyVotes(voteType);

            OnPropertyChanged("VoteCounter");

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

            bool removed = false;

            var votes = GetVotesCollection(voteType);
            Dictionary<string, HashSet<string>> deletedVotes = new Dictionary<string, HashSet<string>>();

            foreach (var v in votes)
            {
                if (v.Key == vote || (voteType == VoteType.Rank && VoteString.CondenseVote(v.Key) == vote))
                {
                    deletedVotes.Add(v.Key, v.Value);
                }
            }

            if (deletedVotes.Count > 0)
            {
                UndoBuffer.Push(new UndoAction(UndoActionType.Delete, voteType, GetVotersCollection(voteType), deletedVotes));

                foreach (var del in deletedVotes)
                {
                    removed = votes.Remove(del.Key) || removed;
                }
            }

            foreach (var v in deletedVotes)
            {
                foreach (var voter in v.Value)
                {
                    TrimVoter(voter, voteType);
                }
            }

            OnPropertyChanged("VoteCounter");

            return removed;
        }

        /// <summary>
        /// Undoes the most recently performed modification to the vote count.
        /// </summary>
        /// <returns>Returns true if it performed an undo action.  Otherwise, false.</returns>
        public bool Undo()
        {
            if (!HasUndoActions)
                return false;

            var undo = UndoBuffer.Pop();
            List<string> vote;

            switch (undo.ActionType)
            {
                case UndoActionType.Delete:
                    foreach (var v in undo.DeletedVotes)
                    {
                        foreach (var voter in v.Value)
                        {
                            AddVote(v.Key, voter, undo.VoteType);
                            AddVoterPostID(voter, undo.PostIDs[voter], undo.VoteType);
                        }
                    }
                    break;
                case UndoActionType.Merge:
                    if (undo.VoteType == VoteType.Rank)
                    {
                        foreach (var mergedVote in undo.MergedVotes)
                        {
                            LimitVoters(mergedVote.Value, mergedVote.Key.Value, VoteType.Rank);

                            foreach (var voter in mergedVote.Key.Value)
                            {
                                AddVote(mergedVote.Key.Key, voter, undo.VoteType);
                                AddVoterPostID(voter, undo.PostIDs[voter], undo.VoteType);
                            }
                        }
                    }
                    else
                    {
                        LimitVoters(undo.Vote2, undo.Voters2, undo.VoteType);

                        vote = new List<string> { undo.Vote1 };
                        foreach (var voter in undo.Voters1)
                        {
                            AddVote(undo.Vote1, voter, undo.VoteType);
                            AddVoterPostID(voter, undo.PostIDs[voter], undo.VoteType);
                        }
                    }
                    break;
                case UndoActionType.Join:
                    foreach (string voter in undo.JoinedVoters)
                    {
                        RemoveSupport(voter, undo.VoteType);

                        foreach (var priorVote in undo.PriorVotes)
                        {
                            if (priorVote.Value.Contains(voter))
                            {
                                vote = new List<string> { priorVote.Key };

                                AddVote(priorVote.Key, voter, undo.VoteType);
                                AddVoterPostID(voter, undo.PostIDs[voter], undo.VoteType);
                            }
                        }
                    }
                    break;
                default:
                    return false;
            }

            CleanupEmptyVotes(undo.VoteType);

            OnPropertyChanged("VoteCounter");

            return true;
        }

        private void LimitVoters(string vote2, HashSet<string> voters2, VoteType voteType)
        {
            var votes = GetVotesCollection(voteType);
            var vote = votes[vote2];

            if (voteType == VoteType.Rank)
            {
                vote.ExceptWith(voters2);
            }
            else
            {
                vote.IntersectWith(voters2);
            }
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
            {
                string clean = VoteString.RemoveBBCode(vote);
                clean = VoteString.DeUrlContent(clean);
                cleanedKeys[vote] = clean;
            }

            // Find any vote that matches using an agnostic string comparison, that ignores
            // case, spacing, and most punctuation.
            string agVote = votes.Keys.FirstOrDefault(k =>
                Agnostic.StringComparer.Equals(cleanedKeys[vote], cleanedKeys[k]));

            // If we found a match, return that; otherwise this is a new vote, so return it unchanged.
            return agVote ?? vote;
        }

        /// <summary>
        /// Remove the voter's support for any existing votes.
        /// </summary>
        /// <param name="voter">The voter name to check for.</param>
        /// <param name="voteType">Type of the vote.</param>
        private bool RemoveSupport(string voter, VoteType voteType)
        {
            bool removedAny = false;

            var votes = GetVotesCollection(voteType);

            // Remove the voter from any existing votes
            foreach (var vote in votes)
            {
                if (vote.Value.Remove(voter))
                    removedAny = true;
            }

            return removedAny;
        }

        /// <summary>
        /// Removes any votes that no longer have any voter support.
        /// </summary>
        /// <param name="voteType">Type of the vote.</param>
        private bool CleanupEmptyVotes(VoteType voteType)
        {
            bool removedAny = false;

            var votes = GetVotesCollection(voteType);

            // Any votes that no longer have any support can be removed
            var emptyVotes = votes.Where(v => v.Value.Count == 0).ToList();
            foreach (var emptyVote in emptyVotes)
            {
                if (votes.Remove(emptyVote.Key))
                    removedAny = true;
            }

            return removedAny;
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

        #endregion
    }
}
