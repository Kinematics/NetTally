using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Utility;
using NetTally.ViewModels;

namespace NetTally.Views
{
    /// <summary>
    /// Interaction logic for ManageVotes2.xaml
    /// </summary>
    [ObservableObject]
    public partial class ManageVotes : Window
    {
        private readonly ManageVotesViewModelF manageVotesViewModel;
        private readonly WPFNavigationService navigationService;
        private readonly ILogger<ManageVotes> logger;

        public ManageVotes(
            ManageVotesViewModelF manageVotesViewModel,
            WPFNavigationService navigationService,
            ILogger<ManageVotes> logger)
        {
            this.manageVotesViewModel = manageVotesViewModel;
            this.navigationService = navigationService;
            this.logger = logger;

            InitializeComponent();

            // Populate the context menu with known tasks.
            CreateContextMenuCommands();
            InitKnownTasks();
            UpdateContextMenu();

            DataContext = manageVotesViewModel;
        }

        #region Window events
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
        private readonly List<MenuItem> ContextMenuTasks = [];
        VoteBlockType? selectedVoteForNewTask;

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
            var sortedTasks = manageVotesViewModel.TaskList.OrderBy(t => t, VoteTaskComparer.Instance);

            foreach (var task in sortedTasks)
                ContextMenuTasks.Add(CreateContextMenuItem(task.Name));
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

            if (listBox.SelectedItem is not VoteBlockType selectedVote)
            {
                e.Handled = true;
                return;
            }

            // Only enable the Parition Children context menu item if it's a valid action for the vote.
            partitionChildren.IsEnabled = HasChildLines(selectedVote);

            // Only clear a task if the vote has one.
            clearTask.IsEnabled = (selectedVote.Task != VoteTask.Empty);

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

        private void ModifyTask(VoteBlockType selectedVote, string newTask)
        {
            manageVotesViewModel.ReplaceTask(selectedVote, newTask);
        }

        private static bool HasChildLines(VoteBlockType vote)
        {
            return (vote.Lines.Count > 1 && vote.Lines.Skip(1).All(v => v.Depth > 0));
        }

        private static VoteBlockType? GetSelectedVoteInContext(object? sender)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    if (cm.PlacementTarget is ListBox listBox)
                    {
                        if (listBox.SelectedItem is VoteBlockType selectedVote)
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
