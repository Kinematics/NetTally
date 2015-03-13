using System;
using System.ComponentModel;
using System.Linq;
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

        // Collections for holding quests
        public ICollectionView QuestCollectionView { get; }
        QuestCollection questCollection;

        // Locals for managing the tally
        IForumAdapter forumAdapter;
        Tally tally;
        CancellationTokenSource cts;

        #region Startup/shutdown
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

            questCollection = new QuestCollection();

            QuestCollectionWrapper wrapper = new QuestCollectionWrapper(questCollection, null);

            NetTallyConfig.Load(tally, wrapper);

            QuestCollectionView = CollectionViewSource.GetDefaultView(questCollection);
            // Sort the collection view
            var sortDesc = new SortDescription("Name", ListSortDirection.Ascending);
            QuestCollectionView.SortDescriptions.Add(sortDesc);
            // Set the current item
            QuestCollectionView.MoveCurrentTo(questCollection[wrapper.CurrentQuest]);


            // Set up data contexts
            DataContext = QuestCollectionView;

            resultsWindow.DataContext = tally;

        }

        /// <summary>
        /// When the program closes, save the current list of quests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            QuestCollectionWrapper qcw = new QuestCollectionWrapper(questCollection, QuestCollectionView.CurrentItem?.ToString());
            NetTallyConfig.Save(tally, qcw);
        }
        #endregion

        #region User action event handling
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
        /// Remove the current quest from the quest list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeQuestButton_Click(object sender, RoutedEventArgs e)
        {
            questCollection.Remove(QuestCollectionView.CurrentItem as IQuest);
            CleanupEditQuestName();
        }
        #endregion

        #region Behavior event handling
        /// <summary>
        /// Any user interaction that ends the use of the text box for editing the quest
        /// name needs to ensure some cleanup occurs.
        /// </summary>
        private void CleanupEditQuestName()
        {
            if (editQuestName.Visibility == Visibility.Visible)
            {
                editQuestName.Visibility = Visibility.Hidden;
                QuestCollectionView.Refresh();
                startPost.Focus();
            }
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
        #endregion
    }
}
