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
            DataContext = AdvancedOptions.Instance;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void resetAllButton_Click(object sender, RoutedEventArgs e)
        {
            allowRankedVotes.IsChecked = true;

            forbidVoteLabelPlanNames.IsChecked = false;
            whitespaceAndPunctuationIsSignificant.IsChecked = false;
            disableProxyVotes.IsChecked = false;
            ignoreSpoilers.IsChecked = false;
            trimExtendedText.IsChecked = false;

            globalSpoilers.IsChecked = false;

            debugMode.IsChecked = false;
        }
    }
}
