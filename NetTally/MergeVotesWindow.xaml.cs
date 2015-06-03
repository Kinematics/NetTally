using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MergeVotesWindow.xaml
    /// </summary>
    public partial class MergeVotesWindow : Window
    {
        public IVoteCounter voteCounter;

        public ObservableCollection<string> ObservableVotes { get; }
        public ICollectionView VoteCollectionView1 { get; }
        public ICollectionView VoteCollectionView2 { get; }

        public ObservableCollection<string> Voters1 { get; }
        public ObservableCollection<string> Voters2 { get; }
        public ICollectionView VoterView1 { get; }
        public ICollectionView VoterView2 { get; }

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

            ObservableVotes = new ObservableCollection<string>(voteCounter.VotesWithSupporters.Keys);

            VoteCollectionView1 = new CollectionView(ObservableVotes);
            VoteCollectionView2 = new CollectionView(ObservableVotes);

            Voters1 = new ObservableCollection<string>();
            Voters2 = new ObservableCollection<string>();
            VoterView1 = new CollectionView(Voters1);
            VoterView2 = new CollectionView(Voters2);

            this.DataContext = this;
        }

        /// <summary>
        /// Returns whether or not it's valid to merge votes based on the current list selections.
        /// </summary>
        public bool VotesCanMerge
        {
            get
            {
                if (votesFromListBox.SelectedIndex < 0)
                    return false;
                if (votesToListBox.SelectedIndex < 0)
                    return false;

                return (votesFromListBox.SelectedItem.ToString() != votesToListBox.SelectedItem.ToString());
            }
        }

        public bool HasRankedVotes
        {
            get
            {
                return voteCounter.HasRankedVotes;
            }
        }

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
                if (voteCounter.Merge(fromVote, toVote, VoteType.Vote))
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

            var voters = voteCounter.VotesWithSupporters[vote];

            if (voters != null)
            {
                foreach (var voter in voters)
                {
                    votersCollection.Add(voter);
                }
            }
        }
    }
}
