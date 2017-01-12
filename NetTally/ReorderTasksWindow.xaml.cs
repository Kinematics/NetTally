using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NetTally.Utility;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for ReorderTasks.xaml
    /// </summary>
    public partial class ReorderTasksWindow : Window, INotifyPropertyChanged
    {
        #region Constructor and variables
        public ListCollectionView TaskView { get; }

        MainViewModel MainViewModel { get; }

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

            // Update the displayed order immediately
            UpdateTaskList();
        }
        #endregion

        private void UpdateTaskList()
        {
            VoteCounter.Instance.OrderedTaskList = TaskView.SourceCollection as List<string>;
            OnPropertyChanged("Votes");
            TaskView.Refresh();
        }

        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            if (e.PropertyName == "Votes")
            {
                TaskView.Refresh();
            }
        }

        private void up_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.TaskList.Swap(TaskView.CurrentPosition, TaskView.CurrentPosition - 1);
            UpdateTaskList();
        }

        private void down_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.TaskList.Swap(TaskView.CurrentPosition, TaskView.CurrentPosition + 1);
            UpdateTaskList();
        }

        private void alpha_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.TaskList.Sort();
            UpdateTaskList();
        }

        private void default_Click(object sender, RoutedEventArgs e)
        {
            MainViewModel.TaskList.Sort((a,b) => (MainViewModel.KnownTasks.ToList().IndexOf(a).CompareTo(MainViewModel.KnownTasks.ToList().IndexOf(b))));
            UpdateTaskList();
        }
        #endregion
    }
}
