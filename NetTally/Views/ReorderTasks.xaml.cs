using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using NetTally.Debugging.Logging;
using NetTally.ViewModels;

namespace NetTally.Views
{
    /// <summary>
    /// Code-behind for window used for reordering tasks.
    /// </summary>
    public partial class ReorderTasks : Window
    {
        private readonly TasksViewModel tasksViewModel;
        private readonly ILogger<ReorderTasks> logger;

        public ReorderTasks(
            TasksViewModel tasksViewModel,
            ILogger<ReorderTasks> logger)
        {
            this.tasksViewModel = tasksViewModel;
            this.logger = logger;

            InitializeComponent();
            DataContext = tasksViewModel;

            this.tasksViewModel.PropertyChanged += TasksViewModel_PropertyChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            tasksViewModel.PropertyChanged -= TasksViewModel_PropertyChanged;
            base.OnClosed(e);
        }

        private void TasksViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(tasksViewModel.SaveCommand))
            {
                logger.ReorderedTasksSaved();
                Close();
            }
        }
    }
}
