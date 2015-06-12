using System;
using System.Collections;
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
        public ListCollectionView VoteView1 { get; }
        public ListCollectionView VoteView2 { get; }

        public ObservableCollection<string> VoterCollection { get; }
        public ListCollectionView VoterView1 { get; }
        public ListCollectionView VoterView2 { get; }

        bool displayStandardVotes = true;

        List<string> Tasks { get; } = new List<string>();
        ListBox newTaskBox = null;


        /// <summary>
        /// Default constructor
        /// </summary>
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
            List<string> votes = voteCounter.VotesWithSupporters.Keys
                .Concat(voteCounter.RankedVotesWithSupporters.Keys)
                .Distinct().ToList();

            // Create a collection for the views to draw from.
            VoteCollection = new ObservableCollection<string>(votes);

            // Create filtered, sortable views into the collection for display in the window.
            VoteView1 = new ListCollectionView(VoteCollection);
            VoteView2 = new ListCollectionView(VoteCollection);

            if (VoteView1.CanSort)
            {
                IComparer voteCompare = new Utility.CustomVoteSort();
                VoteView1.CustomSort = voteCompare;
                VoteView2.CustomSort = voteCompare;
            }

            if (VoteView1.CanFilter)
            {
                VoteView1.Filter = (a) => FilterVotes(a.ToString());
                VoteView2.Filter = (a) => FilterVotes(a.ToString());
            }

            // Initialize starting selected positions
            VoteView1.MoveCurrentToPosition(-1);
            VoteView2.MoveCurrentToFirst();


            // Get the lists of all unique voters/ranked voters that we can show in the display.
            List<string> voters = voteCounter.VoterMessageId.Select(v => v.Key).Except(voteCounter.PlanNames)
                .Concat(voteCounter.RankedVoterMessageId.Select(v => v.Key))
                .Distinct().OrderBy(v => v).ToList();

            // Create a collection for the views to draw from.
            VoterCollection = new ObservableCollection<string>(voters);

            // Create filtered views for display in the window.
            VoterView1 = new ListCollectionView(VoterCollection);
            VoterView2 = new ListCollectionView(VoterCollection);
            VoterView1.Filter = (a) => FilterVoters(VoteView1, a.ToString());
            VoterView2.Filter = (a) => FilterVoters(VoteView2, a.ToString());

            // Update the voters to match the votes.
            VoterView1.Refresh();
            VoterView2.Refresh();

            // Populate the context menu with known tasks.
            InitContextMenuTasks();
            CreateContextMenu();

            // Set the data context for binding.
            DataContext = this;
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

        #region Context Menu events
        private void newTask_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                ContextMenu cm = mi.Parent as ContextMenu;
                if (cm != null)
                {
                    newTaskBox = cm.PlacementTarget as ListBox;
                }
            }

            // Show the custom input box, and put focus on the text box.
            InputBox.Visibility = Visibility.Visible;
            InputTextBox.Focus();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            AcceptInput();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            CancelInput();
        }

        private void InputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter:
                    AcceptInput();
                    break;
                case System.Windows.Input.Key.Escape:
                    CancelInput();
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }

        private void AcceptInput()
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.Visibility = Visibility.Collapsed;

            string newTask = InputTextBox.Text;

            // Clear InputBox.
            InputTextBox.Text = String.Empty;

            // Do something with the Input
            AddTaskToContextMenu(newTask);

            // Update the selected item of the list box

            string selectedVote = newTaskBox?.SelectedItem?.ToString();

            if (selectedVote != null)
            {
                string changedVote = VoteLine.ReplaceTask(selectedVote, newTask);

                if (voteCounter.Rename(selectedVote, changedVote, CurrentVoteType))
                {
                    if (!VoteCollection.Contains(changedVote))
                        VoteCollection.Add(changedVote);

                    VoteView1.Refresh();
                    VoteView2.Refresh();

                    newTaskBox.SelectedItem = changedVote;
                }

            }

            newTaskBox = null;
        }

        private void CancelInput()
        {
            // NoButton Clicked! Let's hide our InputBox.
            InputBox.Visibility = Visibility.Collapsed;

            // Clear InputBox.
            InputTextBox.Text = String.Empty;

            newTaskBox = null;
        }

        private void modifyTask_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                ContextMenu cm = mi.Parent as ContextMenu;
                if (cm != null)
                {
                    ListBox box = cm.PlacementTarget as ListBox;
                    if (box != null)
                    {
                        string selectedVote = box.SelectedItem?.ToString();

                        if (selectedVote != null)
                        {
                            string changedVote = "";

                            if (mi.Header.ToString() == "Clear Task")
                                changedVote = VoteLine.ReplaceTask(selectedVote, "");
                            else
                                changedVote = VoteLine.ReplaceTask(selectedVote, mi.Header.ToString());

                            if (voteCounter.Rename(selectedVote, changedVote, CurrentVoteType))
                            {
                                if (!VoteCollection.Contains(changedVote))
                                    VoteCollection.Add(changedVote);

                                VoteView1.Refresh();
                                VoteView2.Refresh();

                                box.SelectedItem = changedVote;
                            }
                        }
                    }
                }
            }
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


        #region Context Menu Utility

        /// <summary>
        /// Initialize the context menu with known tasks on window startup.
        /// </summary>
        private void InitContextMenuTasks()
        {
            var voteTasks = voteCounter.GetVotesCollection(CurrentVoteType).Keys.
                Select(v => VoteLine.GetVoteTask(v)).Distinct().
                Where(v => v != string.Empty).OrderBy(v => v);

            Tasks.AddRange(voteTasks);
        }

        /// <summary>
        /// Function to create a MenuItem object for the context menu containing the provided header value.
        /// </summary>
        /// <param name="name">The name of the menu item.</param>
        /// <returns>Returns a MenuItem object with appropriate tooltip and click handler.</returns>
        private MenuItem GetContextMenuItem(string name)
        {
            MenuItem mi = new MenuItem();
            mi.Header = name;
            mi.Click += modifyTask_Click;
            mi.ToolTip = string.Format("Change the task for the selected item to '{0}'", mi.Header);
            mi.Tag = "NamedTask";

            return mi;
        }

        /// <summary>
        /// Create the entries of the basic context menu.
        /// </summary>
        private void CreateContextMenu()
        {
            var pMenu = (ContextMenu)this.Resources["TaskContextMenu"];
            if (pMenu != null)
            {
                pMenu.Items.Clear();

                MenuItem newTask = new MenuItem();
                newTask.Header = "New Task...";
                newTask.Click += newTask_Click;
                newTask.ToolTip = "Create a new task value.";
                pMenu.Items.Add(newTask);

                MenuItem clearTask = new MenuItem();
                clearTask.Header = "Clear Task";
                clearTask.Click += modifyTask_Click;
                clearTask.ToolTip = "Clear the task from the currently selected vote.";
                pMenu.Items.Add(clearTask);

                pMenu.Items.Add(new Separator());

                foreach (var task in Tasks)
                {
                    MenuItem mi = GetContextMenuItem(task);
                    pMenu.Items.Add(mi);
                }
            }
        }

        /// <summary>
        /// Given a new task name, add a new MenuItem to the context menu.
        /// </summary>
        /// <param name="task">The name of the new context menu item.</param>
        private void AddTaskToContextMenu(string task)
        {
            if (task == null || task == string.Empty)
                return;

            if (Tasks.Any(t => t == task))
                return;

            var pMenu = (ContextMenu)this.Resources["TaskContextMenu"];

            int priorIndex = -1;

            foreach (var menuItem in pMenu.Items)
            {
                MenuItem m = menuItem as MenuItem;

                if (m != null)
                {
                    if ((string)(m.Tag) == "NamedTask")
                    {
                        if (string.Compare(m.Header.ToString(), task) < 0)
                            priorIndex = pMenu.Items.IndexOf(menuItem);
                    }
                }
            }

            MenuItem mi = GetContextMenuItem(task);

            if (priorIndex > 0)
            {
                pMenu.Items.Insert(priorIndex + 1, mi);
            }
            else
            {
                pMenu.Items.Add(mi);
            }

            Tasks.Add(task);
        }

        #endregion

    }
}
