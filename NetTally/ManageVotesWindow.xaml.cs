using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NetTally.Utility;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MergeVotesWindow.xaml
    /// </summary>
    public partial class ManageVotesWindow : Window, INotifyPropertyChanged
    {
        #region Constructor and variables
        public ListCollectionView VoteView1 { get; }
        public ListCollectionView VoteView2 { get; }

        public ListCollectionView VoterView1 { get; }
        public ListCollectionView VoterView2 { get; }

        object lastSelected2 = null;

        bool displayStandardVotes = true;

        List<MenuItem> ContextMenuCommands = new List<MenuItem>();
        List<MenuItem> ContextMenuTasks = new List<MenuItem>();

        MainViewModel MainViewModel { get; }

        ListBox newTaskBox = null;

        string filter1String;
        string filter2String;


        /// <summary>
        /// Default constructor
        /// </summary>
        public ManageVotesWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mainViewModel">The primary view model of the program.</param>
        public ManageVotesWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();

            MainViewModel = mainViewModel;

            MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

            // Create filtered, sortable views into the collection for display in the window.
            VoteView1 = new ListCollectionView(MainViewModel.AllVotesCollection);
            VoteView2 = new ListCollectionView(MainViewModel.AllVotesCollection);

            if (VoteView1.CanSort)
            {
                IComparer voteCompare = new CustomVoteSort();
                //IComparer voteCompare = StringComparer.InvariantCultureIgnoreCase;
                VoteView1.CustomSort = voteCompare;
                VoteView2.CustomSort = voteCompare;
            }

            if (VoteView1.CanFilter)
            {
                VoteView1.Filter = (a) => FilterVotes1(a.ToString());
                VoteView2.Filter = (a) => FilterVotes2(a.ToString());
            }

            // Initialize starting selected positions
            VoteView1.MoveCurrentToPosition(-1);
            VoteView2.MoveCurrentToFirst();


            // Create filtered views for display in the window.
            VoterView1 = new ListCollectionView(MainViewModel.AllVotersCollection);
            VoterView2 = new ListCollectionView(MainViewModel.AllVotersCollection);

            VoterView1.Filter = (a) => FilterVoters(VoteView1, a.ToString());
            VoterView2.Filter = (a) => FilterVoters(VoteView2, a.ToString());

            // Update the voters to match the votes.
            VoterView1.Refresh();
            VoterView2.Refresh();

            // Populate the context menu with known tasks.
            CreateContextMenuCommands();
            InitKnownTasks();
            UpdateContextMenu();

            // Set the data context for binding.
            DataContext = this;

            Filter1String = "";
            Filter2String = "";
        }
        #endregion

        #region INotifyPropertyChanged implementation
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
                    // Changing: If they're not the same rank, the merge just changes the text of the "from" vote to the "to" vote

                    // Don't allow merging if they're not the same task.

                    string taskFrom = VoteString.GetVoteTask(fromVote, CurrentVoteType);
                    string taskTo = VoteString.GetVoteTask(toVote, CurrentVoteType);

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
        public bool HasRankedVotes => MainViewModel.HasRankedVotes;

        /// <summary>
        /// Returns whether there are stored undo actions in the vote tally.
        /// </summary>
        public bool HasUndoActions => MainViewModel.HasUndoActions;

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

        /// <summary>
        /// Property for holding the string used to filter the 'from' votes.
        /// </summary>
        public string Filter1String
        {
            get
            {
                return filter1String;
            }
            set
            {
                filter1String = value.RemoveUnsafeCharacters();
                OnPropertyChanged();

                IsFilter1Empty = filter1String == string.Empty;
                OnPropertyChanged(nameof(IsFilter1Empty));

                VoteView1.Refresh();
            }
        }

        /// <summary>
        /// Property for holding the string used to filter the 'to' votes.
        /// </summary>
        public string Filter2String
        {
            get
            {
                return filter2String;
            }
            set
            {
                filter2String = value.RemoveUnsafeCharacters();
                OnPropertyChanged();

                IsFilter2Empty = filter2String == string.Empty;
                OnPropertyChanged(nameof(IsFilter2Empty));

                VoteView2.Refresh();
            }
        }

        /// <summary>
        /// Bool property for UI for if the first filter string is empty.
        /// </summary>
        public bool IsFilter1Empty { get; set; }

        /// <summary>
        /// Bool property for UI for if the second filter string is empty.
        /// </summary>
        public bool IsFilter2Empty { get; set; }
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

            MergeVotes(fromVote, toVote);
        }

        /// <summary>
        /// Handler for the button to join voters.
        /// All voters from the from list are adjusted to support all votes supported by the
        /// voter selected in the to list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void join_Click(object sender, RoutedEventArgs e)
        {
            if (VoteView1.Count == 0)
                return;

            if (VoterView2.CurrentItem == null)
                return;

            List<string> fromVoters = votersFromListBox.Items.SourceCollection.OfType<string>().ToList();
            string joinVoter = VoterView2.CurrentItem.ToString();

            try
            {
                if (MainViewModel.JoinVoters(fromVoters, joinVoter, CurrentVoteType))
                {
                    OnPropertyChanged(nameof(HasUndoActions));
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
            try
            {
                if (MainViewModel.DeleteVote(VoteView1.CurrentItem?.ToString(), CurrentVoteType))
                {
                    OnPropertyChanged(nameof(HasUndoActions));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Calls Undo on the vote counter to undo the most recent vote modification action.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void undo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainViewModel.UndoVoteModification())
                {
                    OnPropertyChanged(nameof(HasUndoActions));
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
            lastSelected2 = VoteView2.CurrentItem ?? lastSelected2;
            VoterView2.Refresh();
            merge.IsEnabled = VotesCanMerge;
        }
        #endregion

        #region Context Menu events
        private void newTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
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
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Escape:
                    CancelInput();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Process acceptance of the new task text.
        /// </summary>
        private void AcceptInput()
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.Visibility = Visibility.Collapsed;

            string newTask = InputTextBox.Text.RemoveUnsafeCharacters().Trim();

            // Clear InputBox.
            InputTextBox.Text = String.Empty;

            // Do something with the Input
            AddTaskToContextMenu(newTask);

            // Update the selected item of the list box

            string selectedVote = newTaskBox?.SelectedItem?.ToString();

            if (selectedVote != null)
            {
                string changedVote = VoteString.ReplaceTask(selectedVote, newTask, CurrentVoteType);

                MergeVotes(selectedVote, changedVote);

                newTaskBox.SelectedItem = changedVote;
            }

            newTaskBox = null;
        }

        /// <summary>
        /// Process rejecting the new task text.
        /// </summary>
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
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    if (cm.PlacementTarget is ListBox box)
                    {
                        if (box.SelectedItem?.ToString() is string selectedVote)
                        {
                            string changedVote = "";

                            if (mi.Header.ToString() == "Clear Task")
                                changedVote = VoteString.ReplaceTask(selectedVote, "", CurrentVoteType);
                            else
                                changedVote = VoteString.ReplaceTask(selectedVote, mi.Header.ToString(), CurrentVoteType);

                            MergeVotes(selectedVote, changedVote);

                            box.SelectedItem = changedVote;
                        }
                    }
                }
            }
        }
        #endregion

        #region Watched Events        
        /// <summary>
        /// Watch for notifications from the main view model about changes in the vote backend.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AllVotes" || e.PropertyName == "AllVoters")
            {
                UpdateCollections();
            }
        }
        #endregion

        #region Utility functions
        /// <summary>
        /// Filter to be used by a collection view to determine which votes should
        /// be displayed in the main (from) list box.
        /// </summary>
        /// <param name="vote">The vote to be checked.</param>
        /// <returns>Returns true if the vote is valid for the current vote type.</returns>
        bool FilterVotes1(string vote)
        {
            if (!MainViewModel.VoteExists(vote, CurrentVoteType))
                return false;

            if (string.IsNullOrEmpty(Filter1String))
                return true;

            if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(vote, Filter1String, CompareOptions.IgnoreCase) >= 0)
                return true;

            if (CurrentVoteType == VoteType.Vote)
            {
                var voters = MainViewModel.GetVoterListForVote(vote, CurrentVoteType);
                if (voters != null)
                {
                    if (voters.Any(voter => CultureInfo.InvariantCulture.CompareInfo.IndexOf(voter, Filter1String, CompareOptions.IgnoreCase) >= 0))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Filter to be used by a collection view to determine which votes should
        /// be displayed in the main (to) list box.
        /// </summary>
        /// <param name="vote">The vote to be checked.</param>
        /// <returns>Returns true if the vote is valid for the current vote type.</returns>
        bool FilterVotes2(string vote)
        {
            if (!MainViewModel.VoteExists(vote, CurrentVoteType))
                return false;

            if (string.IsNullOrEmpty(Filter2String))
                return true;

            if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(vote, Filter2String, CompareOptions.IgnoreCase) >= 0)
                return true;

            if (CurrentVoteType == VoteType.Vote)
            {
                var voters = MainViewModel.GetVoterListForVote(vote, CurrentVoteType);
                if (voters != null)
                {
                    if (voters.Any(voter => CultureInfo.InvariantCulture.CompareInfo.IndexOf(voter, Filter2String, CompareOptions.IgnoreCase) >= 0))
                        return true;
                }
            }

            return false;
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

            string currentVote = voteView.CurrentItem.ToString();

            var voters = MainViewModel.GetVoterListForVote(currentVote, CurrentVoteType);

            return voters?.Contains(voterName) ?? false;
        }

        /// <summary>
        /// Shorthand call to run both collection updates.
        /// </summary>
        private void UpdateCollections()
        {
            VoteView1.Refresh();
            VoteView2.Refresh();
            VoterView1.Refresh();
            VoterView2.Refresh();
            VoteView1.MoveCurrentToPosition(-1);
            VoteView2.MoveCurrentTo(lastSelected2 ?? "");
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

        /// <summary>
        /// Handle busywork for merging votes together and updating the VotesCollection.
        /// </summary>
        /// <param name="fromVote">The vote being merged.</param>
        /// <param name="toVote">The vote being merged into.</param>
        private void MergeVotes(string fromVote, string toVote)
        {
            try
            {
                if (MainViewModel.MergeVotes(fromVote, toVote, CurrentVoteType))
                {
                    OnPropertyChanged(nameof(HasUndoActions));
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Context Menu Utility
        /// <summary>
        /// Create the basic command menu items for the context menu.
        /// </summary>
        private void CreateContextMenuCommands()
        {
            MenuItem newTask = new MenuItem();
            newTask.Header = "New Task...";
            newTask.Click += newTask_Click;
            newTask.ToolTip = "Create a new task value.";

            MenuItem clearTask = new MenuItem();
            clearTask.Header = "Clear Task";
            clearTask.Click += modifyTask_Click;
            clearTask.ToolTip = "Clear the task from the currently selected vote.";

            ContextMenuCommands.Add(newTask);
            ContextMenuCommands.Add(clearTask);
        }

        /// <summary>
        /// Populate the ContextMenuTasks list from known tasks on window load.
        /// </summary>
        private void InitKnownTasks()
        {
            foreach (var task in MainViewModel.KnownTasks)
                ContextMenuTasks.Add(CreateContextMenuItem(task));
        }

        /// <summary>
        /// Function to create a MenuItem object for the context menu containing the provided header value.
        /// </summary>
        /// <param name="name">The name of the menu item.</param>
        /// <returns>Returns a MenuItem object with appropriate tooltip and click handler.</returns>
        private MenuItem CreateContextMenuItem(string name)
        {
            MenuItem mi = new MenuItem();
            mi.Header = name;
            mi.Click += modifyTask_Click;
            mi.ToolTip = $"Change the task for the selected item to '{mi.Header}'";
            mi.Tag = "NamedTask";

            return mi;
        }

        /// <summary>
        /// Recreate the context menu when new menu items are added.
        /// </summary>
        private void UpdateContextMenu()
        {
            var pMenu = (ContextMenu)this.Resources["TaskContextMenu"];
            if (pMenu != null)
            {
                pMenu.Items.Clear();

                foreach (var header in ContextMenuCommands)
                {
                    pMenu.Items.Add(header);
                }

                pMenu.Items.Add(new Separator());

                foreach (var task in ContextMenuTasks.OrderBy(m => m.Header))
                {
                    pMenu.Items.Add(task);
                }
            }
        }

        /// <summary>
        /// Given a new task name, create a new menu item and refresh the context menu.
        /// </summary>
        /// <param name="task">The name of a new task.</param>
        private void AddTaskToContextMenu(string task)
        {
            if (string.IsNullOrEmpty(task))
                return;

            if (ContextMenuTasks.Any(t => t.Header.ToString() == task))
                return;

            MainViewModel.UserDefinedTasks.Add(task);

            ContextMenuTasks.Add(CreateContextMenuItem(task));

            UpdateContextMenu();
        }
       

        #endregion

    }
}
