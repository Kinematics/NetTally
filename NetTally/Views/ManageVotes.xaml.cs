using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using NetTally.Comparers;
using NetTally.Navigation;
using NetTally.Types.Components;
using NetTally.Utility;
using NetTally.ViewModels;
using NetTally.Votes;

namespace NetTally.Views
{
    /// <summary>
    /// Interaction logic for ManageVotes2.xaml
    /// </summary>
    [ObservableObject]
    public partial class ManageVotes : Window, IActivable
    {
        private readonly ManageVotesViewModel manageVotesViewModel;
        private readonly IoCNavigationService navigationService;
        private readonly ILogger<ManageVotes> logger;

        public ListCollectionView VoteView1 { get; }
        public ListCollectionView VoteView2 { get; }
        public ListCollectionView VoterView1 { get; }
        public ListCollectionView VoterView2 { get; }


        public ManageVotes(
            ManageVotesViewModel manageVotesViewModel,
            IoCNavigationService navigationService,
            ILogger<ManageVotes> logger)
        {
            this.manageVotesViewModel = manageVotesViewModel;
            this.navigationService = navigationService;
            this.logger = logger;

            VoteView1 = new ListCollectionView(manageVotesViewModel.AllVotesCollection);
            VoteView2 = new ListCollectionView(manageVotesViewModel.AllVotesCollection);

            VoterView1 = new ListCollectionView(manageVotesViewModel.AllVotersCollection);
            VoterView2 = new ListCollectionView(manageVotesViewModel.AllVotersCollection);

            // Setup sorting/filtering/etc for views
            SetupViews();

            // Populate the context menu with known tasks.
            CreateContextMenuCommands();

            // Initialize the window and set up the data context binding.
            InitializeComponent();
            DataContext = manageVotesViewModel;
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }

        private void SetupViews()
        {
            // ** Votes **

            PropertyGroupDescription groupDescription = new("Category");
            VoteView1.GroupDescriptions.Add(groupDescription);
            VoteView2.GroupDescriptions.Add(groupDescription);

            if (VoteView1.CanSort)
            {
                IComparer voteCompare = new CustomVoteSort();
                VoteView1.CustomSort = voteCompare;
            }

            if (VoteView2.CanSort)
            {
                IComparer voteCompare = new CustomVoteSort();
                VoteView2.CustomSort = voteCompare;
            }

            if (VoteView1.CanFilter)
            {
                VoteView1.Filter = (a) => FilterVotes(Filter1String, a as VoteLineBlock);
            }

            if (VoteView2.CanFilter)
            {
                VoteView2.Filter = (a) => FilterVotes(Filter2String, a as VoteLineBlock);
            }

            // Initialize starting selected positions
            VoteView1.MoveCurrentToPosition(-1);
            VoteView2.MoveCurrentToFirst();

            VoteView1.CurrentChanged += (sender, e) =>
            {
                manageVotesViewModel.FromVote = VoteView1.CurrentItem as VoteLineBlock;
            };
            VoteView2.CurrentChanged += (sender, e) =>
            {
                manageVotesViewModel.ToVote = VoteView2.CurrentItem as VoteLineBlock;
            };

            // ** Voters **

            VoterView1.CustomSort = Comparer.Default;
            VoterView2.CustomSort = Comparer.Default;

            VoterView1.Filter = (a) => FilterVoters(VoteView1, a as Origin);
            VoterView2.Filter = (a) => FilterVoters(VoteView2, a as Origin);

            VoterView1.CurrentChanged += (sender, e) =>
            {
                manageVotesViewModel.FromVoters = VoterView1.SourceCollection.OfType<Origin>().ToList();
            };
            VoterView2.CurrentChanged += (sender, e) =>
            {
                manageVotesViewModel.ToVoter = VoterView2.CurrentItem as Origin;
            };

            // Update the voters to match the votes.
            VoterView1.Refresh();
            VoterView2.Refresh();
        }


        #region Filtering
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFilter1Empty))]
        string filter1String = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsFilter2Empty))]
        string filter2String = "";

        partial void OnFilter1StringChanged(string value)
        {
            VoteView1.Refresh();
        }

        partial void OnFilter2StringChanged(string value)
        {
            VoteView2.Refresh();
        }

        public bool IsFilter1Empty => string.IsNullOrEmpty(Filter1String);
        public bool IsFilter2Empty => string.IsNullOrEmpty(Filter2String);


        /// <summary>
        /// Filter to be used by the vote display to determine which votes should be
        /// shown in the list box.
        /// </summary>
        /// <param name="voteView">The view being filtered.</param>
        /// <param name="filterString">The filter string being used.</param>
        /// <param name="vote">The vote being checked by the filter delegate.</param>
        /// <returns>Returns true if the vote should be displayed, or false if it should be hidden.</returns>
        bool FilterVotes(string filterString, VoteLineBlock? vote)
        {
            if (vote == null)
                return false;

            if (string.IsNullOrEmpty(filterString))
                return true;

            if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(vote.ToComparableString(), filterString, CompareOptions.IgnoreCase) >= 0)
                return true;

            var voters = manageVotesViewModel.GetVoterListForVote(vote).ToList();

            if (voters.Count == 0)
                return false;

            return voters.Any(voter => CultureInfo.InvariantCulture.CompareInfo.IndexOf(voter.Author.Name, filterString, CompareOptions.IgnoreCase) >= 0);
        }

        /// <summary>
        /// Filter to be used by a collection view to determine which voters should
        /// be displayed in the voter list box, for each vote that is selected.
        /// </summary>
        /// <param name="voteView">The view of the main vote box.</param>
        /// <param name="voter">The name of the voter being checked.</param>
        /// <returns>Returns true if that voter supports the currently selected
        /// vote in the vote view.</returns>
        private bool FilterVoters(ICollectionView voteView, Origin? voter)
        {
            if (voter == null)
                return false;

            if (voteView.IsEmpty)
                return false;

            if (voteView.CurrentItem is VoteLineBlock currentVote)
            {
                var voters = manageVotesViewModel.GetVoterListForVote(currentVote);
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
        private void VotesFromListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VoterView1.Refresh();
        }

        /// <summary>
        /// Update enabled state of merge button, and current list of voters, based on current vote selection
        /// for the list of votes to be merged to.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VotesToListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VoterView2.Refresh();
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
                if (manageVotesViewModel.UndoCommand.CanExecute(this))
                    manageVotesViewModel.UndoCommand.Execute(this);

                e.Handled = true;
            }
        }
        #endregion

        #region Context Menu Setup
        MenuItem newTask = default!;
        MenuItem clearTask = default!;
        MenuItem reorderTasks = default!;
        MenuItem partitionChildren = default!;
        private readonly Separator separator = new();
        private readonly List<MenuItem> ContextMenuTasks = new();
        VoteLineBlock? selectedVoteForNewTask;

        /// <summary>
        /// Create the command menu items for the context menu.
        /// </summary>
        private void CreateContextMenuCommands()
        {
            newTask = new()
            {
                Header = "New Task...",
                ToolTip = "Create a new task value.",
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            newTask.Click += NewTask_Click;

            clearTask = new()
            {
                Header = "Clear Task",
                ToolTip = "Clear the task from the currently selected vote.",
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            clearTask.Click += ClearTask_Click;

            reorderTasks = new()
            {
                Header = "Re-Order Tasks",
                ToolTip = "Modify the order in which the tasks appear in the output.",
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            reorderTasks.Click += ReorderTasksAsync_Click;

            partitionChildren = new()
            {
                Header = "Partition Children",
                ToolTip = "Split child vote lines into their own vote blocks.",
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Top,
            };
            partitionChildren.Click += PartitionChildren_Click;

            InitKnownTasks();
            UpdateContextMenu();
        }

        /// <summary>
        /// Populate the ContextMenuTasks list from known tasks on window load.
        /// </summary>
        private void InitKnownTasks()
        {
            var sortedTasks = manageVotesViewModel.TaskList.OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

            foreach (var task in sortedTasks)
                ContextMenuTasks.Add(CreateContextMenuItem(task));
        }

        /// <summary>
        /// Function to create a MenuItem object for the context menu containing the provided header value.
        /// </summary>
        /// <param name="name">The name of the menu item.</param>
        /// <returns>Returns a MenuItem object with appropriate tooltip and click handler.</returns>
        private MenuItem CreateContextMenuItem(string name)
        {
            MenuItem mi = new()
            {
                Header = name,
                ToolTip = $"Change the task for the selected item to '{name}'",
                Tag = "NamedTask"
            };
            mi.Click += ModifyTask_Click;

            return mi;
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

        /// <summary>
        /// Recreate the context menu when new menu items are added.
        /// Also disables the Re-Order Tasks menu item if there are no known tasks.
        /// </summary>
        private void UpdateContextMenu()
        {
            var pMenu = (ContextMenu)Resources["TaskContextMenu"];

            if (pMenu is null)
                return;

            pMenu.Items.Clear();

            pMenu.Items.Add(newTask);
            pMenu.Items.Add(clearTask);
            pMenu.Items.Add(reorderTasks);
            pMenu.Items.Add(partitionChildren);
            pMenu.Items.Add(separator);

            foreach (var task in ContextMenuTasks.OrderBy(m => m.Header))
            {
                pMenu.Items.Add(task);
            }
        }
        #endregion

        #region Context Menu events
        private void TaskContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu cm)
                return;

            if (cm.PlacementTarget is not ListBox listBox)
            {
                e.Handled = true;
                return;
            }

            if (listBox.SelectedItem is not VoteLineBlock selectedVote)
            {
                e.Handled = true;
                return;
            }

            // Only enable the Parition Children context menu item if it's a valid action for the vote.
            partitionChildren.IsEnabled = HasChildLines(selectedVote);

            // Only enable Reorder Tasks if we have tasks to reorder
            reorderTasks.IsEnabled = manageVotesViewModel.HasTasks;
        }

        private void NewTask_Click(object sender, RoutedEventArgs e)
        {
            selectedVoteForNewTask = GetSelectedVoteInContext(sender);

            // Show the custom input box, and put focus on the text box.
            InputBox.Visibility = Visibility.Visible;
            InputTextBox.Focus();
        }

        private void ClearTask_Click(object sender, RoutedEventArgs e)
        {
            var selectedVote = GetSelectedVoteInContext(sender);

            if (selectedVote is not null)
                ModifyTask(selectedVote, string.Empty);
        }

        private void ModifyTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                string? newTask = mi.Header.ToString();

                if (newTask is null)
                    return;
               
                var selectedVote = GetSelectedVoteInContext(sender);

                if (selectedVote is not null)
                    ModifyTask(selectedVote, newTask);
            }
        }

        private async void ReorderTasksAsync_Click(object sender, RoutedEventArgs e)
        {
            await navigationService.ShowDialogAsync<ReorderTasks>(this);
        }

        private void PartitionChildren_Click(object sender, RoutedEventArgs e)
        {
            var selectedVote = GetSelectedVoteInContext(sender);

            if (selectedVote is not null)
                manageVotesViewModel.PartitionChildren(selectedVote);
        }

        private void ModifyTask(VoteLineBlock selectedVote, string newTask)
        {
            manageVotesViewModel.ReplaceTask(selectedVote, newTask);
        }

        private static bool HasChildLines(VoteLineBlock vote)
        {
            return (vote.Lines.Count > 1 && vote.Lines.Skip(1).All(v => v.Depth > 0));
        }

        private static VoteLineBlock? GetSelectedVoteInContext(object? sender)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    if (cm.PlacementTarget is ListBox listBox)
                    {
                        if (listBox.SelectedItem is VoteLineBlock selectedVote)
                        {
                            return selectedVote;
                        }
                    }
                }
            }

            return null;
        }
        #endregion

        #region New Task Overlay
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

        /// <summary>
        /// Process acceptance of the new task text.
        /// </summary>
        private void AcceptInput()
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.Visibility = Visibility.Collapsed;

            string newTask = InputTextBox.Text.RemoveUnsafeCharacters().Trim();

            // Clear InputBox.
            InputTextBox.Text = string.Empty;

            // Do something with the Input
            AddTaskToContextMenu(newTask);
            manageVotesViewModel.AddUserDefinedTask(newTask);

            if (selectedVoteForNewTask is not null)
                manageVotesViewModel.ReplaceTask(selectedVoteForNewTask, newTask);
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
        }
        #endregion

    }
}
