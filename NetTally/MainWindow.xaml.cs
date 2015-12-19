using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
    public partial class MainWindow : Window, IDisposable
    {
        #region Fields and Properties
        bool _disposed = false;

        // Collections for holding quests
        public ICollectionView QuestCollectionView { get; }
        QuestCollection questCollection;

        string editingName = string.Empty;

        // Locals for managing the tally
        Tally tally;
        CancellationTokenSource cts;

        CheckForNewRelease checkForNewRelease;

        public List<int> ValidPostsPerPage { get; } = new List<int> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

        public List<string> DisplayModes { get; } = Enumerations.EnumDescriptionsList<DisplayMode>().ToList();

        public List<string> PartitionModes { get; } = Enumerations.EnumDescriptionsList<PartitionMode>().ToList();

        private IQuest CurrentlySelectedQuest => QuestCollectionView?.CurrentItem as IQuest;
        #endregion

        #region Startup/shutdown events
        /// <summary>
        /// Function that's run when the program first starts.
        /// Set up the data context links with the local variables.
        /// </summary>
        public MainWindow()
        {
            try
            {
                // Create a region profiler on startup to get it JIT'd before any actual profiling.
                using (new RegionProfiler(null)) { }

                // Set up an event handler for any otherwise unhandled exceptions in the code.
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                DebugMode.Update();

                if (DebugMode.Active)
                    ErrorLog.Log("Preparing to initialize components.");

                InitializeComponent();

                if (DebugMode.Active)
                    ErrorLog.Log("Completed initializing components.");

                this.Title = GetWindowTitle();

                // Set tally vars
                tally = new Tally();

                questCollection = new QuestCollection();

                QuestCollectionWrapper wrapper = new QuestCollectionWrapper(questCollection, null);

                if (DebugMode.Active)
                    ErrorLog.Log("Preparing to load config.");

                NetTallyConfig.Load(tally, wrapper);

                if (DebugMode.Active)
                    ErrorLog.Log("Completed loading config.");

                // Set up view for binding
                QuestCollectionView = CollectionViewSource.GetDefaultView(questCollection);
                // Sort the collection view
                var sortDesc = new SortDescription("DisplayName", ListSortDirection.Ascending);
                QuestCollectionView.SortDescriptions.Add(sortDesc);
                // Set the current item
                QuestCollectionView.MoveCurrentTo(questCollection[wrapper.CurrentQuest]);

                AdvancedOptions.Instance.DisplayMode = wrapper.DisplayMode;
                AdvancedOptions.Instance.AllowRankedVotes = wrapper.AllowRankedVotes;
                AdvancedOptions.Instance.IgnoreSymbols = wrapper.IgnoreSymbols;
                AdvancedOptions.Instance.TrimExtendedText = wrapper.TrimExtendedText;


                // Set up data contexts
                DataContext = QuestCollectionView;

                resultsWindow.DataContext = tally;
                tallyButton.DataContext = tally;
                cancelTally.DataContext = tally;
                displayMode.DataContext = AdvancedOptions.Instance;

                checkForNewRelease = new CheckForNewRelease();
                newRelease.DataContext = checkForNewRelease;
                checkForNewRelease.Update();

                if (DebugMode.Active)
                    ErrorLog.Log("Completed main window construction.");

            }
            catch (Exception e)
            {
                string file = ErrorLog.Log(e);
                MessageBox.Show($"Error log saved to:\n{file ?? "(unable to write log file)"}", "Error in startup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        ~MainWindow()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true); //I am calling you from Dispose, it's safe
            GC.SuppressFinalize(this); //Hey, GC: don't bother calling finalize later
        }

        protected virtual void Dispose(bool itIsSafeToAlsoFreeManagedObjects)
        {
            if (_disposed)
                return;

            if (itIsSafeToAlsoFreeManagedObjects)
            {
                tally.Dispose();
            }

            _disposed = true;
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

            string file = ErrorLog.Log(ex);

            MessageBox.Show($"Error log saved to:\n{file ?? "(unable to write log file)"}", "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// When the program closes, save the current list of quests.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                string selectedQuest = CurrentlySelectedQuest?.ThreadName ?? "";

                QuestCollectionWrapper qcw = new QuestCollectionWrapper(questCollection, selectedQuest);
                NetTallyConfig.Save(tally, qcw);

                Properties.Settings settings = new Properties.Settings();
                settings.Save();
            }
            catch(Exception ex)
            {
                string file = ErrorLog.Log(ex);
                MessageBox.Show($"Error log saved to:\n{file ?? "(unable to write log file)"}", "Error in shutdown", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Get the title to be used for the main window, showing the program name and version number.
        /// </summary>
        /// <returns>Returns the program name and version number as a string.</returns>
        private string GetWindowTitle()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var product = (AssemblyProductAttribute)assembly.GetCustomAttribute(typeof(AssemblyProductAttribute));
            var version = (AssemblyInformationalVersionAttribute)assembly.GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

            return $"{product.Product} - {version.InformationalVersion}";
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

            if (CurrentlySelectedQuest == null)
                return;

            using (cts = new CancellationTokenSource())
            {
                try
                {
                    await tally.Run(CurrentlySelectedQuest, cts.Token);
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
                        if (!(ex is ApplicationException))
                            ErrorLog.Log(ex);
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
        /// When changing partition mode, update based on the currently collected tally info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void partitionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tally.UpdateTally(CurrentlySelectedQuest);
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

            try
            {
                Clipboard.SetText(tally.TallyResults);
            }
            catch (Exception ex1)
            {
                ErrorLog.Log("First clipboard failure", ex1);
                //MessageBox.Show(ex1.Message, "Clipboard error 1", MessageBoxButton.OK, MessageBoxImage.Error);
                try
                {
                    Clipboard.SetDataObject(tally.TallyResults, false);
                }
                catch (Exception ex2)
                {
                    ErrorLog.Log("Second clipboard failure", ex2);
                    //MessageBox.Show(ex2.Message, "Clipboard error 2", MessageBoxButton.OK, MessageBoxImage.Error);
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
            questCollection.Remove(CurrentlySelectedQuest);
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

            tally.UpdateResults();
        }

        /// <summary>
        /// Opens the advanced options window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void optionsButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow options = new OptionsWindow();
            options.Owner = Application.Current.MainWindow;

            options.ShowDialog();
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

        #region Events to support editing thread names
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
                IQuest quest = CurrentlySelectedQuest;
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
                IQuest quest = CurrentlySelectedQuest;
                if (quest != null)
                    quest.ThreadName = editingName;
                DoneEditingQuestSite();
            }
        }
        #endregion

        #region Edit and edit cleanup methods

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

        /// <summary>
        /// Adjust window so that we can edit the quest name.
        /// </summary>
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

        /// <summary>
        /// Adjust window so that we can edit the quest thread.
        /// </summary>
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

        /// <summary>
        /// Call when we want to cleanup all editing in progress.
        /// </summary>
        private void DoneEditing()
        {
            RunCleanup(CleanupQuestName, CleanupQuestSite);
        }

        /// <summary>
        /// Call when we want to cleanup after editing the quest name.
        /// </summary>
        private void DoneEditingQuestName()
        {
            RunCleanup(CleanupQuestName);
        }

        /// <summary>
        /// Call when we want to cleanup after editing the quest thread.
        /// </summary>
        private void DoneEditingQuestSite()
        {
            RunCleanup(CleanupQuestSite);
        }

        /// <summary>
        /// Cleanup code specific to cleaning up after editing the quest name.
        /// </summary>
        private void CleanupQuestName()
        {
            editQuestName.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Cleanup code specific to cleaning up after editing the quest thread.
        /// </summary>
        private void CleanupQuestSite()
        {
            editNameButton.Content = "Edit Name";
            editQuestThread.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Overall cleanup handling, which runs any provided actions before refreshing the view.
        /// </summary>
        private void RunCleanup(params Action[] actions)
        {
            foreach (Action action in actions)
            {
                action();
            }

            editDescriptorCanvas.Visibility = Visibility.Hidden;
            QuestCollectionView.Refresh();
        }
        #endregion

    }
}
