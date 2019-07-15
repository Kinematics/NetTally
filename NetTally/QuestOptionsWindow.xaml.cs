using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for QuestOptionsWindow.xaml
    /// </summary>
    public partial class QuestOptionsWindow : Window, IActivable
    {
        readonly ILogger<QuestOptionsWindow> logger;

        public QuestOptionsWindow(MainViewModel model, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<QuestOptionsWindow>();

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

        private void resetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            customPostFilters.Clear();
            var bindingExpression = BindingOperations.GetBindingExpression(customPostFilters, TextBox.TextProperty);
            bindingExpression.UpdateSource();

            customTaskFilters.Clear();
            bindingExpression = BindingOperations.GetBindingExpression(customTaskFilters, TextBox.TextProperty);
            bindingExpression.UpdateSource();

            customThreadmarkFilters.Clear();
            bindingExpression = BindingOperations.GetBindingExpression(customThreadmarkFilters, TextBox.TextProperty);
            bindingExpression.UpdateSource();

            customUsernameFilters.Clear();
            bindingExpression = BindingOperations.GetBindingExpression(customUsernameFilters, TextBox.TextProperty);
            bindingExpression.UpdateSource();


            useCustomPostFilters.IsChecked = false;
            useCustomTaskFilters.IsChecked = false;
            useCustomThreadmarkFilters.IsChecked = false;
            useCustomUsernameFilters.IsChecked = false;

            logger.LogDebug("Quest filters have been reset.");
        }

        private void resetOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            forbidVoteLabelPlanNames.IsChecked = false;
            whitespaceAndPunctuationIsSignificant.IsChecked = false;
            caseIsSignificant.IsChecked = false;
            disableProxyVotes.IsChecked = false;
            forcePinnedProxyVotes.IsChecked = false;
            ignoreSpoilers.IsChecked = false;
            trimExtendedText.IsChecked = false;

            logger.LogDebug("Quest options have been reset.");
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
