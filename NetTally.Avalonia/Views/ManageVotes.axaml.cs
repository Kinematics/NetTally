using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using NetTally.Avalonia.Navigation;
using NetTally.Collections;
using NetTally.Tally.ComponentsF.Votes;
using NetTally.Utility;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views
{
    public partial class ManageVotes : Window, INotifyPropertyChanged
    {
        private readonly ManageVotesViewModelF manageVotesViewModel;
        private readonly AvaloniaNavigationService navigationService;
        private readonly ILogger<ManageVotes> logger;

        public ManageVotes(
            ManageVotesViewModelF manageVotesViewModel,
            AvaloniaNavigationService navigationService,
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

#if DEBUG
            this.AttachDevTools();
#endif
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void Close_Click(object sender, RoutedEventArgs e) => this.Close();

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region Context Menu Events
        private void ContextMenu_Opened(object? sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu cm)
                return;

            // The context menu parent should be a Popup. The parent of that
            // should be the placement target listbox. That's what we need
            // to examine.
            if (cm.Parent?.Parent is ListBox listBox)
            {
                if (listBox.SelectedItem is VoteBlockType selectedVote)
                {
                    var selectedVoteTask = selectedVote.Task;

                    // Enable/Disable commands based on whether it's valid for the selected vote.

                    foreach (var cmd in ContextMenuCommands)
                    {
                        string? cmdHeader = cmd.Header as string;

                        switch (cmdHeader)
                        {
                            case partitionChildrenString:
                                cmd.IsEnabled = HasChildLines(selectedVote);
                                break;
                            case clearTaskString:
                                cmd.IsEnabled = (selectedVoteTask != VoteTask.Empty);
                                break;
                            case reorderTasksString:
                                cmd.IsEnabled = ContextMenuTasks.Count > 1;
                                break;
                        }
                    }

                    foreach (var task in ContextMenuTasks)
                    {
                        string? menuTask = task.Header as string;
                        task.IsEnabled = menuTask != selectedVoteTask.Name;
                    }
                }
            }
        }

        private void NewTask_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    newTaskBox = cm.PlacementTarget as ListBox;
                }
            }

            // Show the custom input box, and put focus on the text box.
            InputBox.IsVisible = true;
            InputTextBox.Focus();
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

        private void ModifyTask_Click(object? sender, RoutedEventArgs e)
        {
            // Get the context menu for the menu item.
            if (sender is MenuItem mi && mi.Parent is ContextMenu cm)
            {
                // The context menu parent should be a Popup. The parent of that
                // should be the placement target listbox. That's what we need
                // to examine.
                if (cm.Parent?.Parent is ListBox listBox)
                {
                    if (listBox.SelectedItem is VoteBlockType selectedVote)
                    {
                        string newTask = mi.Header?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(newTask))
                        {
                            if (string.Equals(newTask, "Clear Task", StringComparison.Ordinal))
                                manageVotesViewModel.ReplaceTask(selectedVote, "");
                            else
                                manageVotesViewModel.ReplaceTask(selectedVote, newTask);
                        }
                    }
                }
            }
        }

        private async void ReorderTasks_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                await navigationService.ShowDialogAsync<ReorderTasks>(this);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reordering tasks.");
            }
        }

        private void PartitionChildren_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Parent is ContextMenu cm)
                {
                    if (cm.PlacementTarget is ListBox box)
                    {
                        if (box.SelectedItem is VoteBlockType selectedVote)
                        {
                            manageVotesViewModel.PartitionChildren(selectedVote);
                        }
                    }
                }
            }
        }

        #endregion Context Menu Events

        #region Context Menu Utility
        ObservableCollectionExt<MenuItem> ContextMenuItems { get; } = [];
        readonly List<MenuItem> ContextMenuCommands = [];
        readonly List<MenuItem> ContextMenuTasks = [];
        readonly MenuItem separator = new() { Header = "-" };
        ListBox? newTaskBox = null;
        const string partitionChildrenString = "Partition Children";
        const string clearTaskString = "Clear Task";
        const string reorderTasksString = "Re-Order Tasks";


        /// <summary>
        /// Create the basic command menu items for the context menu.
        /// </summary>
        private void CreateContextMenuCommands()
        {
            MenuItem newTask = new()
            {
                Header = "New Task..."
            };
            newTask.Click += NewTask_Click;
            ToolTip.SetTip(newTask, "Create a new task value.");

            MenuItem clearTask = new()
            {
                Header = clearTaskString
            };
            clearTask.Click += ModifyTask_Click;
            ToolTip.SetTip(clearTask, "Clear the task from the currently selected vote.");

            MenuItem reorderTasks = new()
            {
                Header = reorderTasksString
            };
            reorderTasks.Click += ReorderTasks_Click;
            ToolTip.SetTip(reorderTasks, "Modify the order in which the tasks appear in the output.");

            MenuItem partitionChildren = new()
            {
                Header = partitionChildrenString
            };
            partitionChildren.Click += PartitionChildren_Click;
            ToolTip.SetTip(partitionChildren, "Split child vote lines into their own vote blocks.");

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
            var orderedTasks = manageVotesViewModel.TaskList
                .OrderBy(t => t, VoteTaskComparer.Instance);

            foreach (var task in orderedTasks)
                ContextMenuTasks.Add(CreateContextMenuTaskItem(task.Name));
        }

        /// <summary>
        /// Given a new task name, create a new menu item and refresh the context menu.
        /// </summary>
        /// <param name="task">The name of a new task.</param>
        private void AddTaskToContextMenu(string task)
        {
            if (string.IsNullOrEmpty(task))
                return;

            if (ContextMenuTasks.Any(t => string.Equals(t.Header?.ToString(), task, StringComparison.Ordinal)))
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
            MenuItem mi = new()
            {
                Header = name
            };
            mi.Click += ModifyTask_Click;
            ToolTip.SetTip(mi, $"Change the task for the selected vote item to '{mi.Header}'");
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

            ContextMenuItems.AddRange(ContextMenuCommands);

            ContextMenuItems.Add(separator);

            ContextMenuItems.AddRange(ContextMenuTasks.OrderBy(m => m.Header));

            OnPropertyChanged(nameof(ContextMenuItems));
        }

        /// <summary>
        /// Process acceptance of the new task text.
        /// </summary>
        private void AcceptInput()
        {
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.IsVisible = false;

            string newTask = InputTextBox.Text?.RemoveUnsafeCharacters().Trim() ?? "";

            // Clear InputBox.
            InputTextBox.Text = string.Empty;

            // Do something with the Input
            AddTaskToContextMenu(newTask);
            manageVotesViewModel.AddUserDefinedTask(newTask);

            // Update the selected item of the list box
            if (newTaskBox?.SelectedItem is VoteBlockType selectedVote)
            {
                manageVotesViewModel.ReplaceTask(selectedVote, newTask);
            }

            newTaskBox = null;
        }

        /// <summary>
        /// Process rejecting the new task text.
        /// </summary>
        private void CancelInput()
        {
            // NoButton Clicked! Let's hide our InputBox.
            InputBox.IsVisible = false;

            // Clear InputBox.
            InputTextBox.Text = string.Empty;

            newTaskBox = null;
        }

        private static bool HasChildLines(VoteBlockType vote)
        {
            return (vote.Lines.Count > 1 && vote.Lines.Skip(1).All(v => v.Depth > 0));
        }
        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#if DEBUG
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public ManageVotes()
        {
            InitializeComponent();
        }
#endif
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
