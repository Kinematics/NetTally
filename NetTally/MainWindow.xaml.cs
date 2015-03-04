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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Tally tally = new Tally();

        public MainWindow()
        {
            InitializeComponent();
            this.resultsWindow.DataContext = tally;
            //this.DataContext = tally;
        }

        private async void tallyButton_Click(object sender, RoutedEventArgs e)
        {
            tallyButton.IsEnabled = false;
            clearTallyCacheButton.IsEnabled = false;

            try
            {
                await tally.Run("puella-magi-adfligo-systema.2538", 36743, 37300);
            }
            finally
            {
                tallyButton.IsEnabled = true;
                clearTallyCacheButton.IsEnabled = true;
            }
        }

        private void clearTallyCacheButton_Click(object sender, RoutedEventArgs e)
        {
            tallyButton.IsEnabled = false;
            clearTallyCacheButton.IsEnabled = false;

            try
            {
                tally.ClearPageCache();
            }
            finally
            {
                tallyButton.IsEnabled = true;
                clearTallyCacheButton.IsEnabled = true;
            }
        }
    }
}
