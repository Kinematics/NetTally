using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using NetTally.Avalonia.Config.Xml;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.ExceptionServices;
using NetTally.ViewModels;
using NetTally.Avalonia.Navigation;

namespace NetTally.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        #region Fields and Properties
        private MainViewModel MainViewModel { get; }
        private AvaloniaNavigationService NavigationService { get; }
        private ILogger<MainWindow> Logger { get; }
        #endregion

        #region Startup/shutdown events

        /// <summary>
        /// Function that's run when the program first starts.
        /// Set up the data context links with the local variables.
        /// </summary>
        public MainWindow(
            MainViewModel model,
            AvaloniaNavigationService navigationService, 
            ILogger<MainWindow> logger)
        {
            // Initialize the readonly properties.
            MainViewModel = model;
            NavigationService = navigationService;
            Logger = logger;

            try
            {
                // Initialize the window.
                AvaloniaXamlLoader.Load(this);

                // Set the title.
                Title = $"{SystemInfo.ProductInfo.Name} - {SystemInfo.ProductInfo.Version}";

                // Load configuration data
                Collections.QuestCollection? quests = null;
                string? currentQuest = null;

                try
                {
                    LegacyNetTallyConfig.Load(out quests, out currentQuest, Options.AdvancedOptions.Instance);
                }
                catch (ConfigurationErrorsException e)
                {
                    Logger.LogError(e, "Failure during loading legacy configuration.");
                    WarningDialog.Show("Error in configuration. Current configuration ignored.", "Error in configuration");
                }

                // Complete the platform setup.
                PlatformSetup(quests, currentQuest);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failure during program startup.");
                WarningDialog.Show("Unable to start the program.", "Failure on startup");
                Close();
            }
        }

        /// <summary>
        /// Set up the program with various platform-specific configurations.
        /// </summary>
        /// <param name="quests">The program's config data.</param>
        private void PlatformSetup(Collections.QuestCollection? quests, string? currentQuest)
        {
            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 4;
                System.Net.ServicePointManager.Expect100Continue = true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                DataContext = MainViewModel;
                MainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
                //MainViewModel.ExceptionRaised += MainViewModel_ExceptionRaised;
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "Invalid operation during platform setup.");
            }
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            await MainViewModel.CheckForNewRelease();
            base.OnLoaded(e);
        }
        #endregion

        #region Watched Events        
        /// <summary>
        /// Handles the PropertyChanged event of the MainViewModel control.
        /// </summary>
        /// <remarks>
        /// This should probably move to a seperate config class?
        /// </remarks>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private async void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Logger.LogInformation("Received notification of property change from MainViewModel: {PropertyName}.", e.PropertyName);

            if (e.PropertyName == nameof(MainViewModel.AddQuestCommand))
            {
                string? clipboard = null;

                if (Clipboard is not null)
                    clipboard = await Clipboard.GetTextAsync();

                clipboard ??= string.Empty;

                var result = await NavigationService.ShowDialogAsync<QuestOptions>(this, clipboard);
            }
        }

        /// <summary>
        /// Handles the ExceptionRaised event of the MainViewModel control.
        /// This is called anytime there's an exception generated that needs
        /// to propogate up to the UI.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ExceptionEventArgs"/> instance containing the event data.</param>
        private void MainViewModel_ExceptionRaised(object? sender, CustomEventArgs.ExceptionEventArgs e)
        {
            Exception ex = e.Exception;

            string exmsg = ex.Message;
            var innerEx = ex.InnerException;

            while (innerEx != null)
            {
                exmsg = exmsg + "\n" + innerEx.Message;
                innerEx = innerEx.InnerException;
            }

            WarningDialog.Show(exmsg, "Error");

            if (!(ex.Data.Contains("Application")))
                Logger.LogError(ex, "Exception bubbled up from the view model.");

            e.Handled = true;
        }
        #endregion

        #region UI Events

        //public async void AddQuestButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // should IQuest go into the IoC and we get this via that instead?
        //    var newQuest = new Quest();
        //    var result = await NavigationService.ShowDialogAsync<QuestOptions>(this);

        //    if (result is true)
        //    {
        //        MainViewModel.AddQuestQuiet(newQuest);
        //        MainViewModel.SelectedQuest = newQuest;
        //    }
        //}

        /// <summary>
        /// Event to cause the program to copy the current contents of the the tally
        /// results (ie: what's shown in the main text window) to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void copyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard is not null)
            {
                await Clipboard.SetTextAsync(MainViewModel.Output);
            }
        }

        /// <summary>
        /// Open the window for handling merging votes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void OpenManageVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            await NavigationService.ShowDialogAsync<ManageVotes>(this);

            MainViewModel.UpdateOutput();
        }

        /// <summary>
        /// Opens the global options window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void GlobalOptionsButton_Click(object sender, RoutedEventArgs e) => 
            await NavigationService.ShowDialogAsync<GlobalOptions>(this);

        /// <summary>
        /// Opens the quest options window.
        /// </summary>
        /// <remarks>
        /// Refactor this into a function taking a parameter instead?
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void QuestOptionsButton_Click(object sender, RoutedEventArgs e) => 
            await NavigationService.ShowDialogAsync<QuestOptions>(this);
        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public MainWindow() { throw new InvalidOperationException("The default constructor should not be called"); }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
