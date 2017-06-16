using System;
using System.Collections.Generic;
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

namespace NetTally
{
    /// <summary>
    /// Interaction logic for QuestOptionsNew.xaml
    /// </summary>
    public partial class QuestOptionsNew : Window
    {
        public QuestOptionsNew()
        {
            InitializeComponent();
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

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
