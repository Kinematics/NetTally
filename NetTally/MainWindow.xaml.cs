using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using NetTally.ViewModels;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        #region Fields and Properties
        readonly MainViewModel mainViewModel;

        bool _disposed = false;
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

                ErrorLog.Initialize(new WindowsErrorLog());

                InitializeComponent();

                this.Title = $"{ProductInfo.Name} - {ProductInfo.Version}";

                // Set up data contexts
                mainViewModel = new MainViewModel(NetTallyConfig.Load());
                DataContext = mainViewModel;
            }
            catch (Exception e)
            {
                string file = ErrorLog.Log(e);
                MessageBox.Show($"Error log saved to:\n{file ?? "(unable to write log file)"}", "Error in startup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                string selectedQuest = mainViewModel.SelectedQuest?.ThreadName ?? "";

                QuestCollectionWrapper wrapper = new QuestCollectionWrapper(mainViewModel.QuestList, selectedQuest);
                NetTallyConfig.Save(wrapper);
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
                mainViewModel.Dispose();
            }

            _disposed = true;
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
            TryConfirmEdit();

            if (mainViewModel.SelectedQuest == null)
                return;

            try
            {
                await mainViewModel.Tally();
            }
            catch (Exception ex)
            {
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
            }
        }

        /// <summary>
        /// Cancel the cancellation token, if present.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelTally_Click(object sender, RoutedEventArgs e)
        {
            mainViewModel.Cancel();
        }

        /// <summary>
        /// Clear the page cache so that subsequent tally requests load the pages from the network
        /// rather than from the cache.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearTallyCacheButton_Click(object sender, RoutedEventArgs e)
        {
            TryConfirmEdit();

            tallyButton.IsEnabled = false;

            try
            {
                mainViewModel.ClearCache();
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
            TryConfirmEdit();

            try
            {
                Clipboard.SetText(mainViewModel.Output);
            }
            catch (Exception ex1)
            {
                ErrorLog.Log("First clipboard failure", ex1);
                //MessageBox.Show(ex1.Message, "Clipboard error 1", MessageBoxButton.OK, MessageBoxImage.Error);
                try
                {
                    Clipboard.SetDataObject(mainViewModel.Output, false);
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
            TryConfirmEdit();

            if (mainViewModel.AddNewQuest())
                StartEdit(true);
        }

        /// <summary>
        /// Remove the current quest from the quest list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeQuestButton_Click(object sender, RoutedEventArgs e)
        {
            TryConfirmEdit();
            mainViewModel.RemoveQuest();
        }

        /// <summary>
        /// Open the window for handling merging votes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openManageVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            ManageVotesWindow manageWindow = new ManageVotesWindow()
            {
                Owner = Application.Current.MainWindow,
                DataContext = mainViewModel
            };

            manageWindow.ShowDialog();

            mainViewModel.Update();
        }

        /// <summary>
        /// Opens the global options window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void globalOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            GlobalOptionsWindow options = new GlobalOptionsWindow()
            {
                Owner = Application.Current.MainWindow,
                //DataContext = mainViewModel
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
            QuestOptionsWindow options = new QuestOptionsWindow(mainViewModel.SelectedQuest);
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

        #region Focus on start/end post boxes
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
        private void TryConfirmEdit()
        {
            if (editQuestName.Visibility == Visibility.Visible || editQuestThread.Visibility == Visibility.Visible)
            {
                ConfirmEdit();
                HideEditBoxes();
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

        #region Keyboard Shortcuts
        /// <summary>
        /// Global window key capture for using F2 to edit the quest name and URL.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                StartEdit();
            }
            else if (e.Key == Key.Escape)
            {
                TryCancelEdit();
            }
            else if (e.Key == Key.Enter)
            {
                TryConfirmEdit();
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
        /// When modifying the quest name, hitting enter will complete the entry,
        /// and hitting escape will cancel the edits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editQuestName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryConfirmEdit();
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
                TryConfirmEdit();
            }
        }
        #endregion
    }
}
