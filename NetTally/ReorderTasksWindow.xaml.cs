using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using NetTally.VoteCounting;
using NetTally.ViewModels;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using System.Threading.Tasks;
using NetTally.CustomEventArgs;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for reorder tasks window.
    /// </summary>
    public partial class ReorderTasksWindow : Window, IActivable
    {
        #region Setup and construction
        readonly MainViewModel mainViewModel;
        readonly ILogger<ReorderTasksWindow> logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="viewModel">The primary view model of the program</param>
        public ReorderTasksWindow(MainViewModel viewModel, ILoggerFactory loggerFactory)
        {
            // Save dependencies
            mainViewModel = viewModel;
            logger = loggerFactory.CreateLogger<ReorderTasksWindow>();

            // Create view of the task collection for display in the window.
            TaskView = new ListCollectionView(mainViewModel.TaskList);
            TaskView.MoveCurrentToPosition(-1);

            // Set the data context for the XAML frontend.
            DataContext = this;

            // Initialize the window components
            InitializeComponent();

            // Add event watchers
            mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
            TaskView.CurrentChanged += TaskView_CurrentChanged;

            TaskView.MoveCurrentToFirst();
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }
        #endregion

        #region Window overrides
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure PropertyChanged delegate isn't left dangling.
            mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;
            TaskView.CurrentChanged -= TaskView_CurrentChanged;
        }
        #endregion

        #region Window binding properties for the XAML code
        /// <summary>
        /// The ListCollectionView that the ListBox uses to display information from.
        /// </summary>
        public ListCollectionView TaskView { get; }
        #endregion

        #region Watching for notifications from monitored classes
        /// <summary>
        /// Watch for notifications from the main view model about changes in the vote backend.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Votes" || e.PropertyName == "Tasks" || e.PropertyName == "TaskList")
            {
                logger.LogDebug($"Received notification of property change from MainViewModel: {e.PropertyName}.");

                TaskView.Refresh();
            }
        }

        /// <summary>
        /// Watch for notifications from the TaskView about changing the current selection.
        /// Update the up and down buttons accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TaskView_CurrentChanged(object sender, EventArgs e)
        {
            if (TaskView.Count < 2)
            {
                up.IsEnabled = false;
                down.IsEnabled = false;
            }
            else
            {
                up.IsEnabled = true;
                down.IsEnabled = true;

                if (TaskView.CurrentPosition == 0)
                {
                    up.IsEnabled = false;
                }
                else if (TaskView.CurrentPosition == TaskView.Count - 1)
                {
                    down.IsEnabled = false;
                }
            }
        }
        #endregion

        #region Window element event handlers
        private void up_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.DecreaseTaskPosition(TaskView.CurrentPosition);
        }

        private void down_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.IncreaseTaskPosition(TaskView.CurrentPosition);
        }

        private void alpha_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.ResetTasksOrder(TasksOrdering.Alphabetical);
        }

        private void default_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.ResetTasksOrder(TasksOrdering.AsTallied);
        }
        #endregion
    }
}
