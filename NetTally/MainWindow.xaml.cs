using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Serialization;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Filename to record quests
        const string questFile = "questlist.xml";
        Tally tally = new Tally();
        Quests quests = new Quests();

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = quests;
            this.resultsWindow.DataContext = tally;

            LoadQuests();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveQuests();
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


        #region Serialization
        private void LoadQuests()
        {
            string filepath = Path.Combine(System.Windows.Forms.Application.CommonAppDataPath, questFile);

            if (File.Exists(filepath))
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer sr = new XmlSerializer(typeof(Quests));
                    Quests qs = (Quests)sr.Deserialize(fs);
                    quests.CurrentQuest = qs.CurrentQuest;
                }
            }
        }

        private void SaveQuests()
        {
            string filepath = Path.Combine(System.Windows.Forms.Application.CommonAppDataPath, questFile);

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    XmlSerializer sr = new XmlSerializer(typeof(Quests));
                    sr.Serialize(tw, quests);
                }
            }
        }

        private void InitQuests()
        {
            quests.AddToQuestList(new Quest("awake-already-homura-nge-pmmm-fusion-quest.11111", 1, 100));
            quests.AddToQuestList(new Quest("sayakaquest-thread-10-glory-to-the-death.87", 101, 200));
            quests.AddToQuestList(new Quest("puella-magi-adfligo-systema.2538", 36743, 37322));
        }

        #endregion

    }
}
