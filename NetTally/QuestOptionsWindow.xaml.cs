using System.Windows;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for QuestOptionsWindow.xaml
    /// </summary>
    public partial class QuestOptionsWindow : Window
    {
        public QuestOptionsWindow()
        {
            InitializeComponent();
        }

        private void resetFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            useCustomPostFilters.IsChecked = false;
            useCustomTaskFilters.IsChecked = false;
            useCustomThreadmarkFilters.IsChecked = false;
            useCustomUsernameFilters.IsChecked = false;

            customPostFilters.Text = "";
            customTaskFilters.Text = "";
            customThreadmarkFilters.Text = "";
            customUsernameFilters.Text = "";
        }

        private void resetOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            forbidVoteLabelPlanNames.IsChecked = false;
            whitespaceAndPunctuationIsSignificant.IsChecked = false;
            disableProxyVotes.IsChecked = false;
            forcePinnedProxyVotes.IsChecked = false;
            ignoreSpoilers.IsChecked = false;
            trimExtendedText.IsChecked = false;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
