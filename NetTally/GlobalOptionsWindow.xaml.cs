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
            ignoreSymbols.IsChecked = true;
            trimExtendedText.IsChecked = false;
            ignoreSpoilers.IsChecked = false;
            globalSpoilers.IsChecked = false;
            allowVoteLabelPlanNames.IsChecked = true;
            debugMode.IsChecked = false;
        }
    }
}
