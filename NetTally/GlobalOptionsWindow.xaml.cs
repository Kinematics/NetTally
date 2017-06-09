using System.Windows;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for GlobalOptionsWindow.xaml
    /// </summary>
    public partial class GlobalOptionsWindow : Window
    {
        public GlobalOptionsWindow()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void resetAllButton_Click(object sender, RoutedEventArgs e)
        {
            allowRankedVotes.IsChecked = true;
            rankedVoteAlgorithm.SelectedIndex = 0;

            forbidVoteLabelPlanNames.IsChecked = false;
            whitespaceAndPunctuationIsSignificant.IsChecked = false;
            disableProxyVotes.IsChecked = false;
            forcePinnedProxyVotes.IsChecked = false;
            ignoreSpoilers.IsChecked = false;
            trimExtendedText.IsChecked = false;

            globalSpoilers.IsChecked = false;

            debugMode.IsChecked = false;
        }

        private void rankedVoteAlgorithm_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
