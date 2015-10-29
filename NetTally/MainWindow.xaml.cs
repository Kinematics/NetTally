using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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

        CheckForNewRelease checkForNewRelease = new CheckForNewRelease();

        // Using a DependencyProperty as the backing store for MyTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyTitleProperty =
            DependencyProperty.Register("MyTitle", typeof(string), typeof(MainWindow), new UIPropertyMetadata(null));

        public string MyTitle
        {
            get { return (string)GetValue(MyTitleProperty); }
            set { SetValue(MyTitleProperty, value); }
        }

        private IQuest CurrentlySelectedQuest() => QuestCollectionView.CurrentItem as IQuest;

        public List<int> ValidPostsPerPage { get; } = new List<int>() { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

        public List<string> DisplayModes { get; } = Enumerations.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = Enumerations.EnumDescriptionsList<PartitionMode>().ToList();

        #region Startup/shutdown events
        /// <summary>
        /// Function that's run when the program first starts.
        /// Set up the data context links with the local variables.
        /// </summary>
        public MainWindow()
        {
            // Set up an event handler for any otherwise unhandled exceptions in the code.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();

            // Set tally vars
            tally = new Tally();

            questCollection = new QuestCollection();

            QuestCollectionWrapper wrapper = new QuestCollectionWrapper(questCollection, null, DisplayMode.Normal);

            NetTallyConfig.Load(tally, wrapper);

            // Set up view for binding
            QuestCollectionView = CollectionViewSource.GetDefaultView(questCollection);
            // Sort the collection view
            var sortDesc = new SortDescription("DisplayName", ListSortDirection.Ascending);
            QuestCollectionView.SortDescriptions.Add(sortDesc);
            // Set the current item
            QuestCollectionView.MoveCurrentTo(questCollection[wrapper.CurrentQuest]);

            Properties.Settings settings = new Properties.Settings();
            tally.DisplayMode = wrapper.DisplayMode;

            // Set up data contexts
            DataContext = QuestCollectionView;

            resultsWindow.DataContext = tally;
            tallyButton.DataContext = tally;
            cancelTally.DataContext = tally;
            displayMode.DataContext = tally;
            newRelease.DataContext = checkForNewRelease;


            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));
            MyTitle = $"{product.Product} - {version.InformationalVersion}";
        }

        /// <summary>
        /// Unhandled exception handler.  If an unhandled exception crashes the program, save
        /// the stack trace to a log file.
        /// </summary>
        /// <param name="sender">The AppDomain.</param>
        /// <param name="e">The details of the unhandled exception.</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            
            // print out the exception stack trace to a log
            string output = 
                $"Message is: {ex.Message}\n\n" +
                $"Stack Trace is:\n{ex.StackTrace}\n";

            string tempFile = Path.GetTempFileName();

            File.WriteAllText(tempFile, output);

            // Let the user know where the temp file was written.
            MessageBox.Show($"Error written to:\n{tempFile}", "Unhandled exception log written", MessageBoxButton.OK, MessageBoxImage.Error);
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

            QuestCollectionWrapper qcw = new QuestCollectionWrapper(questCollection, selectedQuest, tally.DisplayMode);
            NetTallyConfig.Save(tally, qcw);

            Properties.Settings settings = new Properties.Settings();
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

            using (cts = new CancellationTokenSource())
            {
                try
                {
                    await tally.Run(QuestCollectionView.CurrentItem as IQuest, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // got a cancel request somewhere
                }
                catch (Exception ex)
                {
                    if (cts.IsCancellationRequested == false)
                    {
                        string exmsg = ex.Message;
                        var innerEx = ex.InnerException;
                        while (innerEx != null)
                        {
                            exmsg = exmsg + "\n" + innerEx.Message;
                            innerEx = innerEx.InnerException;
                        }
                        MessageBox.Show(exmsg, "Error");
                        cts.Cancel();
                    }
                }
            }
        }

        /// <summary>
        /// Cancel the cancellation token, if present.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Open the window for handling merging votes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openMergeVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            ManageVotesWindow mergeWindow = new ManageVotesWindow(tally)
            {
                Owner = Application.Current.MainWindow
            };

            mergeWindow.ShowDialog();

            tally.ConstructResults(CurrentlySelectedQuest());
        }

        /// <summary>
        /// Open a browser to view the wiki URL.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        private void partitionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

    }
}
