using System;
using System.Collections.Specialized;
using System.ComponentModel;
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
    /// Interaction logic for quest options window.
    /// </summary>
    public partial class QuestOptionsWindow : Window, IActivable
    {
        #region Setup and construction
        readonly ILogger<QuestOptionsWindow> logger;
        readonly ViewModel viewModel;

        public QuestOptionsWindow(ViewModel model, ILoggerFactory loggerFactory)
        {
            viewModel = model;
            logger = loggerFactory.CreateLogger<QuestOptionsWindow>();

            DataContext = model;

            InitializeComponent();

            linkedQuests.SelectionChanged += LinkedQuests_SelectionChanged;
            availableQuests.SelectionChanged += AvailableQuests_SelectionChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            linkedQuests.SelectionChanged -= LinkedQuests_SelectionChanged;
            availableQuests.SelectionChanged -= AvailableQuests_SelectionChanged;
        }

        public Task ActivateAsync(object parameter)
        {
            if (parameter is Window owner)
            {
                this.Owner = owner;
            }

            return Task.CompletedTask;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Notify the view model when the available quests' selection changes,
        /// so that it can initiate a CanExecute call on commands.
        /// </summary>
        private void AvailableQuests_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PropertyChangedEventArgs args = new PropertyChangedEventArgs(nameof(availableQuests));
            viewModel.ExternalPropertyChanged(this, args);
        }

        /// <summary>
        /// Notify the view model when the list of linked quests' selection changes,
        /// so that it can initiate a CanExecute call on commands.
        /// </summary>
        private void LinkedQuests_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PropertyChangedEventArgs args = new PropertyChangedEventArgs(nameof(linkedQuests));
            viewModel.ExternalPropertyChanged(this, args);
        }
        #endregion

        #region Window element event handlers
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
        #endregion
    }
}
