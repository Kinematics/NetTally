using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NetTally.Utility;

namespace NetTally.Votes.Experiment
{
    using PlanDictionary = Dictionary<string, (PlanType planType, string plan, string postId)>;

    public class VotingRecords : INotifyPropertyChanged
    {
        #region Lazy singleton creation
        static readonly Lazy<VotingRecords> lazy = new Lazy<VotingRecords>(() => new VotingRecords());

        public static VotingRecords Instance => lazy.Value;

        VotingRecords()
        {
        }
        #endregion


        static Dictionary<string, string> ReferenceVoters { get; } = new Dictionary<string, string>(Agnostic.StringComparer);

        #region Reset
        public void Reset()
        {
            ReferenceVoters.Clear();
        }

        public void ResetUserDefinedTasks(string forQuestName)
        {

        }
        #endregion

        #region Prep
        public void AddPlans(PlanDictionary planRepo)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region General methods
        public void AddVoter(string voterName)
        {
            ReferenceVoters.Add(voterName, voterName);
        }

        public bool HasVoter(string suppliedName)
        {
            return ReferenceVoters.Keys.Contains(suppliedName);
        }

        public string GetVoterName(string suppliedName)
        {
            if (ReferenceVoters.TryGetValue(suppliedName, out string refName))
            {
                return refName;
            }

            return null;
        }

        public void DeleteVoter(string voterName)
        {

        }

        public void AddVoteFragments(IEnumerable<string> voteParts, string voterName, string postID, VoteType voteType)
        {

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
        public bool HasVote()
        {
            return false;
        }

        public bool HasVoter()
        {
            return false;
        }

        public string GetVote()
        {
            return null;
        }

        public List<string> GetVoters()
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
