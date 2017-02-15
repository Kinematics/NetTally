using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NetTally.Extensions;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    using PlanDictionary = Dictionary<string, List<Plan>>;
    using IdentitySet = HashSet<Identity>;
    using VoteEntries = Dictionary<VotePartition, HashSet<Identity>>;
    using VoteEntry = KeyValuePair<VotePartition, HashSet<Identity>>;
    using VoterPartitions = Dictionary<Identity, List<VotePartition>>;

    /// <summary>
    /// Class that handles storing processed votes, querying them, and allowing manual
    /// manipulation of some aspects of the final results.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public class VotingRecords : INotifyPropertyChanged
    {
        #region Lazy singleton creation
        static readonly Lazy<VotingRecords> lazy = new Lazy<VotingRecords>(() => new VotingRecords());

        public static VotingRecords Instance => lazy.Value;

        VotingRecords()
        {
        }
        #endregion

        #region Properties
        /// <summary>
        /// Lookup table to translate names to all identities matching that name.
        /// </summary>
        Dictionary<string, IdentitySet> IdentityLookup { get; } = new Dictionary<string, IdentitySet>(Agnostic.StringComparer);

        /// <summary>
        /// Lookup table to translate plan names to plans.
        /// There is a list to account for multiple variants of the same plan name.
        /// </summary>
        Dictionary<string, List<Plan>> PlansLookup { get; set; } = new Dictionary<string, List<Plan>>(Agnostic.StringComparer);

        /// <summary>
        /// Lookup table for all the vote partitions each voter (identity) voted for.
        /// </summary>
        //VoterPartitions VoterVotes { get; } = new VoterPartitions();

        /// <summary>
        /// Lookup table for all the voters supporting each vote partition.
        /// </summary>
        VoteEntries NormalVotes { get; } = new VoteEntries();
        VoteEntries RankedVotes { get; } = new VoteEntries();
        VoteEntries ApprovalVotes { get; } = new VoteEntries();

        Stack<UndoItem> UndoBuffer { get; } = new Stack<UndoItem>();
        public bool HasUndoItems => UndoBuffer.Count > 0;

        HashSet<string> TallyTasks { get; } = new HashSet<string>();
        HashSet<string> UserTasks { get; } = new HashSet<string>();
        public IEnumerable<string> Tasks => TallyTasks.Union(UserTasks);

        HashSet<Post> FutureReferences { get; } = new HashSet<Post>();
        #endregion

        #region Reset
        public void Reset()
        {
            IdentityLookup.Clear();
            PlansLookup.Clear();

            NormalVotes.Clear();
            RankedVotes.Clear();
            ApprovalVotes.Clear();

            FutureReferences.Clear();
            UndoBuffer.Clear();

            TallyTasks.Clear();
        }

        public void ResetUserDefinedTasks(string forQuestName)
        {
            UserTasks.Clear();
        }
        #endregion

        #region General methods

        #region Future Stuff~~
        public void NoteFutureReference(Post post)
        {
            FutureReferences.Add(post);
        }

        internal bool HasNewerVote(Identity identity)
        {
            throw new NotImplementedException();
        }
        #endregion


        #region Voter identities (Populate, query)
        /// <summary>
        /// Add the specified identity to the voter identity lookup table.
        /// Multiple identities can be added for the same name, representing multiple votes.
        /// </summary>
        /// <param name="identity">The identity to add.  Must not be null.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <returns>Returns true if the identity was added, or false if it was not (ie: a duplicate).</returns>
        public bool AddVoterIdentity(Identity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            if (IdentityLookup.TryGetValue(identity.Name, out IdentitySet identities))
            {
                return identities.Add(identity);
            }

            IdentityLookup.Add(identity.Name, new IdentitySet { identity });
            return true;
        }

        /// <summary>
        /// Checks whether the supplied voter name exists in the list of known identities.
        /// This finds names using the current comparison settings, usually ignoring
        /// differences in whitespace, punctuation, etc.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for. Does not need to be exact.  Must not be null or empty.</param>
        /// <returns>Returns true if the voter name is known to exist.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool HasVoterName(string voterName)
        {
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));

            return IdentityLookup.TryGetValue(voterName, out IdentitySet identities);
        }

        /// <summary>
        /// Gets all of the voter identities.
        /// </summary>
        /// <param name="voterName">Name of the voter.</param>
        /// <returns>The Identity Set for that voter.</returns>
        public IdentitySet GetVoterIdentities(string voterName)
        {
            if (IdentityLookup.TryGetValue(voterName, out IdentitySet identities))
            {
                return identities;
            }

            return null;
        }

        /// <summary>
        /// Gets the last voter identity.
        /// </summary>
        /// <param name="voterName">Name of the voter.</param>
        /// <returns>The last (by post ID) identity for that voter.</returns>
        public Identity GetLastVoterIdentity(string voterName, string host = null)
        {
            if (IdentityLookup.TryGetValue(voterName, out IdentitySet identities))
            {
                var matchHost = identities.Where(i => host == null || i.Host == host).OrderBy(i => i.PostIDValue).Last();

                return matchHost;
            }

            return null;
        }
        #endregion

        #region Plans (Populate, query)
        /// <summary>
        /// Adds the plans that were found.
        /// </summary>
        /// <param name="allPlans">The collection of discovered plans.</param>
        public void AddPlans(PlanDictionary allPlans)
        {
            PlansLookup = new PlanDictionary(allPlans, Agnostic.StringComparer);
        }

        /// <summary>
        /// Gets the lookup table for plans.
        /// </summary>
        /// <returns>Returns the plans lookup table.</returns>
        public IEnumerable<Plan> GetPlans()
        {
            return PlansLookup.Select(a => a.Value).SelectMany(b => b);
        }

        /// <summary>
        /// Checks whether the supplied plan name exists in the list of known plans.
        /// This finds names using the current comparison settings, usually ignoring
        /// differences in whitespace, punctuation, etc.
        /// </summary>
        /// <param name="planName">The name of the plan to check for. Does not need to be exact.</param>
        /// <returns>Returns true if the plan name is known to exist.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool HasPlanName(string planName)
        {
            if (string.IsNullOrEmpty(planName))
                throw new ArgumentNullException(nameof(planName));

            return PlansLookup.TryGetValue(planName, out var value);
        }

        /// <summary>
        /// Gets the canonical plan name that matches the requested plan name.
        /// </summary>
        /// <param name="planName">The name of the plan to check for. Does not need to be exact.</param>
        /// <param name="canonicalName">Returns the canonical version of the name, if found.</param>
        /// <returns>Returns true if the requested name is found.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool TryGetPlanName(string planName, out string canonicalName)
        {
            if (string.IsNullOrEmpty(planName))
                throw new ArgumentNullException(nameof(planName));

            if (PlansLookup.TryGetValue(planName, out var value))
            {
                canonicalName = value.First().Identity.Name;
                return true;
            }

            canonicalName = null;
            return false;
        }

        /// <summary>
        /// Gets the canonical name of the requested plan.
        /// </summary>
        /// <param name="planName">Name of the plan.</param>
        /// <returns>Returns the name of the plan that properly matches the request, or null if not found.</returns>
        public string GetPlanName(string planName)
        {
            if (TryGetPlanName(planName, out string canonicalName))
                return canonicalName;

            return null;
        }
        #endregion

        #region Vote Entries (Populate, query)
        /// <summary>
        /// Add any number of vote fragments for a given voter, from a given post.
        /// </summary>
        /// <param name="partitions">A list of vote fragments.</param>
        /// <param name="voterName">The voter.</param>
        /// <param name="postID">The post the vote fragments are from.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddVoteEntries(IEnumerable<VotePartition> partitions, Identity identity)
        {
            if (partitions == null)
                throw new ArgumentNullException(nameof(partitions));
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            if (!partitions.Any())
                return;

            // All existing partitions have this identity removed.
            bool changedVoters = RemoveVoterSupport(identity);
            bool addedAnyVotes = false;

            foreach (var partition in partitions)
            {
                AddTask(partition.Task);

                var votes = GetVoteEntries(partition.VoteType);

                if (votes.TryGetValue(partition, out var identitySet))
                {
                    changedVoters = identitySet.Add(identity) || changedVoters;
                }
                else
                {
                    votes[partition] = new IdentitySet { identity };
                    addedAnyVotes = true;
                }
            }

            if (addedAnyVotes)
                OnPropertyChanged("Votes");
            if (changedVoters)
                OnPropertyChanged("Voters");
        }

        /// <summary>
        /// Removes the voter support of an identity from a vote partition.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns>Returns true if any support identity was removed.</returns>
        private bool RemoveVoterSupport(Identity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            bool removedAny = false;

            RemoveVoterSupportOfType(GetVoteEntries(VoteType.Vote));
            RemoveVoterSupportOfType(GetVoteEntries(VoteType.Rank));
            RemoveVoterSupportOfType(GetVoteEntries(VoteType.Approval));

            return removedAny;

            // Private local function
            void RemoveVoterSupportOfType(VoteEntries votes)
            {
                foreach (var vote in votes)
                {
                    var matching = vote.Value.Where(v => v.Matches(identity));

                    if (matching.Any())
                    {
                        var matchingList = matching.ToList();
                        foreach (var ident in matchingList)
                        {
                            vote.Value.Remove(ident);
                            removedAny = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the vote entries of a particular vote type.
        /// </summary>
        /// <param name="voteType">Type of the vote.</param>
        /// <returns>Returns the vote entries table for the vote type.</returns>
        public VoteEntries GetVoteEntries(VoteType voteType)
        {
            switch (voteType)
            {
                case VoteType.Vote:
                    return NormalVotes;
                case VoteType.Rank:
                    return RankedVotes;
                case VoteType.Approval:
                    return ApprovalVotes;
                default:
                    throw new ArgumentException($"Unknown vote type: {voteType}", nameof(voteType));
            }
        }

        #endregion

        #region Tasks (Populate)
        /// <summary>
        /// Adds a task due to information gleaned from adding votes.
        /// </summary>
        /// <param name="task">The task.</param>
        private void AddTask(string task)
        {
            if (string.IsNullOrEmpty(task))
                return;

            TallyTasks.Add(task);
        }

        /// <summary>
        /// Adds a task based on user input.
        /// </summary>
        /// <param name="task">The task.</param>
        public void AddUserTask(string task)
        {
            if (string.IsNullOrEmpty(task))
                return;

            UserTasks.Add(task);
        }
        #endregion

        #region Managing Votes
        // Merge vote 1 > vote 2
        // Modify task of vote 1 (may imply merge vote 1 > vote 2)
        // Delete vote 1
        // Change voter 1 votes to those made by voter 2
        // Delete voter 1

        public void Merge(VotePartition vote1, VotePartition vote2, VoteType type)
        {
            var votes = GetVoteEntries(type);

            if (!votes.TryGetValue(vote1, out var voters1))
            {
                voters1 = new IdentitySet();
                votes[vote1] = voters1;
            }

            if (!votes.TryGetValue(vote2, out var voters2))
            {
                voters2 = new IdentitySet();
                votes[vote2] = voters2;
            }

            // Save prior state to allow an undo
            PreserveMerge(vote1, vote2, voters1, voters2, type);

            // Update the votes->identity lookup
            voters2.UnionWith(voters1);
            voters1.Clear();
        }

        public void ModifyTask(VotePartition vote1, string newTask, VoteType type)
        {
            var votes = GetVoteEntries(type);

            var vote2 = vote1.ModifyTask(newTask);
            if (vote1 == vote2)
                return;

            if (!votes.TryGetValue(vote1, out var voters1))
            {
                voters1 = new IdentitySet();
                votes[vote1] = voters1;
            }

            if (!votes.TryGetValue(vote2, out var voters2))
            {
                voters2 = new IdentitySet();
                votes[vote2] = voters2;
            }

            // Save prior state to allow an undo
            PreserveMerge(vote1, vote2, voters1, voters2, type);

            // Update the votes->identity lookup
            voters2.UnionWith(voters1);
            voters1.Clear();
        }

        public void Join(IEnumerable<Identity> voters1, Identity voter2, VoteType type)
        {
            foreach (var voter in voters1)
            {
                Join(voter, voter2, type);
            }
        }

        public void Join(Identity voter1, Identity voter2, VoteType type)
        {
            var votes = GetVoteEntries(type);

            var voter1Votes = votes.Where(v => v.Value.Contains(voter1)).Select(v => v.Key);

            PreserveJoin(voter1, voter2, voter1Votes, type);

            foreach (var vote in votes)
            {
                if (vote.Value.Contains(voter2))
                    vote.Value.Add(voter1);
                else
                    vote.Value.Remove(voter1);
            }
        }

        public bool Remove(VotePartition vote1, VoteType type)
        {
            var votes = GetVoteEntries(type);
            bool removed = false;

            if (votes.TryGetValue(vote1, out var voters1))
            {
                if (voters1.Count > 0)
                {
                    removed = true;
                    PreserveRemovedVote(vote1, voters1, type);
                    voters1.Clear();
                }
            }

            return removed;
        }

        public bool Remove(Identity voter1, VoteType type)
        {
            var votes = GetVoteEntries(type);

            var votesWithVoter = votes.Where(v => v.Value.Contains(voter1));

            if (votesWithVoter.Any())
            {
                PreserveRemovedVoter(voter1, votesWithVoter.Select(v => v.Key), type);

                foreach (var vote in votesWithVoter)
                {
                    vote.Value.Remove(voter1);
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Undo
        /// <summary>
        /// Preserves the specified state prior to a vote merge, to allow undoing it.
        /// </summary>
        /// <param name="vote1">The vote1.</param>
        /// <param name="vote2">The vote2.</param>
        /// <param name="voters1">The voters1.</param>
        /// <param name="voters2">The voters2.</param>
        /// <param name="type">The type.</param>
        private void PreserveMerge(VotePartition vote1, VotePartition vote2, IEnumerable<Identity> voters1, IEnumerable<Identity> voters2, VoteType type)
        {
            var undoItem = new UndoItem(UndoItemType.Merge, type, vote1: vote1, vote2: vote2, voters1: voters1, voters2: voters2);
            UndoBuffer.Push(undoItem);
        }

        private void PreserveJoin(Identity voter1, Identity voter2, IEnumerable<VotePartition> voter1Votes, VoteType type)
        {
            var undoItem = new UndoItem(UndoItemType.Join, type, voter1: voter1, voter2: voter2, votes1: voter1Votes);
            UndoBuffer.Push(undoItem);
        }

        private void PreserveRemovedVote(VotePartition vote1, IEnumerable<Identity> vote1Voters, VoteType type)
        {
            var undoItem = new UndoItem(UndoItemType.RemoveVote, type, vote1: vote1, voters1: vote1Voters);
            UndoBuffer.Push(undoItem);
        }

        private void PreserveRemovedVoter(Identity voter1, IEnumerable<VotePartition> voter1Votes, VoteType type)
        {
            var undoItem = new UndoItem(UndoItemType.RemoveVoter, type, voter1: voter1, votes1: voter1Votes);
            UndoBuffer.Push(undoItem);
        }

        /// <summary>
        /// Undo the top action on the undo buffer.
        /// </summary>
        public void Undo()
        {
            if (UndoBuffer.Count == 0)
                return;

            var undoItem = UndoBuffer.Pop();

            switch (undoItem.UndoType)
            {
                case UndoItemType.Merge:
                    UndoMerge(undoItem);
                    break;
                case UndoItemType.Join:
                    UndoJoin(undoItem);
                    break;
                case UndoItemType.RemoveVote:
                    UndoRemoveVote(undoItem);
                    break;
                case UndoItemType.RemoveVoter:
                    UndoRemoveVoter(undoItem);
                    break;
                default:
                    break;
            }

            OnPropertyChanged("Votes");
            OnPropertyChanged("Voters");
        }

        private void UndoMerge(UndoItem undo)
        {
            var votes = GetVoteEntries(undo.VoteType);

            votes[undo.Vote1].UnionWith(undo.Voters1);
            votes[undo.Vote2].Clear();
            votes[undo.Vote2].UnionWith(undo.Voters2);
        }

        private void UndoJoin(UndoItem undo)
        {
            var votes = GetVoteEntries(undo.VoteType);

            foreach (var vote in votes)
            {
                if (undo.Votes1.Contains(vote.Key))
                    vote.Value.Add(undo.Voter1);
                else
                    vote.Value.Remove(undo.Voter1);
            }
        }

        private void UndoRemoveVote(UndoItem undo)
        {
            var votes = GetVoteEntries(undo.VoteType);

            votes[undo.Vote1].UnionWith(undo.Voters1);
        }

        private void UndoRemoveVoter(UndoItem undo)
        {
            var votes = GetVoteEntries(undo.VoteType);

            foreach (var vote in undo.Votes1)
            {
                votes[vote].Add(undo.Voter1);
            }
        }

        #endregion

        #endregion


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
    }
}
