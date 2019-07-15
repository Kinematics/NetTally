using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using NetTally.VoteCounting;
using NetTally.ViewModels;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using System.Threading.Tasks;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for ReorderTasks.xaml
    /// </summary>
    public partial class ReorderTasksWindow : Window, IActivable
    {
        #region Constructor and variables
        readonly MainViewModel mainViewModel;
        readonly ILogger<ReorderTasksWindow> logger;
        public ListCollectionView TaskView { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mainViewModel">The primary view model of the program</param>
        public ReorderTasksWindow(MainViewModel mainViewModel, ILoggerFactory loggerFactory)
        {
            this.mainViewModel = mainViewModel;
            this.logger = loggerFactory.CreateLogger<ReorderTasksWindow>();

            InitializeComponent();

            this.mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

            // Create filtered, sortable views into the collection for display in the window.
            TaskView = new ListCollectionView(this.mainViewModel.TaskList);

            // Initialize starting selected positions
            TaskView.MoveCurrentToPosition(-1);

            // Set the data context for binding.
            DataContext = this;

            TaskView.Refresh();
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }

        protected override void OnClosed(EventArgs e)
        {
            mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;

            base.OnClosed(e);
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
            if (e.PropertyName == "Votes" || e.PropertyName == "Tasks" || e.PropertyName == "TaskList")
            {
                logger.LogDebug($"Received notification of property change from MainViewModel: {e.PropertyName}.");
                TaskView.Refresh();
            }
        }

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
