using System;
using System.ComponentModel;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views
{
    /// <summary>
    /// Code-behind for window used for reordering tasks.
    /// </summary>
    public partial class ReorderTasks : Window
    {
        private readonly TasksViewModelF tasksViewModel;
        private readonly ILogger<ReorderTasks> logger;

        public ReorderTasks(
            TasksViewModelF tasksViewModel,
            ILogger<ReorderTasks> logger)
        {
            this.tasksViewModel = tasksViewModel;
            this.logger = logger;

            InitializeComponent();
            DataContext = this.tasksViewModel;

            this.tasksViewModel.PropertyChanged += TasksViewModel_PropertyChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (tasksViewModel != null)
            {
                tasksViewModel.PropertyChanged -= TasksViewModel_PropertyChanged;
            }

            base.OnClosed(e);
        }

        private void TasksViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(tasksViewModel.SaveCommand))
            {
                logger.LogDebug("Reordered tasks were saved.");
                Close();
            }
        }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#if DEBUG
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public ReorderTasks()
        {
            InitializeComponent();
        }
#endif
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
