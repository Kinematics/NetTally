using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    using PlanDictionary = Dictionary<string, Plan>;

    using SupportersList = List<string>;
    using VoteSupporters = Dictionary<string, List<string>>;
    using VoteEntry = KeyValuePair<string, List<string>>;
    using FragmentsLists = Dictionary<VoteType, IEnumerable<string>>;

    using VoteFragments = List<VoteFragment>;

    public class VotingRecords : INotifyPropertyChanged
    {
        #region Lazy singleton creation
        static readonly Lazy<VotingRecords> lazy = new Lazy<VotingRecords>(() => new VotingRecords());

        public static VotingRecords Instance => lazy.Value;

        VotingRecords()
        {
            ResetVotesAndSupporters();
        }
        #endregion

        /// <summary>
        /// Reference dictionaries allow translating between canonical names and user-entered names.
        /// </summary>
        Dictionary<string, string> ReferenceVoterNames { get; } = new Dictionary<string, string>(Agnostic.StringComparer);
        Dictionary<string, string> ReferenceVoterPosts { get; } = new Dictionary<string, string>();
        Dictionary<string, string> ReferencePlanNames { get; } = new Dictionary<string, string>(Agnostic.StringComparer);

        Dictionary<VoteType, VoteSupporters> VotesAndSupporters { get; } = new Dictionary<VoteType, VoteSupporters>();

        Dictionary<string, string> VoterPostIDs { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        HashSet<string> TallyTasks { get; } = new HashSet<string>();
        HashSet<string> UserTasks { get; } = new HashSet<string>();

        public IEnumerable<string> Tasks => TallyTasks.Union(UserTasks);


        #region Reset
        public void Reset()
        {
            ReferenceVoterNames.Clear();
            ReferencePlanNames.Clear();
            VoterPostIDs.Clear();

            TallyTasks.Clear();

            ResetVotesAndSupporters();

        }

        public void ResetUserDefinedTasks(string forQuestName)
        {
            UserTasks.Clear();
        }

        private void ResetVotesAndSupporters()
        {
            if (VotesAndSupporters.Count == 0 || VotesAndSupporters.First().Value.Comparer != Agnostic.StringComparer)
            {
                foreach (VoteType vt in Enum.GetValues(typeof(VoteType)))
                {
                    VotesAndSupporters[vt] = new VoteSupporters(Agnostic.StringComparer);
                }
            }
            else
            {
                foreach (var v in VotesAndSupporters)
                {
                    v.Value.Clear();
                }
            }
        }

        #endregion

        #region Prep
        public void AddPlans(PlanDictionary planRepo)
        {
            var checkForVariants = planRepo.GroupBy(p => p.Value.Name);
            foreach (var plan in planRepo)
            {
                AddPlanName(plan.Key);

                List<VoteFragment> fragments = new VoteFragments { new VoteFragment(VoteType.Plan, plan.Value.Name) };

                AddVoteFragments(fragments, plan.Key, plan.Value.Vote.Post.Identity.PostID);
            }
        }
        #endregion

        #region General methods

        #region Voter names (add, delete, query)
        /// <summary>
        /// Add the specified voter name to the list of known voters.
        /// </summary>
        /// <param name="voterName">The voter name to add.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddVoterRecord(Identity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            ReferenceVoterNames.Add(identity.Name, identity.Name);
            ReferenceVoterPosts.Add(identity.Name, identity.PostID);
        }

        /// <summary>
        /// Checks whether the supplied voter name exists in the list of known voters.
        /// This finds names using the current comparison settings, usually ignoring
        /// differences in whitespace, punctuation, etc.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for. Does not need to be exact.</param>
        /// <returns>Returns true if the voter name is known to exist.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool HasVoterName(string voterName)
        {
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));

            return ReferenceVoterNames.Keys.Contains(voterName);
        }

        /// <summary>
        /// Gets the canonical voter name that matches the requested voter name.
        /// </summary>
        /// <param name="voterName">The name of the voter to check for. Does not need to be exact.</param>
        /// <param name="canonicalName">Returns the canonical version of the name, if found.</param>
        /// <returns>Returns true if the requested name is found.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool TryGetVoterName(string voterName, out string canonicalName)
        {
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));

            return ReferenceVoterNames.TryGetValue(voterName, out canonicalName);
        }

        public string GetVoterName(string voterName)
        {
            if (TryGetVoterName(voterName, out string canonicalName))
                return canonicalName;

            return null;
        }

        /// <summary>
        /// Deletes the specified voter name from the list of known voters.
        /// </summary>
        /// <param name="voterName">The voter name to delete.</param>
        /// <returns>Returns true if the voter name was deleted.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool DeleteVoterName(string voterName)
        {
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));

            return ReferenceVoterNames.Remove(voterName);
        }
        #endregion

        #region Plan names (add, delete, query)
        /// <summary>
        /// Add the specified plan name to the list of known plans.
        /// </summary>
        /// <param name="planName">The plan name to add.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddPlanName(string planName)
        {
            if (string.IsNullOrEmpty(planName))
                throw new ArgumentNullException(nameof(planName));

            ReferencePlanNames.Add(planName, planName);
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

            return ReferencePlanNames.Keys.Contains(planName);
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

            return ReferencePlanNames.TryGetValue(planName, out canonicalName);
        }

        public string GetPlanName(string planName)
        {
            if (TryGetPlanName(planName, out string canonicalName))
                return canonicalName;

            return null;
        }


        /// <summary>
        /// Deletes the specified plan name from the list of known plans.
        /// </summary>
        /// <param name="planName">The plan name to delete.</param>
        /// <returns>Returns true if the plan name was deleted.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool DeletePlanName(string planName)
        {
            if (string.IsNullOrEmpty(planName))
                throw new ArgumentNullException(nameof(planName));

            return ReferencePlanNames.Remove(planName);
        }
        #endregion

        #region Post IDs (add, query)
        /// <summary>
        /// Add the latest post ID for the specified voter.
        /// </summary>
        /// <param name="voterName">The name of the voter.</param>
        /// <param name="postID">The ID of the last post the voter made.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddVoterPostID(string voterName, string postID)
        {
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            VoterPostIDs[voterName] = postID;
        }

        /// <summary>
        /// Try to get the post ID of the last post made by the specified voter.
        /// </summary>
        /// <param name="voterName">The voter whose post is being asked for.</param>
        /// <param name="postID">The ID of the last post made by the specified voter.</param>
        /// <returns>Returns true if the post ID for the voter was found.</returns>
        /// <exception cref="ArgumentNullException"/>
        public bool TryGetVoterPostID(string voterName, out string postID)
        {
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));

            return VoterPostIDs.TryGetValue(voterName, out postID);
        }
        #endregion

        #region Vote Fragments
        /// <summary>
        /// Add any number of vote fragments for a given voter, from a given post.
        /// </summary>
        /// <param name="voteFragments">A list of vote fragments.</param>
        /// <param name="voterName">The voter.</param>
        /// <param name="postID">The post the vote fragments are from.</param>
        /// <exception cref="ArgumentNullException"/>
        public void AddVoteFragments(List<VoteFragment> voteFragments, string voterName, string postID)
        {
            if (voteFragments == null)
                throw new ArgumentNullException(nameof(voteFragments));
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));
            if (string.IsNullOrEmpty(postID))
                throw new ArgumentNullException(nameof(postID));

            if (!voteFragments.Any())
                return;

            RemoveVoterSupport(voterName);
            AddVoterPostID(voterName, postID);

            foreach (var fragment in voteFragments)
            {

                AddVoteFragment(fragment.Fragment, fragment.VoteType, voterName);
                AddFragmentTask(fragment.Fragment);
            }
        }
        #endregion

        private void AddVoteFragment(string voteFragment, VoteType voteType, string voterName)
        {
            if (string.IsNullOrEmpty(voteFragment))
                throw new ArgumentNullException(nameof(voteFragment));
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));

            GetVoteSupporters(voteFragment, voteType).Add(voterName);
            OnPropertyChanged("Voters");
        }

        private bool RemoveVoterSupport(string voterName)
        {
            if (string.IsNullOrEmpty(voterName))
                throw new ArgumentNullException(nameof(voterName));

            bool removedAny = false;

            foreach (var voteType in VotesAndSupporters)
            {
                foreach (var vote in voteType.Value)
                {
                    if (vote.Value.Remove(voterName))
                        removedAny = true;
                }
            }

            if (removedAny)
                OnPropertyChanged("Voters");

            return removedAny;
        }

        private void AddFragmentTask(string fragment)
        {
            string task = VoteString.GetVoteTask(fragment);
            if (!string.IsNullOrEmpty(task))
                TallyTasks.Add(task);
        }


        private SupportersList GetVoteSupporters(string voteFragment, VoteType voteType)
        {
            VoteSupporters votes = VotesAndSupporters[voteType];

            if (votes.TryGetValue(voteFragment, out var voteSupporters))
            {
                return voteSupporters;
            }

            votes[voteFragment] = new SupportersList();
            OnPropertyChanged("Votes");

            return votes[voteFragment];
        }





        public void AddVote()
        {

        }

        public void DeleteVote()
        {

        }

        public void MergeVotes()
        {

        }

        public void RenameVote()
        {

        }

        public void JoinVoters()
        {

        }

        public void Undo()
        {

        }
        #endregion

        #region Query methods
        public bool HasVote(string voteFragment, VoteType voteType)
        {
            VoteSupporters votes = VotesAndSupporters[voteType];

            return votes.ContainsKey(voteFragment);
        }

        public bool HasVoter(string voterName)
        {
            foreach (var vt in VotesAndSupporters)
            {
                foreach (var vote in vt.Value)
                {
                    if (vote.Value.Contains(voterName))
                        return true;
                }
            }

            return false;
        }

        public string GetVote()
        {
            return null;
        }

        public SupportersList GetVoters(string voteFragment)
        {
            return null;
        }
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
