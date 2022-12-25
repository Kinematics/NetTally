using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.ViewModels;

namespace NetTally.Views
{
    /// <summary>
    /// Interaction logic for GlobalOptions2.xaml
    /// </summary>
    public partial class GlobalOptions2 : Window, IActivable
    {
        private readonly ILogger<GlobalOptions2> logger;

        public GlobalOptions2(
            GlobalOptionsViewModel globalOptionsViewModel,
            ILogger<GlobalOptions2> logger)
        {
            this.logger = logger;

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

        private void ResetAllButton_Click(object sender, RoutedEventArgs e)
        {
            rankedVoteAlgorithm.SelectedIndex = 0;
            allowUsersToUpdatePlans.IsChecked = null;
            trackPostAuthorsUniquely.IsChecked = false;
            globalSpoilers.IsChecked = false;
            displayPlansWithNoVotes.IsChecked = false;
            debugMode.IsChecked = false;
            disableWebProxy.IsChecked = false;

            logger.LogDebug("Global options have been reset.");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
