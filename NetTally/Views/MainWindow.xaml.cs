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
        private readonly IoCNavigationService navigation;
        private readonly ILogger<MainWindow> logger;

        public MainWindow(
            MainViewModel mainViewModel,
            IoCNavigationService navigation,
            ILogger<MainWindow> logger)
        {
            this.mainViewModel = mainViewModel;
            this.navigation = navigation;
            this.logger = logger;

            InitializeComponent();
            DataContext = this.mainViewModel;

            Title = $"{ProductInfo.Name} - {ProductInfo.Version}";

            this.mainViewModel.Quests.CollectionChanged += Quests_CollectionChanged;
        }


        #region Event Handlers
        private async void QuestOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowQuestOptions();
        }

        private async Task ShowQuestOptions()
        {
            await navigation.ShowDialogAsync<QuestOptions>(this);
        }

        private async void GlobalOptionsButton_Click(object sender, RoutedEventArgs e)
        {
            await navigation.ShowDialogAsync<GlobalOptions>(this);
        }

        private async void OpenManageVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            await navigation.ShowDialogAsync<ManageVotes>(this);

            mainViewModel.UpdateOutput();
        }

        private async void Quests_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                await ShowQuestOptions();
            }
        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            CopyOutputTextToClipboard();
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
    }
}
