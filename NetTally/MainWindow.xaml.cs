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
        Quests quests = new Quests();

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = quests;
            this.resultsWindow.DataContext = tally;

            InitQuests();
        }

        private void InitQuests()
        {

            quests.AddToQuestList(new Quest("awake-already-homura-nge-pmmm-fusion-quest.11111", 1, 100));
            quests.AddToQuestList(new Quest("sayakaquest-thread-10-glory-to-the-death.87", 101, 200));
            quests.AddToQuestList(new Quest("puella-magi-adfligo-systema.2538", 36743, 37322));
        }

        private async void tallyButton_Click(object sender, RoutedEventArgs e)
        {
            tallyButton.IsEnabled = false;
            clearTallyCacheButton.IsEnabled = false;

            try
            {
                await tally.Run(quests.CurrentQuest.Name, quests.CurrentQuest.StartPost, quests.CurrentQuest.EndPost);
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

        private void copyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(tally.TallyResults);
        }
    }
}
