using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using NetTally.Collections;
using NetTally.Navigation;
using NetTally.ViewModels;
using NetTally.ViewModels.Commands;
using NetTally.VoteCounting;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for reorder tasks window.
    /// </summary>
    public partial class ReorderTasksWindow : Window, IActivable, INotifyPropertyChanged, ICommandFilter
    {
        #region Setup and construction
        readonly ViewModel mainViewModel;
        readonly ObservableCollectionExt<string> taskCollection;
        readonly ILogger<ReorderTasksWindow> logger;

        /// <summary>
        /// The ListCollectionView that the ListBox uses to display information from.
        /// Necessary for XAML binding.
        /// </summary>
        public ListCollectionView TaskView { get; }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when creating the window.
        /// </summary>
        /// <param name="viewModel">The primary view model of the program</param>
        /// <param name="loggerFactory">DI for the local logger.</param>
        public ReorderTasksWindow(ViewModel viewModel, ILogger<ReorderTasksWindow> logger)
        {
            // Save dependencies
            mainViewModel = viewModel;
            taskCollection = viewModel.TaskList;
            this.logger = logger;

            // Set up commands
            MoveUpCommand = new RelayCommand(this, nameof(MoveUpCommand), MoveTaskUp, CanMoveTaskUp);
            MoveDownCommand = new RelayCommand(this, nameof(MoveDownCommand), MoveTaskDown, CanMoveTaskDown);
            SortAlphaCommand = new RelayCommand(this, nameof(SortAlphaCommand), SortTasksAlpha, CanSortTasks);
            SortOriginalCommand = new RelayCommand(this, nameof(SortOriginalCommand), SortTasksOriginal, CanSortTasks);

            // Create view of the task collection for display in the window.
            TaskView = new ListCollectionView(taskCollection);

            // Set the data context for the XAML frontend.
            DataContext = this;

            // Initialize the window components
            InitializeComponent();

            // Add event watchers
            viewModel.PropertyChanged += MainViewModel_PropertyChanged;
            taskCollection.CollectionChanged += TaskList_CollectionChanged;
            TaskView.CurrentChanged += TaskView_CurrentChanged;
        }

        /// <summary>
        /// Called when the window closes.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure PropertyChanged delegate isn't left dangling.
            mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;
            taskCollection.CollectionChanged -= TaskList_CollectionChanged;
            TaskView.CurrentChanged -= TaskView_CurrentChanged;
        }
        #endregion

        #region Commands
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand SortAlphaCommand { get; }
        public ICommand SortOriginalCommand { get; }

        #region Command Filters

        public PropertyFilterListOption PropertyFilterListMode => PropertyFilterListOption.Exclude;

        /// <summary>
        /// Exclude TaskView from the normal CanExecuteChanged event updates
        /// so that it can propogate to the XAML UI and update the current
        /// position before the commands are checked on.
        /// </summary>
        public HashSet<string> PropertyFilterList => new HashSet<string>() { "TaskView" };

        #endregion

        #region Can Execute functions
        private bool CanMoveTaskUp(object? parameter)
        {
            if (taskCollection.Count < 2)
                return false;

            if (parameter is int position)
            {
                return position > 0;
            }

            return false;
        }

        private bool CanMoveTaskDown(object? parameter)
        {
            if (taskCollection.Count < 2)
                return false;

            if (parameter is int position)
            {
                return position >= 0 && position < taskCollection.Count - 1;
            }

            return false;
        }

        private bool CanSortTasks(object? parameter)
        {
            return taskCollection.Count > 1;
        }
        #endregion

        #region Execute functions
        private void MoveTaskUp(object? parameter)
        {
            if (parameter is int position)
            {
                if (position >= 0 && position < taskCollection.Count)
                {
                    taskCollection.Move(position, position - 1);
                }
            }
        }

        private void MoveTaskDown(object? parameter)
        {
            if (parameter is int position)
            {
                if (position >= 0 && position < taskCollection.Count)
                {
                    taskCollection.Move(position, position + 1);
                }
            }
        }

        private void SortTasksAlpha(object? parameter)
        {
            taskCollection.Sort();
        }

        private void SortTasksOriginal(object? parameter)
        {
            var currentSelection = TaskView.CurrentItem;

            mainViewModel.ResetTasksOrder(TasksOrdering.AsTallied);

            if (currentSelection != null)
                TaskView.MoveCurrentTo(currentSelection);
        }
        #endregion
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Watching for notifications from monitored classes
        /// <summary>
        /// Watch for notifications from the main view model about changes in the vote backend.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Tasks" || e.PropertyName == "TaskList")
            {
                logger.LogDebug($"Received notification of property change from MainViewModel: {e.PropertyName}.");

                TaskView.Refresh();
            }
        }

        /// <summary>
        /// Watch for events from the task list collection, and pass that on
        /// to any classes watching for changes, such as the CanExecute checks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("TaskList");
        }

        /// <summary>
        /// Watch for notifications from the TaskView about changing the current selection.
        /// Update the up and down buttons accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskView_CurrentChanged(object? sender, EventArgs e)
        {
            // Call once to notifiy the XAML view, which must be done before the commands update,
            // so that the XAML can update to the TaskView's current position.
            OnPropertyChanged("TaskView");
            // And a separate call to get the commands to update themselves.
            OnPropertyChanged("Command");
        }
        #endregion

    }
}
