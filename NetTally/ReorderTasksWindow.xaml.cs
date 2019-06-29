using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using NetTally.VoteCounting;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for ReorderTasks.xaml
    /// </summary>
    public partial class ReorderTasksWindow : Window
    {
        #region Constructor and variables
        public ListCollectionView TaskView { get; } = new ListCollectionView(new string[] { });

        MainViewModel? MainViewModel { get; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReorderTasksWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mainViewModel">The primary view model of the program</param>
        public ReorderTasksWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();

            MainViewModel = mainViewModel;

            MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

            // Create filtered, sortable views into the collection for display in the window.
            if (MainViewModel.TaskList != null)
            {
                TaskView = new ListCollectionView(MainViewModel.TaskList);
            }

            // Initialize starting selected positions
            TaskView.MoveCurrentToPosition(-1);

            // Set the data context for binding.
            DataContext = this;

            TaskView.Refresh();
        }

        protected override void OnClosed(EventArgs e)
        {
            MainViewModel!.PropertyChanged -= MainViewModel_PropertyChanged;

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
            if (e.PropertyName == "Votes" || e.PropertyName == "Tasks")
            {
                TaskView.Refresh();
            }
        }

        private void up_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel!.DecreaseTaskPosition(TaskView.CurrentPosition);
        }

        private void down_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel!.IncreaseTaskPosition(TaskView.CurrentPosition);
        }

        private void alpha_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel!.ResetTasksOrder(TasksOrdering.Alphabetical);
        }

        private void default_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel!.ResetTasksOrder(TasksOrdering.AsTallied);
        }
        #endregion
    }
}
