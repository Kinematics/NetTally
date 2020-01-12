using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTally.Comparers;
using NetTally.Forums;
using NetTally.Navigation;
using NetTally.Utility;
using NetTally.ViewModels;
using NetTally.Votes;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MergeVotesWindow.xaml
    /// </summary>
    public partial class ManageVotesWindow : Window, INotifyPropertyChanged, IActivable
    {
        #region Constructor and variables
        public ListCollectionView VoteView1 { get; } = new ListCollectionView(Array.Empty<string>());
        public ListCollectionView VoteView2 { get; } = new ListCollectionView(Array.Empty<string>());

        public ListCollectionView VoterView1 { get; } = new ListCollectionView(Array.Empty<string>());
        public ListCollectionView VoterView2 { get; } = new ListCollectionView(Array.Empty<string>());

        object lastSelected2 = null;
        int lastPosition1 = -1;
        int lastPosition2 = -1;

        readonly List<MenuItem> ContextMenuCommands = new List<MenuItem>();
        readonly List<MenuItem> ContextMenuTasks = new List<MenuItem>();

        readonly ViewModel mainViewModel;

        ListBox newTaskBox = null;

        string filter1String = "";
        string filter2String = "";

        private readonly ILogger<ManageVotesWindow> logger;
        private readonly IoCNavigationService navigationService;

        public Task ActivateAsync(object parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mainViewModel">The primary view model of the program.</param>
        public ManageVotesWindow(ViewModel mainViewModel, IoCNavigationService navigationService, ILogger<ManageVotesWindow> logger)
        {
            this.mainViewModel = mainViewModel;
            this.navigationService = navigationService;
            this.logger = logger;

            InitializeComponent();

            this.mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

            // Create filtered, sortable views into the collection for display in the window.
            VoteView1 = new ListCollectionView(this.mainViewModel.AllVotesCollection);
            VoteView2 = new ListCollectionView(this.mainViewModel.AllVotesCollection);

            PropertyGroupDescription groupDescription = new PropertyGroupDescription("Category");
            VoteView1.GroupDescriptions.Add(groupDescription);
            VoteView2.GroupDescriptions.Add(groupDescription);

            if (VoteView1.CanSort && VoteView2.CanSort)
            {
                IComparer voteCompare = new CustomVoteSort();
                VoteView1.CustomSort = voteCompare;
                VoteView2.CustomSort = voteCompare;
            }

            if (VoteView1.CanFilter && VoteView2.CanFilter)
            {
                VoteView1.Filter = (a) => FilterVotes(Filter1String, a as VoteLineBlock);
                VoteView2.Filter = (a) => FilterVotes(Filter2String, a as VoteLineBlock);
            }

            // Initialize starting selected positions
            VoteView1.MoveCurrentToPosition(-1);
            VoteView2.MoveCurrentToFirst();


            // Create filtered views for display in the window.
            VoterView1 = new ListCollectionView(this.mainViewModel.AllVotersCollection);
            VoterView2 = new ListCollectionView(this.mainViewModel.AllVotersCollection);

            VoterView1.CustomSort = Comparer.Default;
            VoterView2.CustomSort = Comparer.Default;

            VoterView1.Filter = (a) => FilterVoters(VoteView1, a as Origin);
            VoterView2.Filter = (a) => FilterVoters(VoteView2, a as Origin);

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

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closed" /> event.
        /// Removes event listeners on close, to prevent memory leaks.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;

            base.OnClosed(e);
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
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Filtering
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

                IsFilter1Empty = string.IsNullOrEmpty(filter1String);
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

                IsFilter2Empty = string.IsNullOrEmpty(filter2String);
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

        /// <summary>
        /// Filter to be used by the vote display to determine which votes should be
        /// shown in the list box.
        /// </summary>
        /// <param name="voteView">The view being filtered.</param>
        /// <param name="filterString">The filter string being used.</param>
        /// <param name="vote">The vote being checked by the filter delegate.</param>
        /// <returns>Returns true if the vote should be displayed, or false if it should be hidden.</returns>
        bool FilterVotes(string filterString, VoteLineBlock vote)
        {
            if (vote == null)
                return false;

            if (string.IsNullOrEmpty(filterString))
                return true;

            if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(vote.ToComparableString(), filterString, CompareOptions.IgnoreCase) >= 0)
                return true;

            var voters = mainViewModel.GetVoterListForVote(vote);

            return voters.Any(voter => CultureInfo.InvariantCulture.CompareInfo.IndexOf(voter.Author, filterString, CompareOptions.IgnoreCase) >= 0);
        }

        /// <summary>
        /// Filter to be used by a collection view to determine which voters should
        /// be displayed in the voter list box, for each vote that is selected.
        /// </summary>
        /// <param name="voteView">The view of the main vote box.</param>
        /// <param name="voter">The name of the voter being checked.</param>
        /// <returns>Returns true if that voter supports the currently selected
        /// vote in the vote view.</returns>
        private bool FilterVoters(ICollectionView voteView, Origin voter)
        {
            if (voter == null)
                return false;

            if (voteView.IsEmpty)
                return false;

            if (voteView.CurrentItem is VoteLineBlock currentVote)
            {
                var voters = mainViewModel.GetVoterListForVote(currentVote);
                return voters.Contains(voter);
            }

            return false;
        }

        #endregion

        #region Window events
        /// <summary>
        /// Update enabled state of merge button, and current list of voters, based on current vote selection
        /// for the list of votes to be merged from.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void votesFromListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VoterView1.Refresh();
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
        }

        /// <summary>
        /// Handler for the button to merge two vote items together.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void merge_Click(object sender, RoutedEventArgs e)
        {
            if (VoteView1.CurrentItem is VoteLineBlock fromVote && VoteView2.CurrentItem is VoteLineBlock toVote)
            {
                MergeVotes(fromVote, toVote);
            }
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

            List<Origin> fromVoters = votersFromListBox.Items.SourceCollection.OfType<Origin>().ToList();
            Origin joinVoter = VoterView2.CurrentItem as Origin;

            if (joinVoter == null)
                return;

            try
            {
                mainViewModel.JoinVoters(fromVoters, joinVoter);
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
                lastPosition1 = VoteView1.CurrentPosition;
                lastPosition2 = VoteView2.CurrentPosition;

                if (VoteView1.CurrentItem is VoteLineBlock currentVote)
                {
                    mainViewModel.DeleteVote(currentVote);
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
            UndoLastAction();
        }

        /// <summary>
        /// Handles the KeyDown event of the Window control.
        /// Ctrl-Z acts as a call to Undo.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                UndoLastAction();
                e.Handled = true;
            }
        }
        #endregion

        #region Binding Properties
        /// <summary>
        /// Binding for the Undo button on the window.
        /// </summary>
        public bool HasUndoActions => mainViewModel.HasUndoActions;
        #endregion

        #region Context Menu events
        private void TaskContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (!(sender is ContextMenu cm))
                return;

            if (!(cm.PlacementTarget is ListBox listBox))
                return;

            if (listBox.SelectedItem is VoteLineBlock selectedVote)
            {
                // Only enable the Parition Children context menu item if it's a valid action for the vote.
                bool enabled = HasChildLines(selectedVote);

                if (Resources["TaskContextMenu"] is ContextMenu pMenu)
                {
                    foreach (object item in pMenu.Items)
                    {
                        if (item is MenuItem mItem)
                        {
                            if (string.Equals(mItem.Header.ToString(), "Partition Children", StringComparison.Ordinal))
                            {
                                mItem.IsEnabled = enabled;
                            }
                        }
                    }
                }
            }
        }

        private bool HasChildLines(VoteLineBlock vote)
        {
            return (vote.Lines.Count > 1 && vote.Lines.Skip(1).All(v => v.Depth > 0));
        }

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

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    AcceptInput();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    CancelInput();
                    e.Handled = true;
                    break;
            }
        }

        private void modifyTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    if (cm.PlacementTarget is ListBox box)
                    {
                        if (box.SelectedItem is VoteLineBlock selectedVote)
                        {
                            string newTask = mi.Header.ToString();

                            if (!string.IsNullOrEmpty(newTask))
                            {
                                if (string.Equals(newTask, "Clear Task", StringComparison.Ordinal))
                                    mainViewModel.ReplaceTask(selectedVote, "");
                                else
                                    mainViewModel.ReplaceTask(selectedVote, newTask);
                            }
                        }
                    }
                }
            }
        }

        private async void reorderTasks_ClickAsync(object sender, RoutedEventArgs e)
        {
            await navigationService.ShowDialogAsync<ReorderTasksWindow>(this);

            mainViewModel.UpdateOutput();
        }

        private void partitionChildren_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    if (cm.PlacementTarget is ListBox box)
                    {
                        if (box.SelectedItem is VoteLineBlock selectedVote)
                        {
                            PartitionChildren(selectedVote);

                            mainViewModel.UpdateOutput();
                        }
                    }
                }
            }
        }
        #endregion

        #region Window Action Functions
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
            mainViewModel.AddUserDefinedTask(newTask);

            // Update the selected item of the list box
            if (newTaskBox?.SelectedItem is VoteLineBlock selectedVote)
            {
                mainViewModel.ReplaceTask(selectedVote, newTask);
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
            InputTextBox.Text = string.Empty;

            newTaskBox = null;
        }

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        private void UndoLastAction()
        {
            try
            {
                mainViewModel.UndoVoteModification();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            logger.LogTrace($"Received notification of property change from MainViewModel: {e.PropertyName}.");

            if (string.Equals(e.PropertyName, nameof(mainViewModel.AllVotesCollection), StringComparison.Ordinal))
            {
                UpdateVoteCollections();
            }
            else if (string.Equals(e.PropertyName, nameof(mainViewModel.AllVotersCollection), StringComparison.Ordinal))
            {
                UpdateVoterCollections();
            }
            else if (!string.IsNullOrEmpty(e.PropertyName))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }
        #endregion

        #region Utility functions
        /// <summary>
        /// Shorthand call to run both collection updates.
        /// </summary>
        private void UpdateVoteCollections()
        {
            VoteView1.Refresh();
            VoteView2.Refresh();

            if (lastPosition1 > VoteView1.Count)
                VoteView1.MoveCurrentToLast();
            else
                VoteView1.MoveCurrentToPosition(lastPosition1);

            if (lastPosition2 < 0)
                VoteView2.MoveCurrentTo(lastSelected2 ?? "");
            else if (lastPosition2 > VoteView2.Count)
                VoteView2.MoveCurrentToLast();
            else
                VoteView2.MoveCurrentToPosition(lastPosition2);

            // Retain the new position.
            lastPosition1 = VoteView1.CurrentPosition;
            lastPosition2 = VoteView2.CurrentPosition;
        }

        private void UpdateVoterCollections()
        {
            VoterView1.Refresh();
            VoterView2.Refresh();
        }

        /// <summary>
        /// Handle busywork for merging votes together and updating the VotesCollection.
        /// </summary>
        /// <param name="fromVote">The vote being merged.</param>
        /// <param name="toVote">The vote being merged into.</param>
        private void MergeVotes(VoteLineBlock fromVote, VoteLineBlock toVote)
        {
            try
            {
                lastPosition1 = VoteView1.CurrentPosition;
                lastPosition2 = -1;
                lastSelected2 = VoteView2.CurrentItem ?? lastSelected2;
                mainViewModel.MergeVotes(fromVote, toVote);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PartitionChildren(VoteLineBlock vote)
        {
            try
            {
                lastPosition1 = VoteView1.CurrentPosition;
                lastPosition2 = VoteView2.CurrentPosition;

                mainViewModel.PartitionChildren(vote);
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

            MenuItem reorderTasks = new MenuItem();
            reorderTasks.Header = "Re-Order Tasks";
            reorderTasks.Click += reorderTasks_ClickAsync;
            reorderTasks.ToolTip = "Modify the order in which the tasks appear in the output.";

            MenuItem partitionChildren = new MenuItem();
            partitionChildren.Header = "Partition Children";
            partitionChildren.Click += partitionChildren_Click;
            partitionChildren.ToolTip = "Split child vote lines into their own vote blocks.";

            ContextMenuCommands.Add(newTask);
            ContextMenuCommands.Add(clearTask);
            ContextMenuCommands.Add(reorderTasks);
            ContextMenuCommands.Add(partitionChildren);
        }

        /// <summary>
        /// Populate the ContextMenuTasks list from known tasks on window load.
        /// </summary>
        private void InitKnownTasks()
        {
            foreach (var task in mainViewModel.TaskList.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
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
        /// Also disables the Re-Order Tasks menu item if there are no known tasks.
        /// </summary>
        private void UpdateContextMenu()
        {
            var pMenu = (ContextMenu)this.Resources["TaskContextMenu"];
            if (pMenu != null)
            {
                pMenu.Items.Clear();

                foreach (var header in ContextMenuCommands)
                {
                    switch (header.Header.ToString())
                    {
                        case "Re-Order Tasks":
                            header.IsEnabled = mainViewModel.TaskList.Any();
                            break;
                        case "Partition Children":
                            pMenu.Items.Add(new Separator());
                            break;
                    }

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

            if (ContextMenuTasks.Any(t => string.Equals(t.Header.ToString(), task, StringComparison.Ordinal)))
                return;

            ContextMenuTasks.Add(CreateContextMenuItem(task));

            UpdateContextMenu();
        }


        #endregion

    }
}
