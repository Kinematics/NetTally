using System;
using System.ComponentModel;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views
{
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

            DataContext = this.tasksViewModel;

            tasksViewModel.PropertyChanged += TasksViewModel_PropertyChanged;
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
                logger.LogDebug("Reordered tasks were saved.");
                Close();
            }
        }
    }
}
