using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.ViewModels;

namespace NetTally.Views
{
    /// <summary>
    /// Interaction logic for GlobalOptions.xaml
    /// </summary>
    public partial class GlobalOptions : Window, IActivable
    {
        private readonly GlobalOptionsViewModel globalOptionsViewModel;
        private readonly ILogger<GlobalOptions> logger;

        public GlobalOptions(
            GlobalOptionsViewModel globalOptionsViewModel,
            ILogger<GlobalOptions> logger)
        {
            this.globalOptionsViewModel = globalOptionsViewModel;
            this.logger = logger;

            this.globalOptionsViewModel.SaveCompleted += GlobalOptionsViewModel_SaveCompleted;

            InitializeComponent();
            DataContext = globalOptionsViewModel;
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            globalOptionsViewModel.SaveCompleted -= GlobalOptionsViewModel_SaveCompleted;
            base.OnClosing(e);
        }

        private void GlobalOptionsViewModel_SaveCompleted()
        {
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
