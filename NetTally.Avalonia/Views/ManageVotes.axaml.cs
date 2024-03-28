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
using NetTally.Utility;
using NetTally.ViewModels;
using NetTally.Votes;

namespace NetTally.Avalonia.Views
{
    public partial class ManageVotes : Window, INotifyPropertyChanged
    {
        private readonly ManageVotesViewModel manageVotesViewModel;
        private readonly ILogger<ManageVotes> logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mainViewModel">The primary view model of the program.</param>
        public ManageVotes(
            ManageVotesViewModel manageVotesViewModel,
            ILogger<ManageVotes> logger)
        {
            this.manageVotesViewModel = manageVotesViewModel;
            this.logger = logger;

            // Populate the context menu with known tasks.
            CreateContextMenuCommands();
            InitKnownTasks();
            UpdateContextMenu();

            InitializeComponent();
            DataContext = manageVotesViewModel;

            manageVotesViewModel.PropertyChanged += ManageVotesViewModel_PropertyChanged;

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void ManageVotesViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            logger.LogTrace("Received notification of property change from manageVotesViewModel: {PropertyName}.", e.PropertyName);

            if (!string.IsNullOrEmpty(e.PropertyName))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        public void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Raises the <see cref="Window" />.Closed event.
        /// Removes event listeners on close, to prevent memory leaks.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            manageVotesViewModel.PropertyChanged -= ManageVotesViewModel_PropertyChanged;

            base.OnClosed(e);
        }

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
                                manageVotesViewModel.ReplaceTask(selectedVote, "");
                            else
                                manageVotesViewModel.ReplaceTask(selectedVote, newTask);
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
                            manageVotesViewModel.PartitionChildren(selectedVote);
                        }
                    }
                }
            }
        }

        #endregion Context Menu Events

        #region Context Menu Utility
        ListBox? newTaskBox = null;
        readonly List<MenuItem> ContextMenuCommands = [];
        readonly List<MenuItem> ContextMenuTasks = [];
        List<MenuItem> ContextMenuItems { get; } = [];
        readonly MenuItem separator = new() { Header = "-" };


        /// <summary>
        /// Create the basic command menu items for the context menu.
        /// </summary>
        private void CreateContextMenuCommands()
        {
            MenuItem newTask = new MenuItem
            {
                Header = "New Task..."
            };
            newTask.Click += newTask_Click;
            //newTask.ToolTip = "Create a new task value.";

            MenuItem clearTask = new()
            {
                Header = "Clear Task"
            };
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
            foreach (var task in manageVotesViewModel.TaskList.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
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
            MenuItem mi = new()
            {
                Header = name
            };
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
            // YesButton Clicked! Let's hide our InputBox and handle the input text.
            InputBox.IsVisible = false;

            string newTask = InputTextBox.Text?.RemoveUnsafeCharacters().Trim() ?? "";

            // Clear InputBox.
            InputTextBox.Text = string.Empty;

            // Do something with the Input
            AddTaskToContextMenu(newTask);
            manageVotesViewModel.AddUserDefinedTask(newTask);

            // Update the selected item of the list box
            if (newTaskBox?.SelectedItem is VoteLineBlock selectedVote)
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

        private bool HasChildLines(VoteLineBlock vote)
        {
            return (vote.Lines.Count > 1 && vote.Lines.Skip(1).All(v => v.Depth > 0));
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
