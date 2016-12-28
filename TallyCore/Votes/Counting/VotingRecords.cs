using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NetTally.Votes.Experiment
{
    public class VotingRecords : INotifyPropertyChanged
    {
        public static HashSet<string> ReferenceVoters { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Dictionary<string, string> ReferenceVoterPosts { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        #region Reset
        public static void Reset()
        {
            ReferenceVoters.Clear();
            ReferenceVoterPosts.Clear();

        }

        public void ResetUserDefinedTasks(string forQuestName)
        {

        }
        #endregion

        #region Prep
        internal static void AddPlans(Dictionary<string, (PlanType planType, string plan, string postId)> planRepo)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region General methods
        internal static void AddVoter(string voterName, string postID)
        {
            ReferenceVoters.Add(voterName);
            ReferenceVoterPosts[voterName] = postID;
        }

        internal static void DeleteVoter()
        {

        }

        internal static void AddVotes()
        {

        }

        internal static void AddVote()
        {

        }

        internal static void DeleteVote()
        {

        }

        internal static void MergeVotes()
        {

        }

        internal static void RenameVote()
        {

        }

        internal static void JoinVoters()
        {

        }

        internal static void Undo()
        {

        }
        #endregion

        #region Query methods
        internal bool HasVote()
        {
            return false;
        }

        internal bool HasVoter()
        {
            return false;
        }

        internal string GetVote()
        {
            return null;
        }

        internal List<string> GetVoters()
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
