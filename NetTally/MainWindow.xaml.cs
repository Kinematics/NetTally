using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Storage vars
        const string questFile = "questlist.xml";
        Properties.Settings settings = new Properties.Settings();

        // Collections for holding quests
        public ICollectionView QuestCollectionView { get; }
        QuestCollection questCollection;

        // Locals for managing the tally
        IForumAdapter forumAdapter;
        Tally tally;
        CancellationTokenSource cts;

        /// <summary>
        /// Function that's run when the program first starts.
        /// Set up the data context links with the local variables.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Set tally vars
            forumAdapter = new SVForumAdapter();
            tally = new Tally(forumAdapter);

            // Deserialize XML of quests
            var wrapper = LoadQuestsContract();

            if (wrapper == null)
                questCollection = new QuestCollection();
            else
                questCollection = wrapper.QuestCollection;

            QuestCollectionView = CollectionViewSource.GetDefaultView(questCollection);

            if (wrapper != null)
                QuestCollectionView.MoveCurrentTo(questCollection.FirstOrDefault(q => q.Name == wrapper.CurrentQuest));

            // Sort the collection view
            var sortDesc = new SortDescription("Name", ListSortDirection.Ascending);
            QuestCollectionView.SortDescriptions.Add(sortDesc);

            // Set up data contexts
            DataContext = QuestCollectionView;

            resultsWindow.DataContext = tally;
            partitionedVotes.DataContext = tally;
            partitionByBlock.DataContext = tally;
            partitionByLine.DataContext = tally;
            tryLastThreadmark.DataContext = tally;

            LoadSettings();
        }

        /// <summary>
        /// When the program closes, save the current list of quests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
            SaveQuests();
        }

        private void LoadSettings()
        {
            tally.CheckForLastThreadmark = settings.CheckForLastThreadmark;
            tally.UseVotePartitions = settings.UseVotePartitions;
            tally.PartitionByLine = settings.PartitionByLine;
        }

        private void SaveSettings()
        {
            settings.CheckForLastThreadmark = tally.CheckForLastThreadmark;
            settings.UseVotePartitions = tally.UseVotePartitions;
            settings.PartitionByLine = tally.PartitionByLine;
            settings.Save();
        }


        /// <summary>
        /// Start running the tally on the currently selected quest and post range.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void tallyButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupEditQuestName();

            if (QuestCollectionView.CurrentItem == null)
                return;

            tallyButton.IsEnabled = false;

            try
            {
                cts = new CancellationTokenSource();
                await tally.Run(QuestCollectionView.CurrentItem as IQuest, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // got a cancel request somewhere
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                cts.Cancel();
            }
            finally
            {
                tallyButton.IsEnabled = true;
                cts.Dispose();
                cts = null;
            }
        }


        private void cancelTally_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
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
        private void textEntry_GotFocus(object sender, RoutedEventArgs e)
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
        private void textEntry_PreviewMouseDown(object sender, MouseButtonEventArgs e)
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

        /// <summary>
        /// Event handler for adding a new quest.
        /// Create a new quest, and make the edit textbox visible so that the user can rename it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addQuestButton_Click(object sender, RoutedEventArgs e)
        {
            var newEntry = questCollection.AddNewQuest();
            if (newEntry == null)
            {
                newEntry = questCollection.FirstOrDefault(q => q.Name == Quest.NewEntryName);
                if (newEntry == null)
                    return;
            }

            QuestCollectionView.MoveCurrentTo(newEntry);

            editQuestName.Visibility = Visibility.Visible;
            editQuestName.Focus();
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
                questCollection.Remove(QuestCollectionView.CurrentItem as IQuest);
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
            questCollection.Remove(QuestCollectionView.CurrentItem as IQuest);
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
                QuestCollectionView.MoveCurrentTo(QuestCollectionView.CurrentItem);
                QuestCollectionView.Refresh();
                startPost.Focus();
            }
        }

        #region Serialization
        /// <summary>
        /// Load any saved quest information when starting up the program.
        /// </summary>
        private QuestCollectionWrapper LoadQuestsContract()
        {
            string filepath = Path.Combine(System.Windows.Forms.Application.CommonAppDataPath, questFile);

            if (File.Exists(filepath))
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    DataContractSerializer ser = new DataContractSerializer(typeof(QuestCollectionWrapper));
                    try
                    {
                        QuestCollectionWrapper qcw = (QuestCollectionWrapper)ser.ReadObject(fs);
                        return qcw;
                    }
                    catch (SerializationException)
                    {
                        MessageBox.Show("Unable to load stored quests.  Data may be corrupt.\n\nNote: XML file will be overwritten on program close.",
                            "Unable to load stored quests", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Save any quest information when shutting the program down.
        /// </summary>
        private void SaveQuests()
        {
            string filepath = Path.Combine(System.Windows.Forms.Application.CommonAppDataPath, questFile);

            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                QuestCollectionWrapper qcw = new QuestCollectionWrapper(questCollection, QuestCollectionView.CurrentItem?.ToString());
                DataContractSerializer ser = new DataContractSerializer(typeof(QuestCollectionWrapper));
                ser.WriteObject(fs, qcw);
            }
        }

        #endregion
    }
}
