using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        // Local holding variables
        Tally tally = new Tally();
        Quests quests = new Quests();

        /// <summary>
        /// Function that's run when the program first starts.
        /// Set up the data context links with the local variables.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = quests;
            this.resultsWindow.DataContext = tally;
            this.partitionedVotes.DataContext = tally;
            this.partitionByBlock.DataContext = tally;
            this.partitionByLine.DataContext = tally;

            LoadQuests();
        }

        /// <summary>
        /// When the program closes, save the current list of quests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveQuests();
        }

        /// <summary>
        /// Start running the tally on the currently selected quest and post range.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void tallyButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupEditQuestName();

            if (quests.CurrentQuest == null)
                return;

            tallyButton.IsEnabled = false;
            clearTallyCacheButton.IsEnabled = false;

            try
            {
                await tally.Run(quests.CurrentQuest.Name, quests.CurrentQuest.StartPost, quests.CurrentQuest.EndPost);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                tallyButton.IsEnabled = true;
                clearTallyCacheButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Clear the page cache so that subsequent tally requests load the pages from the network
        /// rather than from the cache.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearTallyCacheButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupEditQuestName();

            tallyButton.IsEnabled = false;
            clearTallyCacheButton.IsEnabled = false;

            try
            {
                tally.ClearPageCache();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                tallyButton.IsEnabled = true;
                clearTallyCacheButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Event to cause the program to copy the current contents of the the tally
        /// results (ie: what's shown in the main text window) to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupEditQuestName();
            Clipboard.SetText(tally.TallyResults);
        }

        /// <summary>
        /// If either start post or end post text boxes get focus, select the entire
        /// contents so that they're easy to replace.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void post_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb != null)
                tb.SelectAll();
        }

        /// <summary>
        /// If either start post or end post text boxes are clicked on with the mouse,
        /// and they don't already have focus, explicitly set their focus.  This will
        /// in turn cause them to select the inner contents.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void post_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (sender as TextBox);

            if (tb != null)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus();
                }
            }
        }


        #region Adding and removing quests
        /// <summary>
        /// Event handler for adding a new quest.
        /// Create a new quest, and make the edit textbox visible so that the user can rename it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addQuestButton_Click(object sender, RoutedEventArgs e)
        {
            quests.AddToQuestList(new Quest());
            quests.SetCurrentQuestByName("New Entry");
            editQuestName.Visibility = Visibility.Visible;
            editQuestName.Focus();
            editQuestName.SelectAll();
        }

        /// <summary>
        /// When modifying the quest name, hitting enter will complete the entry,
        /// and hitting escape will cancel (and delete) the entry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editQuestName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CleanupEditQuestName();
            }
            else if (e.Key == Key.Escape)
            {
                quests.RemoveCurrentQuest();
                CleanupEditQuestName();
            }
        }

        /// <summary>
        /// Remove the current quest from the quest list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeQuestButton_Click(object sender, RoutedEventArgs e)
        {
            quests.RemoveCurrentQuest();
            CleanupEditQuestName();
        }

        /// <summary>
        /// Any user interaction that ends the use of the text box for editing the quest
        /// name needs to ensure some cleanup occurs.
        /// </summary>
        private void CleanupEditQuestName()
        {
            if (editQuestName.Visibility == Visibility.Visible)
            {
                editQuestName.Visibility = Visibility.Hidden;
                quests.CurrentQuest.CleanName();
                var cqn = quests.CurrentQuest.Name;
                quests.Update();
                quests.SetCurrentQuestByName(cqn);
                startPost.Focus();
            }
        }
        #endregion

        #region Serialization
        /// <summary>
        /// Load any saved quest information when starting up the program.
        /// </summary>
        private void LoadQuests()
        {
            string filepath = Path.Combine(System.Windows.Forms.Application.CommonAppDataPath, questFile);

            if (File.Exists(filepath))
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                {
                    XmlSerializer sr = new XmlSerializer(typeof(Quests));
                    Quests qs = (Quests)sr.Deserialize(fs);
                    quests.SetCurrentQuestByName(qs.CurrentQuestName);
                }
            }
        }

        /// <summary>
        /// Save any quest information when shutting the program down.
        /// </summary>
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

        #endregion
    }
}
