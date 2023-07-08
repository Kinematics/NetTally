using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using NetTally.ViewModels;

namespace NetTally.Views
{
    /// <summary>
    /// Interaction logic for ReorderTasks2.xaml
    /// </summary>
    public partial class ReorderTasks : Window
    {
        private readonly ILogger<ReorderTasks> logger;

        public ReorderTasks(
            TasksViewModel tasksViewModel,
            ILogger<ReorderTasks> logger)
        {
            this.logger = logger;

            TasksView = CollectionViewSource.GetDefaultView(tasksViewModel.Tasks);

            InitializeComponent();
            DataContext = tasksViewModel;
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }

        public ICollectionView TasksView { get; }
    }
}
