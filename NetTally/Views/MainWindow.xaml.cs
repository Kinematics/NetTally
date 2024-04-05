using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Extensions.Logging;
using NetTally.Navigation;
using NetTally.SystemInfo;
using NetTally.ViewModels;

namespace NetTally.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel mainViewModel;
        private readonly WPFNavigationService navigationService;
        private readonly ILogger<MainWindow> logger;

        public MainWindow(
            MainViewModel mainViewModel,
            WPFNavigationService navigationService,
            ILogger<MainWindow> logger)
        {
            this.mainViewModel = mainViewModel;
            this.navigationService = navigationService;
            this.logger = logger;

            InitializeComponent();
            DataContext = this.mainViewModel;

            Title = $"{ProductInfo.Name} - {ProductInfo.Version}";

            this.mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
        }


        #region Event Handlers
        private async void MainViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            logger.LogInformation("Received notification of property change from MainViewModel: {PropertyName}.", e.PropertyName);

            // If a new quest was added, load the QuestOptions dialog to
            // allow setting the URL and display name.
            if (e.PropertyName == nameof(mainViewModel.AddQuestCommand))
            {
                // If we have a URL in the clipboard, make use of that as
                // the default new URL for the quest.
                string clipboard = Clipboard.GetText();

                string uri = string.Empty;

                if (Uri.IsWellFormedUriString(clipboard, UriKind.Absolute))
                {
                    uri = clipboard;
                }

                var result = await navigationService.ShowDialogAsync<QuestOptions>(this, uri);

                // If the QuestOptions dialog was canceled, remove the quest we just added.
                // Otherwise, update the position of the quest.
                if (!result.HasValue || result.Value == false)
                {
                    mainViewModel.RemoveQuestCommand.Execute(null);
                }
                else
                {
                    mainViewModel.RepositionQuest();
                }
            }
        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            CopyOutputTextToClipboard();
        }

        private void CopyOutputTextToClipboard()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Clipboard.SetText(mainViewModel.Output);
                }
            }
            catch (Exception e1)
            {
                try
                {
                    // Try again
                    Clipboard.SetDataObject(mainViewModel.Output, false);
                }
                catch (Exception)
                {
                    logger.LogWarning(e1, "Unable to copy output to the clipboard.");
                }
            }
        }

        private async void OpenManageVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await navigationService.ShowDialogAsync<ManageVotes>(this);

                mainViewModel.UpdateOutput();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error managing votes");
            }
        }

        private async void GlobalOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await navigationService.ShowDialogAsync<GlobalOptions>(this);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error handling global options");
            }
        }

        private async void QuestOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await navigationService.ShowDialogAsync<QuestOptions>(this);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error handling quest options");
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C &&
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                CopyOutputTextToClipboard();
                e.Handled = true;
            }
        }

        private void TextEntry_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TextEntry_PreviewMouseDown(object sender, MouseButtonEventArgs e)
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

        /// <summary>
        /// Open a browser to view the wiki URL.
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {e.Uri.AbsoluteUri}") { CreateNoWindow = true });
                e.Handled = true;
            }
        }
        #endregion Event Handlers
    }
}
