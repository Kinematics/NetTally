using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using NetTally.Types.Components;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Avalonia.Views
{
    public partial class ManageVotes : Window, INotifyPropertyChanged
    {
        #region Properties for binding

        #region Votes Properties

        /// <summary>
        /// Gets the list of <see cref="VoteLineBlock"/> that should be displayed in the from box.
        /// </summary>
        /// <remarks>
        /// This list is based on the internal <see cref="AllVotes"/> list, automatically sorted
        /// and filtered based on the <see cref="VoteFromFilter"/>.
        /// </remarks>
        public IEnumerable<VoteLineBlock> VotesFrom => this.AllVotes
            .OrderBy(vote => vote)
            .Where(vote => this.FilterVotes(this.VoteFromFilter, vote));

        /// <summary>
        /// Gets the list of <see cref="VoteLineBlock"/> that should be displayed in the to box.
        /// </summary>
        /// <remarks>
        /// This list is based on the internal <see cref="AllVotes"/> list, automatically sorted
        /// and filtered based on the <see cref="VoteToFilter"/>.
        /// </remarks>
        public IEnumerable<VoteLineBlock> VotesTo => this.AllVotes
            .OrderBy(vote => vote)
            .Where(vote => this.FilterVotes(this.VoteToFilter, vote));

        /// <summary>
        /// Gets all <see cref="VoteLineBlock"/>s present in the <see cref="ViewModel"/>,
        /// </summary>
        private IEnumerable<VoteLineBlock> AllVotes => this.MainViewModel.AllVotesCollection;

        /// <summary>
        /// Gets or Sets the currently selected From Vote.
        /// </summary>
        /// <remarks>
        /// Triggers <see cref="PropertyChanged"/> for <see cref="VotersFrom"/> because a change
        /// of this value indicates that that properties has changed.
        /// </remarks>
        public VoteLineBlock? SelectedFromVote
        {
            get => this.InternalSelectedFromVote;
            set
            {
                this.InternalSelectedFromVote = value;
                this.OnPropertyChanged(nameof(this.VotersFrom));
            }
        }
        private VoteLineBlock? InternalSelectedFromVote { get; set; }

        /// <summary>
        /// Gets or Sets the currently selected To Vote.
        /// </summary>
        /// <remarks>
        /// Triggers <see cref="PropertyChanged"/> for <see cref="VotersTo"/> because a change
        /// of this value indicates that that properties has changed.
        /// </remarks>
        public VoteLineBlock? SelectedToVote
        {
            get => this.InternalSelectedToVote;
            set
            {
                this.InternalSelectedToVote = value;
                this.OnPropertyChanged(nameof(this.VotersTo));
            }
        }
        private VoteLineBlock? InternalSelectedToVote { get; set; }

        #endregion

        #region Voters Properties
        public IEnumerable<Origin> VotersFrom =>
            this.GetVotersForVote(this.SelectedFromVote).OrderBy(voters => voters);
        public IEnumerable<Origin> VotersTo =>
            this.GetVotersForVote(this.SelectedToVote).OrderBy(voters => voters);

        #endregion

        #region Filter Properties

        /// <summary>
        /// Property for holding the string used to filter the 'from' votes.
        /// </summary>
        public string VoteFromFilter
        {
            get => this.InternalVoteFromFilter;
            set
            {
                InternalVoteFromFilter = value.RemoveUnsafeCharacters();
                OnPropertyChanged(nameof(this.VotesFrom));
            }
        }
        private string InternalVoteFromFilter { get; set; } = string.Empty;

        /// <summary>
        /// Property for holding the string used to filter the 'to' votes.
        /// </summary>
        public string VoteToFilter
        {
            get => this.InternalVoteToFilter;
            set
            {
                InternalVoteToFilter = value.RemoveUnsafeCharacters();
                OnPropertyChanged(nameof(this.VotesTo));
            }
        }
        private string InternalVoteToFilter { get; set; } = string.Empty;

        #endregion

        private ViewModels.ViewModel MainViewModel { get; }

        private ILogger<ManageVotes> Logger { get; set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mainViewModel">The primary view model of the program.</param>
        public ManageVotes(ViewModels.ViewModel mainViewModel, ILogger<ManageVotes> logger)
        {
            this.MainViewModel = mainViewModel;
            this.Logger = logger;

            AvaloniaXamlLoader.Load(this);

            // Populate the context menu with known tasks.
            CreateContextMenuCommands();
            InitKnownTasks();
            UpdateContextMenu();

            // Set the data context for binding.
            this.DataContext = this;

#if DEBUG
            this.AttachDevTools();
#endif

            this.MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
        }

        /// <summary>
        /// Raises the <see cref="Window" />.Closed event.
        /// Removes event listeners on close, to prevent memory leaks.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            MainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;

            base.OnClosed(e);
        }
        #endregion

        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region Filtering

        /// <summary>
        /// Filters votes and containing a given string.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="vote">The vote being checked.</param>
        /// <returns><see langword="true"/> if the vote contains the string, <see langword="false"/> otherwise.</returns>
        private bool FilterVotes(string filter, VoteLineBlock vote) =>
            (CultureInfo.InvariantCulture.CompareInfo.IndexOf(vote.ToComparableString(), filter, CompareOptions.IgnoreCase) >= 0)
            || string.IsNullOrEmpty(filter);

        /// <summary>
        /// Gets a list of voters that support a given vote.
        /// </summary>
        /// <remarks>
        /// <paramref name="vote"/> is nullable here, because the view might want to query when it has no votes.
        /// </remarks>
        /// <param name="vote">The vote to lookup.</param>
        /// <returns>All voters that support the given vote.</returns>
        public IEnumerable<Origin> GetVotersForVote(VoteLineBlock? vote) =>
            (vote != null) ? this.MainViewModel.GetVoterListForVote(vote) : Enumerable.Empty<Origin>();

        #endregion

        #region Window events
        /// <summary>
        /// Handler for the button to merge two vote items together.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Merge_Click(object sender, RoutedEventArgs e)
        {
            if ((this.SelectedFromVote != null) && (this.SelectedToVote != null))
            {
                MergeVotes(this.SelectedFromVote, this.SelectedToVote);
            }
        }

        /// <summary>
        /// Handler for the button to join voters.
        /// All voters from the from list are adjusted to support all votes supported by the
        /// voter selected in the to list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Join_Click(object sender, RoutedEventArgs e)
        {
            if ((this.VotesFrom.Count() != 0) && (this.FindControl<ListBox>("VotersTo").SelectedItem is Origin joinVoter))
            {
                try
                {
                    this.MainViewModel.JoinVoters(this.VotersFrom.ToList(), joinVoter);
                }
                catch (Exception ex)
                {
                    WarningDialog.Show(ex.Message, "Error", false);
                }
            }
        }

        /// <summary>
        /// Delete the vote that has been selected in both list boxes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.SelectedFromVote != null)
                {
                    MainViewModel.DeleteVote(this.SelectedFromVote);
                }
            }
            catch (Exception ex)
            {
                WarningDialog.Show(ex.Message, "Error", false);
            }
        }

        /// <summary>
        /// Calls Undo on the vote counter to undo the most recent vote modification action.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void Undo_Click(object sender, RoutedEventArgs e) => this.UndoLastAction();

        /// <summary>
        /// Closes the window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        #endregion

        #region Binding Properties
        /// <summary>
        /// Binding for the Undo button on the window.
        /// </summary>
        public bool HasUndoActions => this.MainViewModel.HasUndoActions;
        #endregion

        #region Window Action Functions

        /// <summary>
        /// Undoes the last action.
        /// </summary>
        private void UndoLastAction()
        {
            try
            {
                this.MainViewModel.UndoVoteModification();
            }
            catch (Exception ex)
            {
                WarningDialog.Show(ex.Message, "Error", false);
            }
        }
        #endregion

        #region Watched Events        
        /// <summary>
        /// Watch for notifications from the main view model about changes in the vote backend.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Logger.LogTrace("Received notification of property change from MainViewModel: {PropertyName}.", e.PropertyName);

            if (string.Equals(e.PropertyName, nameof(this.MainViewModel.AllVotesCollection), StringComparison.Ordinal))
            {
                this.UpdateVoteCollections();
            }
            else if (string.Equals(e.PropertyName, nameof(this.MainViewModel.AllVotersCollection), StringComparison.Ordinal))
            {
                this.UpdateVoterCollections();
            }
            else if (!string.IsNullOrEmpty(e.PropertyName))
            {
                this.OnPropertyChanged(e.PropertyName);
            }
        }
        #endregion

        #region Context Menu Events

        private void TaskContextMenu_Opened(object? sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu cm)
                return;

            // The context menu parent should be a Popup. The parent of that
            // should be the placement target listbox. That's what we need
            // to examine.
            if (cm.Parent?.Parent is ListBox listBox)
            {
                if (listBox.SelectedItem is VoteLineBlock selectedVote)
                {
                    // Parition Children context menu item if it's a valid action for the vote.
                    // Only relevant when we add in that action option.
                    //if (HasChildLines(selectedVote))
                    //{ }
                }
            }
        }

        private void newTask_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    newTaskBox = cm.PlacementTarget as ListBox;
                }
            }

            if (this.FindControl<Grid>("InputBox") is Grid InputBox)
            {
                // Show the custom input box, and put focus on the text box.
                InputBox.IsVisible = true;
                this.FindControl<TextBox>("InputTextBox")?.Focus();
            }
        }

        private void YesButton_Click(object? sender, RoutedEventArgs e)
        {
            AcceptInput();
        }

        private void NoButton_Click(object? sender, RoutedEventArgs e)
        {
            CancelInput();
        }

        private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
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

        private void modifyTask_Click(object? sender, RoutedEventArgs e)
        {
            // Get the context menu for the menu item.
            if (sender is MenuItem mi && mi.Parent is ContextMenu cm)
            {
                // The context menu parent should be a Popup. The parent of that
                // should be the placement target listbox. That's what we need
                // to examine.
                if (cm.Parent?.Parent is ListBox listBox)
                {
                    if (listBox.SelectedItem is VoteLineBlock selectedVote)
                    {
                        string newTask = mi.Header.ToString() ?? "";

                        if (!string.IsNullOrEmpty(newTask))
                        {
                            if (string.Equals(newTask, "Clear Task", StringComparison.Ordinal))
                                MainViewModel.ReplaceTask(selectedVote, "");
                            else
                                MainViewModel.ReplaceTask(selectedVote, newTask);
                        }
                    }
                }
            }
        }

        private void partitionChildren_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    if (cm.PlacementTarget is ListBox box)
                    {
                        if (box.SelectedItem is VoteLineBlock selectedVote)
                        {
                            MainViewModel.PartitionChildren(selectedVote);
                            MainViewModel.UpdateOutput();
                        }
                    }
                }
            }
        }

        #endregion Context Menu Events

        #region Context Menu Utility
        ListBox? newTaskBox = null;
        readonly List<MenuItem> ContextMenuCommands = new();
        readonly List<MenuItem> ContextMenuTasks = new();
        List<MenuItem> ContextMenuItems { get; } = new();
        readonly MenuItem separator = new() { Header = "-" };


        /// <summary>
        /// Create the basic command menu items for the context menu.
        /// </summary>
        private void CreateContextMenuCommands()
        {
            MenuItem newTask = new MenuItem();
            newTask.Header = "New Task...";
            newTask.Click += newTask_Click;
            //newTask.ToolTip = "Create a new task value.";

            MenuItem clearTask = new MenuItem();
            clearTask.Header = "Clear Task";
            clearTask.Click += modifyTask_Click;
            //clearTask.ToolTip = "Clear the task from the currently selected vote.";

            //MenuItem reorderTasks = new MenuItem();
            //reorderTasks.Header = "Re-Order Tasks";
            //reorderTasks.Click += reorderTasks_ClickAsync;
            //reorderTasks.ToolTip = "Modify the order in which the tasks appear in the output.";

            //MenuItem partitionChildren = new MenuItem();
            //partitionChildren.Header = "Partition Children";
            //partitionChildren.Click += partitionChildren_Click;
            //partitionChildren.ToolTip = "Split child vote lines into their own vote blocks.";

            ContextMenuCommands.Add(newTask);
            ContextMenuCommands.Add(clearTask);
            //ContextMenuCommands.Add(reorderTasks);
            //ContextMenuCommands.Add(partitionChildren);
        }

        /// <summary>
        /// Populate the ContextMenuTasks list from known tasks on window load.
        /// </summary>
        private void InitKnownTasks()
        {
            foreach (var task in MainViewModel.TaskList.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
                ContextMenuTasks.Add(CreateContextMenuTaskItem(task));
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

            ContextMenuTasks.Add(CreateContextMenuTaskItem(task));

            UpdateContextMenu();
        }

        /// <summary>
        /// Function to create a MenuItem object for the context menu containing the provided header value.
        /// </summary>
        /// <param name="name">The name of the menu item.</param>
        /// <returns>Returns a MenuItem object with appropriate tooltip and click handler.</returns>
        private MenuItem CreateContextMenuTaskItem(string name)
        {
            MenuItem mi = new MenuItem();
            mi.Header = name;
            mi.Click += modifyTask_Click;
            //mi.ToolTip = $"Change the task for the selected item to '{mi.Header}'";
            mi.Tag = "NamedTask";

            return mi;
        }

        /// <summary>
        /// Recreate the context menu when new menu items are added.
        /// Also disables the Re-Order Tasks menu item if there are no known tasks.
        /// </summary>
        private void UpdateContextMenu()
        {
            ContextMenuItems.Clear();

            foreach (MenuItem header in ContextMenuCommands)
            {
                //switch (header.Header.ToString())
                //{
                //    case "Re-Order Tasks":
                //        header.IsEnabled = MainViewModel.TaskList.Any();
                //        break;
                //    case "Partition Children":
                //        pMenuItems.Add(new Separator());
                //        break;
                //}

                ContextMenuItems.Add(header);
            }

            ContextMenuItems.Add(separator);

            foreach (MenuItem task in ContextMenuTasks.OrderBy(m => m.Header))
            {
                ContextMenuItems.Add(task);
            }

            OnPropertyChanged(nameof(ContextMenuItems));
        }

        /// <summary>
        /// Process acceptance of the new task text.
        /// </summary>
        private void AcceptInput()
        {
            Grid? InputBox = this.FindControl<Grid>("InputBox");
            if (InputBox is null)
                return;

            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.IsVisible = false;

            TextBox? InputTextBox = this.FindControl<TextBox>("InputTextBox");
            if (InputTextBox is null)
                return;

            string newTask = InputTextBox.Text.RemoveUnsafeCharacters().Trim();

            // Clear InputBox.
            InputTextBox.Text = string.Empty;

            // Do something with the Input
            AddTaskToContextMenu(newTask);
            MainViewModel.AddUserDefinedTask(newTask);

            // Update the selected item of the list box
            if (newTaskBox?.SelectedItem is VoteLineBlock selectedVote)
            {
                MainViewModel.ReplaceTask(selectedVote, newTask);
            }

            newTaskBox = null;
        }

        /// <summary>
        /// Process rejecting the new task text.
        /// </summary>
        private void CancelInput()
        {
            Grid? InputBox = this.FindControl<Grid>("InputBox");
            if (InputBox is null)
                return;

            // NoButton Clicked! Let's hide our InputBox.
            InputBox.IsVisible = false;


            TextBox? InputTextBox = this.FindControl<TextBox>("InputTextBox");
            if (InputTextBox is null)
                return;

            // Clear InputBox.
            InputTextBox.Text = string.Empty;

            newTaskBox = null;
        }

        private bool HasChildLines(VoteLineBlock vote)
        {
            return (vote.Lines.Count > 1 && vote.Lines.Skip(1).All(v => v.Depth > 0));
        }
        #endregion


        #region Utility functions
        /// <summary>
        /// Shorthand call to run both collection updates.
        /// </summary>
        private void UpdateVoteCollections()
        {
            this.OnPropertyChanged(nameof(this.VotesFrom));
            this.OnPropertyChanged(nameof(this.VotesTo));
        }

        private void UpdateVoterCollections()
        {
            this.OnPropertyChanged(nameof(this.VotersFrom));
            this.OnPropertyChanged(nameof(this.VotersTo));
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
                MainViewModel.MergeVotes(fromVote, toVote);
            }
            catch (ArgumentException ex)
            {
                WarningDialog.Show(ex.Message, "Error", false);
            }
        }

        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public ManageVotes() { throw new InvalidOperationException("The default constructor should not be called"); }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
