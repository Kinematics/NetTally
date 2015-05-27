using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;


namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Collections for holding quests
        public ICollectionView QuestCollectionView { get; }
        QuestCollection questCollection;

        string editingName = string.Empty;

        // Locals for managing the tally
        Tally tally;
        CancellationTokenSource cts;

        private IQuest CurrentlySelectedQuest()
        {
            return QuestCollectionView.CurrentItem as IQuest;
        }


        #region Startup/shutdown events
        /// <summary>
        /// Function that's run when the program first starts.
        /// Set up the data context links with the local variables.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Set tally vars
            tally = new Tally();

            questCollection = new QuestCollection();

            QuestCollectionWrapper wrapper = new QuestCollectionWrapper(questCollection, null);

            NetTallyConfig.Load(tally, wrapper);

            // Set up view for binding
            QuestCollectionView = CollectionViewSource.GetDefaultView(questCollection);
            // Sort the collection view
            var sortDesc = new SortDescription("DisplayName", ListSortDirection.Ascending);
            QuestCollectionView.SortDescriptions.Add(sortDesc);
            // Set the current item
            QuestCollectionView.MoveCurrentTo(questCollection[wrapper.CurrentQuest]);

            Properties.Settings settings = new Properties.Settings();
            tally.UseSpoilerForVoters = settings.UseSpoilerForVoters;

            // Set up data contexts
            DataContext = QuestCollectionView;
            resultsWindow.DataContext = tally;
            useSpoilerForVoters.DataContext = tally;
        }

        /// <summary>
        /// When the program closes, save the current list of quests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            string selectedQuest = "";

            if (CurrentlySelectedQuest() != null)
            {
                selectedQuest = CurrentlySelectedQuest().ThreadName;
            }

            QuestCollectionWrapper qcw = new QuestCollectionWrapper(questCollection, selectedQuest);
            NetTallyConfig.Save(tally, qcw);

            Properties.Settings settings = new Properties.Settings();
            settings.UseSpoilerForVoters = tally.UseSpoilerForVoters;
            settings.Save();
        }
        #endregion

        #region User action events
        /// <summary>
        /// Start running the tally on the currently selected quest and post range.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void tallyButton_Click(object sender, RoutedEventArgs e)
        {
            DoneEditing();

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
            DoneEditing();

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
            DoneEditing();
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
            DoneEditing();

            var newEntry = questCollection.AddNewQuest();
            if (newEntry == null)
            {
                newEntry = questCollection.FirstOrDefault(q => q.ThreadName == Quest.NewThreadEntry);
                if (newEntry == null)
                    return;
            }

            QuestCollectionView.MoveCurrentTo(newEntry);

            EditQuestThread();
        }

        /// <summary>
        /// Remove the current quest from the quest list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeQuestButton_Click(object sender, RoutedEventArgs e)
        {
            DoneEditing();
            questCollection.Remove(QuestCollectionView.CurrentItem as IQuest);
            QuestCollectionView.Refresh();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
        #endregion

        #region Behavior events
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
        /// Global window key capture for using F2 to edit the quest name and URL.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                EditActions();
            }
        }

        /// <summary>
        /// Button event handler for editing the quest name and URL.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editNameButton_Click(object sender, RoutedEventArgs e)
        {
            EditActions();
        }


        /// <summary>
        /// When modifying the quest name, hitting enter will complete the entry,
        /// and hitting escape will cancel the edits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editQuestName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoneEditingQuestName();
                startPost.Focus();
            }
            else if (e.Key == Key.Escape)
            {
                // Restore original name if we escape.
                IQuest quest = QuestCollectionView.CurrentItem as IQuest;
                if (quest != null)
                    quest.DisplayName = editingName;
                DoneEditingQuestName();
            }
        }

        /// <summary>
        /// When modifying the quest site, hitting enter will complete the entry,
        /// and hitting escape will cancel the edits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editQuestThread_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DoneEditingQuestSite();
            }
            else if (e.Key == Key.Escape)
            {
                // Restore original name if we escape.
                IQuest quest = QuestCollectionView.CurrentItem as IQuest;
                if (quest != null)
                    quest.Site = editingName;
                DoneEditingQuestSite();
            }
        }


        /// <summary>
        /// If the option to partition votes is changed, retally the results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void partitionedVotes_CheckedChanged(object sender, RoutedEventArgs e)
        {
            tally.UpdateTally(CurrentlySelectedQuest());
        }

        /// <summary>
        /// If the partition type is changed, retally the results.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void partitionByLine_CheckedChanged(object sender, RoutedEventArgs e)
        {
            tally.UpdateTally(CurrentlySelectedQuest());
        }

        #endregion


        #region Utility support methods

        /// <summary>
        /// The sequence of edit actions performed when either hitting F2 or the Edit Name button.
        /// </summary>
        private void EditActions()
        {
            if ((editQuestName.Visibility == Visibility.Hidden) && (editQuestThread.Visibility == Visibility.Hidden))
            {
                EditQuestName();
            }
            else if (editQuestThread.Visibility == Visibility.Hidden)
            {
                EditQuestThread();
            }
            else
            {
                DoneEditingQuestSite();
            }
        }


        private void EditQuestName()
        {
            DoneEditingQuestSite();
            editingName = ((IQuest)QuestCollectionView.CurrentItem).DisplayName;
            editDescriptor.Text = "Name";
            editNameButton.Content = "Edit URL";
            editQuestName.Visibility = Visibility.Visible;
            editDescriptorCanvas.Visibility = Visibility.Visible;
            editQuestName.Focus();
        }

        private void EditQuestThread()
        {
            DoneEditingQuestName();
            editingName = ((IQuest)QuestCollectionView.CurrentItem).ThreadName;
            editDescriptor.Text = "Thread";
            editNameButton.Content = "Finish Edit";
            editQuestThread.Visibility = Visibility.Visible;
            editDescriptorCanvas.Visibility = Visibility.Visible;
            editQuestThread.Focus();
        }

        private void DoneEditing()
        {
            DoneEditingQuestName();
            DoneEditingQuestSite();
        }

        private void DoneEditingQuestName()
        {
            editQuestName.Visibility = Visibility.Hidden;
            editDescriptorCanvas.Visibility = Visibility.Hidden;
            QuestCollectionView.Refresh();
        }

        private void DoneEditingQuestSite()
        {
            editNameButton.Content = "Edit Name";
            editQuestThread.Visibility = Visibility.Hidden;
            editDescriptorCanvas.Visibility = Visibility.Hidden;
            QuestCollectionView.Refresh();
        }

        #endregion


        private void openMergeVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            MergeVotesWindow mergeWindow = new MergeVotesWindow(tally)
            {
                ShowInTaskbar = false,               // don't show the dialog on the taskbar
                Topmost = true,                      // ensure we're Always On Top
                ResizeMode = ResizeMode.NoResize,    // remove excess caption bar buttons
                Owner = Application.Current.MainWindow,
            };

            mergeWindow.ShowDialog();

            tally.ConstructResults(CurrentlySelectedQuest());
        }
    }
}
