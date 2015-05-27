using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MergeVotesWindow.xaml
    /// </summary>
    public partial class MergeVotesWindow : Window
    {
        IVoteCounter voteCounter;

        public ICollectionView VoteCollectionView1 { get; }
        public ICollectionView VoteCollectionView2 { get; }
        public ObservableCollection<string> ObservableVotes { get; }

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
            //ObservableVotes = new ObservableCollection<Dictionary<string, HashSet<string>>>(voteCounter.VotesWithSupporters);

            VoteCollectionView1 = new CollectionView(ObservableVotes);
            VoteCollectionView2 = new CollectionView(ObservableVotes);


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
            if (votesFromListBox.SelectedIndex < 0)
                return;
            if (votesToListBox.SelectedIndex < 0)
                return;

            string fromVote = votesFromListBox.SelectedItem.ToString();
            string toVote = votesToListBox.SelectedItem.ToString();

            try
            {
                if (voteCounter.Merge(fromVote, toVote))
                {
                    ObservableVotes.Remove(fromVote);
                    //ShowVoters(votesToListBox);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            merge.IsEnabled = VotesCanMerge;
            //ShowVoters(sender as ListBox);

        }

        private void ShowVoters(ListBox listBox)
        {
            if (listBox == null)
                return;

            ListBox voterListBox = votersFromListBox;
            if (listBox == votesToListBox)
                voterListBox = votersToListBox;


            //voterListBox.Items.Clear();

            if (listBox.SelectedIndex < 0)
            {
                return;
            }

            string vote = listBox.SelectedItem.ToString();
            HashSet<string> voters;
            if (voteCounter.VotesWithSupporters.TryGetValue(vote, out voters))
            {
                voterListBox.ItemsSource = voters;
            }

        }
    }
}
