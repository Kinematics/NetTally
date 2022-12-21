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
    /// Interaction logic for QuestOptions2.xaml
    /// </summary>
    public partial class QuestOptions2 : Window
    {
        private readonly QuestOptionsViewModel questOptionsViewModel;
        private readonly ILogger<QuestOptions2> logger;

        public QuestOptions2(QuestOptionsViewModel questOptionsViewModel,
            ILogger<QuestOptions2> logger)
        {
            this.questOptionsViewModel = questOptionsViewModel;
            this.logger = logger;

            this.questOptionsViewModel.SaveCompleted += QuestOptionsViewModel_SaveCompleted;

            DataContext = this.questOptionsViewModel;
            InitializeComponent();
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is Window owner)
            {
                Owner = owner;
            }

            return Task.CompletedTask;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (questOptionsViewModel.ThreadName == Quest.NewThreadEntry)
            {
                QuestUrlBox.Focus();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            questOptionsViewModel.SaveCompleted -= QuestOptionsViewModel_SaveCompleted;
        }

        #region Window element event handlers
        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
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

        private void ClearOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            whitespaceAndPunctuationIsSignificant.IsChecked = false;
            caseIsSignificant.IsChecked = false;
            forcePlanReferencesToBeLabeled.IsChecked = false;
            forbidVoteLabelPlanNames.IsChecked = false;
            allowUsersToUpdatePlans.IsChecked = false;
            disableProxyVotes.IsChecked = false;
            forcePinnedProxyVotes.IsChecked = false;
            ignoreSpoilers.IsChecked = false;
            trimExtendedText.IsChecked = false;
            useRSSThreadmarks.IsChecked = null;

            logger.LogDebug("Quest options have been reset.");
        }

        private void TextEntry_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TextEntry_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    tb.Focus();
                    e.Handled = true;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void QuestOptionsViewModel_SaveCompleted()
        {
            Close();
        }
        #endregion

    }
}
