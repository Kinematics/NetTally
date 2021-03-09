using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.ExceptionServices;

namespace NetTally.Avalonia.Views
{
    public class MainWindow : Window
    {
        #region Fields and Properties
        private ViewModels.ViewModel ViewModel { get; }
        private Navigation.AvaloniaNavigationService NavigationService { get; }
        private ILogger<MainWindow> Logger { get; }
        #endregion

        #region Startup/shutdown events

        /// <summary>
        /// Function that's run when the program first starts.
        /// Set up the data context links with the local variables.
        /// </summary>
        public MainWindow(
            ViewModels.ViewModel model,
            Navigation.AvaloniaNavigationService navigationService, 
            ILogger<MainWindow> logger)
        {
            // Initialize the readonly properties.
            this.ViewModel = model;
            this.NavigationService = navigationService;
            this.Logger = logger;

            try
            {
                // Set up an event handler for any otherwise unhandled exceptions in the code.
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

                // Initialize the window.
                AvaloniaXamlLoader.Load(this);

                // Set the title.
                Title = $"{SystemInfo.ProductInfo.Name} - {SystemInfo.ProductInfo.Version}";

                // Load configuration data
                Collections.QuestCollection? quests = null;
                string? currentQuest = null;

                try
                {
                    this.Logger.LogDebug("Loading configuration.");
                    Config.NetTallyConfig.Load(out quests, out currentQuest, Options.AdvancedOptions.Instance);
                    this.Logger.LogInformation("Configuration loaded.");
                }
                catch (ConfigurationErrorsException e)
                {
                    this.Logger.LogError(e, "Failure during configuration.");
                    WarningDialog.Show("Error in configuration. Current configuration ignored.", "Error in configuration");
                }

                // Complete the platform setup.
                this.PlatformSetup(quests, currentQuest);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Failure during program startup.");
                WarningDialog.Show("Unable to start the program.", "Failure on startup");
                this.Close();
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

                ViewModel.InitializeQuests(quests, currentQuest);

                DataContext = ViewModel;
                ViewModel.PropertyChanged += MainViewModel_PropertyChanged;
                ViewModel.ExceptionRaised += MainViewModel_ExceptionRaised;

                ViewModel.CheckForNewRelease();
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "Invalid operation during platform setup.");
            }
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                string selectedQuest = ViewModel.SelectedQuest?.ThreadName ?? "";

                Config.NetTallyConfig.Save(ViewModel.QuestList, selectedQuest, Options.AdvancedOptions.Instance);

                Logger.LogDebug("Configuration saved.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to save configuration.");
                WarningDialog.Show("The program failed to save configuration data.", "Failed to save configuration");
            }
        }

        /// <summary>
        /// Handles the FirstChanceException event of the Current Domain.
        /// Logs all first chance exceptions when debug mode is on, for debug builds.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FirstChanceExceptionEventArgs"/> instance containing the event data.</param>
        private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (Options.AdvancedOptions.Instance.DebugMode)
                Logger.LogWarning(e.Exception, "First chance exception warning.");
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
            Logger.LogCritical(ex, "Unhandled exception");
            WarningDialog.Show("The program failed to handle an exception.", "Unhandled exception");
        }

        /// <summary>
        /// Raises the <see cref="Window" />.Closed event.
        /// Removes event listeners on close, to prevent memory leaks.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            this.SaveConfig();

            base.OnClosed(e);
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
        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.LogInformation($"Received notification of property change from MainViewModel: {e.PropertyName}.");
            
            this.SaveConfig();
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

        public async void AddQuestButton_Click(object sender, RoutedEventArgs e)
        {
            // should IQuest go into the IoC and we get this via that instead?
            var newQuest = new Quest();
            var result = await this.NavigationService.ShowDialogAsync<Views.QuestOptions>(this, newQuest, this.ViewModel.QuestList);

            if (result is true)
            {
                this.ViewModel.AddQuestQuiet(newQuest);
                this.ViewModel.SelectedQuest = newQuest;
            }
        }

        /// <summary>
        /// Event to cause the program to copy the current contents of the the tally
        /// results (ie: what's shown in the main text window) to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void copyToClipboardButton_Click(object sender, RoutedEventArgs e) => 
            Application.Current.Clipboard.SetTextAsync(ViewModel.Output);

        /// <summary>
        /// Open the window for handling merging votes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void OpenManageVotesWindow_Click(object sender, RoutedEventArgs e)
        {
            await this.NavigationService.ShowDialogAsync<ManageVotes>(this);

            ViewModel.UpdateOutput();
        }

        /// <summary>
        /// Opens the global options window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void GlobalOptionsButton_Click(object sender, RoutedEventArgs e) => 
            await this.NavigationService.ShowDialogAsync<GlobalOptions>(this, this.ViewModel.Options);

        /// <summary>
        /// Opens the quest options window.
        /// </summary>
        /// <remarks>
        /// Refactor this into a function taking a parameter instead?
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public async void QuestOptionsButton_Click(object sender, RoutedEventArgs e) => 
            await NavigationService.ShowDialogAsync<Views.QuestOptions>(this,
                                                                        this.ViewModel.SelectedQuest ?? throw new ArgumentNullException("Selected Quest is null."),
                                                                        this.ViewModel.QuestList);
        #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        /// <summary>
        /// A blank constructor is needed for Avalonia Windows. It should never be called.
        /// </summary>
        public MainWindow() { throw new InvalidOperationException("The default constructor should not be called"); }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
