using System.Threading.Tasks;
using System.Windows;
using NetTally.Navigation;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for GlobalOptionsWindow.xaml
    /// </summary>
    public partial class GlobalOptionsWindow : Window, IActivable
    {
        public GlobalOptionsWindow()
        {
            InitializeComponent();
        }

        public Task ActivateAsync(object? parameter)
        {
            if (parameter is (Window owner, MainViewModel model))
            {
                this.Owner = owner;
                this.DataContext = model;
            }

            return Task.CompletedTask;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void resetAllButton_Click(object sender, RoutedEventArgs e)
        {
            allowRankedVotes.IsChecked = true;
            rankedVoteAlgorithm.SelectedIndex = 0;

            globalSpoilers.IsChecked = false;

            debugMode.IsChecked = false;
        }
    }
}
