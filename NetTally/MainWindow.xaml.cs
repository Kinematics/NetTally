using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using NetTally.Collections;
using NetTally.CustomEventArgs;
using NetTally.Platform;
using NetTally.Utility;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        #region Fields and Properties
        bool _disposed = false;
        MainViewModel mainViewModel;
        private bool updateFlag;
        readonly SynchronizationContext _syncContext;
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
                // Set up an event handler for any otherwise unhandled exceptions in the code.
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
                _syncContext = SynchronizationContext.Current;

                InitializeComponent();

                Title = $"{ProductInfo.Name} - {ProductInfo.Version}";

                QuestCollection quests = null;
                string currentQuest = null;

                // Load configuration data
                try
                {
                    NetTallyConfig.Load(out quests, out currentQuest, AdvancedOptions.Instance);
                }
                catch (ConfigurationErrorsException e)
                {
                    MessageBox.Show(e.Message, "Error in configuration.  Current configuration ignored.", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                PlatformSetup(quests, currentQuest);
            }
            catch (Exception e)
            {
                ErrorLog.Log(e);
                MessageBox.Show(e.Message, "Unable to start up. Closing.", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Set up the program with various platform-specific configurations.
        /// </summary>
        /// <param name="quests">The program's config data.</param>
        private void PlatformSetup(QuestCollection quests, string currentQuest)
        {
            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 4;

                mainViewModel = ViewModelService.Instance
                    .Configure(quests, currentQuest)
                    .LogErrorsUsing(new WindowsErrorLog())
                    .HashAgnosticStringsUsing(UnicodeHashFunction.HashFunction)
                    .Build();

                DataContext = mainViewModel;
                mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
                mainViewModel.ExceptionRaised += MainViewModel_ExceptionRaised;

                ViewModelService.MainViewModel.CheckForNewRelease();
            }
            catch (InvalidOperationException e)
            {
                ErrorLog.Log(e);
            }
        }

        /// <summary>
        /// Handles the FirstChanceException event of the Current Domain.
        /// Logs all first chance exceptions when debug mode is on, for debug builds.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs"/> instance containing the event data.</param>
        private void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
#if DEBUG
            if (AdvancedOptions.Instance.DebugMode)
            {
                try
                {
                    string msg = $"{e.Exception.GetBaseException().GetType().Name} exception event raised: {e.Exception.Message}\n\n{e.Exception.StackTrace}";
                    ErrorLog.Log(msg);
                }
                catch (Exception)
                {
                }
            }
#endif
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
            SaveConfig();
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                if (mainViewModel == null)
                    return;

                string selectedQuest = mainViewModel.SelectedQuest?.ThreadName ?? "";

                NetTallyConfig.Save(mainViewModel.QuestList, selectedQuest, AdvancedOptions.Instance);
            }
            catch (Exception ex)
            {
                string file = ErrorLog.Log(ex);
                MessageBox.Show($"Error log saved to:\n{file ?? "(unable to write log file)"}", "Error in shutdown", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Disposal
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
                mainViewModel?.Dispose();
            }

            _disposed = true;
        }
        #endregion

        #region Synchronization between instances of Nettally        
        /// <summary>
        /// When the SourceInitialized event is raised, add a hook so that we can
        /// watch the WndProc event queue.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        /// <summary>
        /// When the event queue messages come in on the OS, watch for a special message indicating
        /// that some instance of NetTally has modified its quest list.  If so, reload the quests
        /// from the config file and update.
        /// </summary>
        /// <param name="hwnd">The HWND.</param>
        /// <param name="msg">The MSG.</param>
        /// <param name="wParam">The w parameter.</param>
        /// <param name="lParam">The l parameter.</param>
        /// <param name="handled">if set to <c>true</c> [handled].</param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // WM_NETTALLYUPDATE notifies all other NetTally instances that an instance has changed
            // its quests, and has saved those changes to the config file.
            if (msg == NativeMethods.WM_NETTALLYUPDATE)
            {
                _syncContext.Post(o => Reload(), null);
                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Reloads the configuration file, and updates our local list of quests with any modifications.
        /// </summary>
        /// <returns></returns>
        private async Task Reload()
        {
            if (!updateFlag)
            {
                if (mainViewModel.TallyIsRunning)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(t => Reload());
                    return;
                }

                NetTallyConfig.Load(out QuestCollection quests, out string currentQuest, null);

                var removedQuests = mainViewModel.QuestList.Where(q => !quests.Any(qq => qq.ThreadName == q.ThreadName)).ToList();
                var addedQuests = quests.Where(q => !mainViewModel.QuestList.Any(qq => qq.ThreadName == q.ThreadName)).ToList();
                var renamedQuests = quests.Where(q => mainViewModel.QuestList.Where(qq => qq.ThreadName == q.ThreadName).Any(qqr => qqr.DisplayName != q.DisplayName)).ToList();

                foreach (var q in removedQuests)
                    mainViewModel.RemoveQuestQuiet(q);
                foreach (var q in addedQuests)
                    mainViewModel.AddQuestQuiet(q);
                foreach (var q in renamedQuests)
                    mainViewModel.RenameQuestQuiet(q.ThreadName, q.DisplayName);
            }

            updateFlag = false;
        }

        /// <summary>
        /// Broadcasts the update notification for the quest config modification.
        /// </summary>
        private void BroadcastUpdateNotification()
        {
            updateFlag = true;

            // send our Win32 message to make the currently running instance
            // jump on top of all the other windows
            NativeMethods.PostMessage(
                (IntPtr)NativeMethods.HWND_BROADCAST,
                NativeMethods.WM_NETTALLYUPDATE,
                IntPtr.Zero,
                IntPtr.Zero);
        }
        #endregion

        #region Watched Events        
        /// <summary>
        /// Handles the PropertyChanged event of the MainViewModel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AddQuest")
            {
                StartEdit(true);
            }
            else if (e.PropertyName == "RenameQuest" || e.PropertyName == "RemoveQuest")
            {
                // Any renames or removals should trigger an update broadcast.
                // Additions are not notified for, since that would only indicate
                // the addition of a new fake quest.
                SaveConfig();
                BroadcastUpdateNotification();
            }
        }

        /// <summary>
        /// Handles the ExceptionRaised event of the MainViewModel control.
        /// This is called anytime there's an exception generated that needs
        /// to propogate up to the UI.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ExceptionEventArgs"/> instance containing the event data.</param>
        private void MainViewModel_ExceptionRaised(object sender, ExceptionEventArgs e)
        {
            Exception ex = e.Exception;

            string exmsg = ex.Message;
            var innerEx = ex.InnerException;
            while (innerEx != null)
            {
                exmsg = exmsg + "\n" + innerEx.Message;
                innerEx = innerEx.InnerException;
            }
            MessageBox.Show(exmsg, "Error");
            if (!(ex.Data.Contains("Application")))
                ErrorLog.Log(ex);

            e.Handled = true;
        }
        #endregion

        #region UI Events
        #region User action events
        /// <summary>
        /// Event to cause the program to copy the current contents of the the tally
        /// results (ie: what's shown in the main text window) to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(mainViewModel.Output);
            }
            catch (Exception ex1)
            {
                ErrorLog.Log("First clipboard failure", ex1);
                try
                {
                    Clipboard.SetDataObject(mainViewModel.Output, false);
                }
                catch (Exception ex2)
                {
                    ErrorLog.Log("Second clipboard failure", ex2);
                }
            }
        }

        /// <summary>
        /// Button event handler for editing the quest name and URL.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editNameButton_Click(object sender, RoutedEventArgs e)
        {
            StartEdit();
        }

        /// <summary>
        /// Open the window for handling merging votes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openManageVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            ManageVotesWindow manageWindow = new ManageVotesWindow(mainViewModel)
            {
                Owner = Application.Current.MainWindow
            };

            manageWindow.ShowDialog();

            mainViewModel.UpdateOutput();
        }

        /// <summary>
        /// Opens the global options window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void globalOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            GlobalOptionsWindow options = new GlobalOptionsWindow
            {
                Owner = Application.Current.MainWindow,
                DataContext = mainViewModel
            };

            options.ShowDialog();
        }

        /// <summary>
        /// Opens the global options window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void questOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            QuestOptionsWindow options = new QuestOptionsWindow
            {
                Owner = Application.Current.MainWindow,
                DataContext = mainViewModel
            };

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

        #region Keyboard Shortcuts
        /// <summary>
        /// Global window key capture for managing the edit of the quest name/URL,
        /// if the focus isn't in the edit box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    StartEdit();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    TryCancelEdit();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    TryConfirmEdit(true);
                    e.Handled = true;
                    break;
            }
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
                TryConfirmEdit(true);
                e.Handled = true;
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
                TryConfirmEdit(true);
                e.Handled = true;
            }
        }
        #endregion

        #region UI Focus
        /// <summary>
        /// If the text box gets focus, select the entire contents so that it's easy to replace.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void textEntry_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        /// <summary>
        /// If the text box loses focus, make sure to try to confirm any edits made before
        /// other controls activate, unless we're switching focus to a different edit box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void editQuest_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus == editQuestThread || e.NewFocus == editNameButton)
                return;

            TryConfirmEdit();
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
            if (sender is TextBox tb)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    tb.Focus();
                    e.Handled = true;
                }
            }
        }

        #endregion
        #endregion

        #region Functions for editing quest names and URLs.
        /// <summary>
        /// Starts editing quest name/thread info.
        /// </summary>
        /// <param name="startWithThread">If set to <c>true</c> [start with thread].</param>
        private void StartEdit(bool startWithThread = false)
        {
            if (mainViewModel.SelectedQuest == null)
            {
                HideEditBoxes();
                return;
            }

            if (editQuestThread.Visibility == Visibility.Visible)
            {
                ConfirmEdit();
                HideEditBoxes();
            }
            else if (editQuestName.Visibility == Visibility.Visible || startWithThread)
            {
                ConfirmEdit();
                editQuestName.Visibility = Visibility.Hidden;
                editQuestThread.Visibility = Visibility.Visible;
                editDescriptor.Text = "Thread";
                editDescriptorCanvas.Visibility = Visibility.Visible;
                editNameButton.Content = "Finish Edit";
                editQuestThread.Focus();
            }
            else
            {
                editQuestName.Visibility = Visibility.Visible;
                editQuestThread.Visibility = Visibility.Hidden;
                editDescriptor.Text = "Name";
                editDescriptorCanvas.Visibility = Visibility.Visible;
                editNameButton.Content = "Edit URL";
                editQuestName.Focus();
            }
        }

        /// <summary>
        /// Hides the edit boxes.  Run after editing is complete.
        /// </summary>
        private void HideEditBoxes()
        {
            editQuestName.Visibility = Visibility.Hidden;
            editQuestThread.Visibility = Visibility.Hidden;
            editDescriptorCanvas.Visibility = Visibility.Hidden;
            editNameButton.Content = "Edit Name";
        }

        /// <summary>
        /// Tries to cancel any in-progress edit.
        /// </summary>
        private void TryCancelEdit()
        {
            if (editQuestName.Visibility == Visibility.Visible || editQuestThread.Visibility == Visibility.Visible)
            {
                CancelEdit();
                HideEditBoxes();
            }
        }

        /// <summary>
        /// Tries to confirm any in-progress edit.
        /// </summary>
        private void TryConfirmEdit(bool focusOnStart = false)
        {
            if (editQuestName.Visibility == Visibility.Visible || editQuestThread.Visibility == Visibility.Visible)
            {
                ConfirmEdit();
                HideEditBoxes();
                if (focusOnStart)
                    startPost.Focus();
            }
        }

        /// <summary>
        /// If the edit is cancelled, revert to the original value.
        /// </summary>
        private void CancelEdit()
        {
            BindingExpression be = GetCurrentEditBinding();

            if (be != null && mainViewModel.SelectedQuest != null)
                be.UpdateTarget();
        }

        /// <summary>
        /// If the edit is confirmed, push the current value to the quest.
        /// </summary>
        private void ConfirmEdit()
        {
            BindingExpression be = GetCurrentEditBinding();

            if (be != null && mainViewModel.SelectedQuest != null)
                be.UpdateSource();
        }

        /// <summary>
        /// Helper function to get the edit binding of the currently visible edit window.
        /// </summary>
        /// <returns>Returns the binding for the current visible edit window, if any.</returns>
        private BindingExpression GetCurrentEditBinding()
        {
            BindingExpression be = null;

            if (editQuestName.Visibility == Visibility.Visible)
                be = editQuestName.GetBindingExpression(TextBox.TextProperty);
            else if (editQuestThread.Visibility == Visibility.Visible)
                be = editQuestThread.GetBindingExpression(TextBox.TextProperty);

            return be;
        }
        #endregion
    }
}
