using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MergeVotesWindow.xaml
    /// </summary>
    public partial class MergeVotesWindow : Window, INotifyPropertyChanged
    {
        #region Constructor and variables
        public IVoteCounter voteCounter;

        public ObservableCollection<string> VoteCollection { get; }
        public ICollectionView VoteView1 { get; }
        public ICollectionView VoteView2 { get; }

        public ObservableCollection<string> VoterCollection { get; }
        public ICollectionView VoterView1 { get; }
        public ICollectionView VoterView2 { get; }

        List<string> Voters { get; }
        List<string> Votes { get; }

        bool displayStandardVotes = true;

        public MergeVotesWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tally">The tally that is having votes merged.</param>
        public MergeVotesWindow(Tally tally)
        {
            InitializeComponent();

            voteCounter = tally.VoteCounter;

            // Gets the lists of all current votes and ranked votes that can be shown.
            Votes = voteCounter.VotesWithSupporters.Keys
                .Concat(voteCounter.RankedVotesWithSupporters.Keys)
                .Distinct().OrderBy(v => VoteLine.GetVoteTask(v) + " " + VoteLine.GetVoteContent(v), StringComparer.OrdinalIgnoreCase)
                .ToList();

            VoteCollection = new ObservableCollection<string>(Votes);

            VoteView1 = new ListCollectionView(VoteCollection);
            VoteView2 = new ListCollectionView(VoteCollection);
            VoteView1.Filter = (a) => FilterVotes(a.ToString());
            VoteView2.Filter = (a) => FilterVotes(a.ToString());


            // Get the lists of all unique voters/ranked voters that we can show in the display.
            Voters = voteCounter.VoterMessageId.Select(v => v.Key).Except(voteCounter.PlanNames)
                .Concat(voteCounter.RankedVoterMessageId.Select(v => v.Key))
                .Distinct().OrderBy(v => v).ToList();

            VoterCollection = new ObservableCollection<string>(Voters);

            VoterView1 = new ListCollectionView(VoterCollection);
            VoterView2 = new ListCollectionView(VoterCollection);
            VoterView1.Filter = (a) => FilterVoters(VoteView1, a.ToString());
            VoterView2.Filter = (a) => FilterVoters(VoteView2, a.ToString());

            VoterView1.Refresh();
            VoterView2.Refresh();


            this.DataContext = this;
        }

        #endregion

        #region Event handling
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

        #region Properties

        /// <summary>
        /// Returns whether or not it's valid to merge votes based on the current list selections.
        /// </summary>
        public bool VotesCanMerge
        {
            get
            {
                using (var r = new Utility.RegionProfiler("VotesCanMerge"))
                {
                    // Can't merge if nothing is selected
                    if (VoteView1.CurrentItem == null || VoteView2.CurrentItem == null)
                        return false;

                    string fromVote = VoteView1.CurrentItem.ToString();
                    string toVote = VoteView2.CurrentItem.ToString();

                    if (CurrentVoteType == VoteType.Rank)
                    {
                        // Don't allow merging if they're not the same rank.

                        string markFrom = VoteLine.GetVoteMarker(fromVote);
                        string markTo = VoteLine.GetVoteMarker(toVote);

                        if (markFrom != markTo)
                            return false;

                        // Don't allow merging if they're not the same task.

                        string taskFrom = VoteLine.GetVoteTask(fromVote);
                        string taskTo = VoteLine.GetVoteTask(toVote);

                        if (taskFrom != taskTo)
                            return false;
                    }

                    // Otherwise, allow merge if they're not the same
                    return (fromVote != toVote);
                }
            }
        }

        /// <summary>
        /// Returns whether there are ranked votes available in the vote tally.
        /// </summary>
        public bool HasRankedVotes
        {
            get
            {
                return voteCounter.HasRankedVotes;
            }
        }

        /// <summary>
        /// Flag whether we should be displaying standard votes or ranked votes.
        /// </summary>
        public bool DisplayStandardVotes
        {
            get
            {
                return displayStandardVotes;
            }
            set
            {
                displayStandardVotes = value;
                ChangeVotesDisplayed();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Get the VoteType enum value that corresponds to the current display.
        /// </summary>
        public VoteType CurrentVoteType
        {
            get
            {
                if (DisplayStandardVotes)
                    return VoteType.Vote;
                else
                    return VoteType.Rank;
            }
        }
        #endregion

        #region Window events
        /// <summary>
        /// Handler for the button to merge two vote items together.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void merge_Click(object sender, RoutedEventArgs e)
        {
            if (!VotesCanMerge)
                return;

            string fromVote = VoteView1.CurrentItem?.ToString();
            string toVote = VoteView2.CurrentItem?.ToString();

            try
            {
                if (voteCounter.Merge(fromVote, toVote, CurrentVoteType))
                {
                    VoteCollection.Remove(fromVote);
                    VoterView1.Refresh();
                    VoterView2.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Delete the vote that has been selected in both list boxes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delete_Click(object sender, RoutedEventArgs e)
        {
            if (voteCounter.Delete(VoteView1.CurrentItem?.ToString(), CurrentVoteType))
            {
                VoteView1.Refresh();
                VoteView2.Refresh();
                VoteView1.MoveCurrentToPosition(-1);
                VoteView2.MoveCurrentToFirst();

            }
        }

        /// <summary>
        /// Update enabled state of merge button, and current list of voters, based on current vote selection
        /// for the list of votes to be merged from.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void votesFromListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VoterView1.Refresh();
            merge.IsEnabled = VotesCanMerge;
        }

        /// <summary>
        /// Update enabled state of merge button, and current list of voters, based on current vote selection
        /// for the list of votes to be merged to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void votesToListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VoterView2.Refresh();
            merge.IsEnabled = VotesCanMerge;
        }
        #endregion

        #region Utility functions
        /// <summary>
        /// Filter to be used by a collection view to determine which votes should
        /// be displayed in the main list box.
        /// </summary>
        /// <param name="vote">The vote to be checked.</param>
        /// <returns>Returns true if the vote is valid for the current vote type.</returns>
        private bool FilterVotes(string vote)
        {
            var votes = voteCounter.GetVotesCollection(CurrentVoteType);
            return votes.ContainsKey(vote);
        }

        /// <summary>
        /// Filter to be used by a collection view to determine which voters should
        /// be displayed in the voter list box, for each vote that is selected.
        /// </summary>
        /// <param name="voteView">The view of the main vote box.</param>
        /// <param name="voterName">The name of the voter being checked.</param>
        /// <returns>Returns true if that voter supports the currently selected
        /// vote in the vote view.</returns>
        private bool FilterVoters(ICollectionView voteView, string voterName)
        {
            if (voteView.IsEmpty)
                return false;

            if (voteView.CurrentItem == null)
                return false;

            var votes = voteCounter.GetVotesCollection(CurrentVoteType);
            HashSet<string> voterList;

            if (votes.TryGetValue(voteView.CurrentItem.ToString(), out voterList))
            {
                if (voterList == null)
                    return false;

                return voterList.Contains(voterName);
            }

            return false;
        }

        /// <summary>
        /// Updated the observed collection when the vote display mode is changed.
        /// </summary>
        private void ChangeVotesDisplayed()
        {
            VoteView1.Refresh();
            VoteView2.Refresh();
            VoteView1.MoveCurrentToFirst();
            VoteView2.MoveCurrentToFirst();
        }
        #endregion
    }
}
