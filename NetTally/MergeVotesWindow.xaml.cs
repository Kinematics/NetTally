using System;
using System.Collections.ObjectModel;
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

        public ObservableCollection<string> ObservableVotes { get; }
        public ICollectionView VoteCollectionView1 { get; }
        public ICollectionView VoteCollectionView2 { get; }

        public ObservableCollection<string> Voters1 { get; }
        public ObservableCollection<string> Voters2 { get; }
        public ICollectionView VoterView1 { get; }
        public ICollectionView VoterView2 { get; }

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

            ObservableVotes = new ObservableCollection<string>(voteCounter.GetVotesCollection(CurrentVoteType).
                Keys.OrderBy(v => VoteLine.GetVoteContent(v), StringComparer.OrdinalIgnoreCase));

            VoteCollectionView1 = new CollectionView(ObservableVotes);
            VoteCollectionView2 = new CollectionView(ObservableVotes);

            Voters1 = new ObservableCollection<string>();
            Voters2 = new ObservableCollection<string>();
            VoterView1 = new CollectionView(Voters1);
            VoterView2 = new CollectionView(Voters2);

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
                // Can't merge if nothing is selected
                if (votesFromListBox.SelectedIndex < 0)
                    return false;
                if (votesToListBox.SelectedIndex < 0)
                    return false;

                string fromVote = votesFromListBox.SelectedItem.ToString();
                string toVote = votesToListBox.SelectedItem.ToString();

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

            string fromVote = votesFromListBox.SelectedItem.ToString();
            string toVote = votesToListBox.SelectedItem.ToString();

            try
            {
                if (voteCounter.Merge(fromVote, toVote, CurrentVoteType))
                {
                    ObservableVotes.Remove(fromVote);
                    UpdateVoters(votesToListBox, Voters2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            merge.IsEnabled = VotesCanMerge;
            UpdateVoters(sender as ListBox, Voters1);
        }

        /// <summary>
        /// Update enabled state of merge button, and current list of voters, based on current vote selection
        /// for the list of votes to be merged to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void votesToListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            merge.IsEnabled = VotesCanMerge;
            UpdateVoters(sender as ListBox, Voters2);
        }
        #endregion


        /// <summary>
        /// Update the collection that the voter list boxes are observing.
        /// </summary>
        /// <param name="votesBox">The list box that we're checking the currently selected vote in.</param>
        /// <param name="votersCollection">The voter collection being observed by the associated voter list box.</param>
        private void UpdateVoters(ListBox votesBox, ObservableCollection<string> votersCollection)
        {
            if (votesBox == null || votersCollection == null)
                return;

            votersCollection.Clear();

            if (votesBox.SelectedIndex < 0)
            {
                return;
            }

            string vote = votesBox.SelectedItem.ToString();

            var voters = voteCounter.GetVotesCollection(CurrentVoteType)[vote];

            if (voters != null)
            {
                foreach (var voter in voters)
                {
                    votersCollection.Add(voter);
                }
            }
        }

        /// <summary>
        /// Updated the observed collection when the vote display mode is changed.
        /// </summary>
        private void ChangeVotesDisplayed()
        {
            ObservableVotes.Clear();

            var votes = voteCounter.GetVotesCollection(CurrentVoteType).Keys;

            IOrderedEnumerable<string> orderedVotes;

            if (CurrentVoteType == VoteType.Rank)
                orderedVotes = votes.OrderBy(v => BracketTask(VoteLine.GetVoteTask(v)) + VoteLine.GetVoteContent(v) + VoteLine.GetVoteMarker(v), StringComparer.OrdinalIgnoreCase);
            else
                orderedVotes = votes.OrderBy(v => VoteLine.GetVoteContent(v), StringComparer.OrdinalIgnoreCase);

            foreach (var vote in orderedVotes)
            {
                ObservableVotes.Add(vote);
            }
        }

        /// <summary>
        /// Convert the empty string to a space for the purposes of sorting ranked entries.
        /// </summary>
        /// <param name="task">The task for the vote</param>
        /// <returns>Returns the task, or a space if the task is empty.</returns>
        private string BracketTask(string task)
        {
            if (task == string.Empty)
                return " ";
            else
                return task;
        }
    }
}
