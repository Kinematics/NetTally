using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using NetTally.ViewModels;
using NetTally.Collections;
using NetTally.CustomEventArgs;
using NetTally.Platform;
using NetTally.Utility;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        #region Fields and Properties
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

                PlatformSetup();

                InitializeComponent();

                Title = $"{ProductInfo.Name} - {ProductInfo.Version}";

                // Set up data contexts
                QuestCollectionWrapper config;
                try
                {
                    config = NetTallyConfig.Load();
                }
                catch (ConfigurationErrorsException e)
                {
                    string file = ErrorLog.Log(e);

                    string message = $"{e.Message}\n\nError log saved to:\n{file ?? "(unable to write log file)"}";
                    MessageBox.Show(message, "Error in configuration", MessageBoxButton.OK, MessageBoxImage.Error);
                    config = null;
                }

                SetupModel(config);
            }
            catch (Exception e)
            {
                string file = ErrorLog.Log(e);
                MessageBox.Show($"Error log saved to:\n{file ?? "(unable to write log file)"}", "Error in startup", MessageBoxButton.OK, MessageBoxImage.Error);

                try
                {
                    // If mainViewModel failed to be set via config, just create a null-config version.
                    SetupModel(null);
                }
                catch (Exception e2)
                {
                    ErrorLog.Log(e2);
                    this.Close();
                }
            }
        }

        private static void PlatformSetup()
        {
            ErrorLog.LogUsing(new WindowsErrorLog());
            System.Net.ServicePointManager.DefaultConnectionLimit = 4;
            Agnostic.HashStringsUsing(UnicodeHashFunction.HashFunction);
        }

        private void SetupModel(QuestCollectionWrapper config)
        {
            try
            {
                ViewModelService.Instance.Configure(config).Build();

                DataContext = ViewModelService.MainViewModel;
                ViewModelService.MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
                ViewModelService.MainViewModel.ExceptionRaised += MainViewModel_ExceptionRaised;

                ViewModelService.MainViewModel.CheckForNewRelease();
            }
            catch (InvalidOperationException e)
            {
                ErrorLog.Log(e);
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
                string selectedQuest = ViewModelService.MainViewModel.SelectedQuest?.ThreadName ?? "";

                QuestCollectionWrapper wrapper = new QuestCollectionWrapper(ViewModelService.MainViewModel.QuestList, selectedQuest);
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
                ViewModelService.MainViewModel?.Dispose();
            }

            _disposed = true;
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
                Clipboard.SetText(ViewModelService.MainViewModel.Output);
            }
            catch (Exception ex1)
            {
                ErrorLog.Log("First clipboard failure", ex1);
                try
                {
                    Clipboard.SetDataObject(ViewModelService.MainViewModel.Output, false);
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
            ManageVotesWindow manageWindow = new ManageVotesWindow(ViewModelService.MainViewModel)
            {
                Owner = Application.Current.MainWindow
            };

            manageWindow.ShowDialog();

            ViewModelService.MainViewModel.UpdateOutput();
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
                DataContext = ViewModelService.MainViewModel
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
                DataContext = ViewModelService.MainViewModel
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
            if (ViewModelService.MainViewModel.SelectedQuest == null)
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

            if (be != null && ViewModelService.MainViewModel.SelectedQuest != null)
                be.UpdateTarget();
        }

        /// <summary>
        /// If the edit is confirmed, push the current value to the quest.
        /// </summary>
        private void ConfirmEdit()
        {
            BindingExpression be = GetCurrentEditBinding();

            if (be != null && ViewModelService.MainViewModel.SelectedQuest != null)
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
