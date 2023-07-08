using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.ViewModels;

namespace NetTally.Views.original
{
    /// <summary>
    /// Interaction logic for the global options window.
    /// </summary>
    public partial class GlobalOptions : Window, IActivable
    {
        #region Setup and construction
        readonly ILogger<GlobalOptions> logger;

        public GlobalOptions(ViewModel model, ILogger<GlobalOptions> logger)
        {
            this.logger = logger;

            InitializeComponent();

            DataContext = model;
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

        #region Window element event handlers
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void resetAllButton_Click(object sender, RoutedEventArgs e)
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
        #endregion
    }
}
