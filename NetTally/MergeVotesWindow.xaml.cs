using System;
using System.Collections.Generic;
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

        public ICollectionView VoteCollectionView1 { get; }
        public ICollectionView VoteCollectionView2 { get; }
        public ObservableCollection<string> ObservableVotes { get; }

        public ICollectionView VoterView1 { get; }
        public ICollectionView VoterView2 { get; }
        public ObservableCollection<string> Voters1 { get; }
        public ObservableCollection<string> Voters2 { get; }

        //public ObservableCollection<Dictionary<string, HashSet<string>>> ObservableVotes { get; }

        public MergeVotesWindow()
        {
            InitializeComponent();
        }

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

        private void merge_Click(object sender, RoutedEventArgs e)
        {
            if (!VotesCanMerge)
                return;

            string fromVote = votesFromListBox.SelectedItem.ToString();
            string toVote = votesToListBox.SelectedItem.ToString();

            try
            {
                if (voteCounter.Merge(fromVote, toVote))
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

        private void votesFromListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            merge.IsEnabled = VotesCanMerge;
            UpdateVoters(sender as ListBox, Voters1);
        }

        private void votesToListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            merge.IsEnabled = VotesCanMerge;
            UpdateVoters(sender as ListBox, Voters2);
        }

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
