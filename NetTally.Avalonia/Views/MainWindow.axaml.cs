using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Avalonia.Navigation;
using NetTally.Configure;
using NetTally.Debugging.Logging;
using NetTally.Enums;
using NetTally.Product;
using NetTally.Utility.Events;
using NetTally.ViewModels;

namespace NetTally.Avalonia.Views;

public partial class MainWindow : Window
{
    #region Fields and Properties
    private readonly MainViewModel mainViewModel;
    private readonly AvaloniaNavigationService navigationService;
    private readonly ILogger<MainWindow> logger;
    private readonly IHostEnvironment hostEnvironment;
    private readonly GlobalSettings globalSettings;
    #endregion

    #region Startup/shutdown events

    /// <summary>
    /// Function that's run when the program first starts.
    /// Set up the data context links with the local variables.
    /// </summary>
    public MainWindow(
        MainViewModel viewModel,
        AvaloniaNavigationService navigationService,
        ILogger<MainWindow> logger,
        IHostEnvironment hostEnvironment,
        IOptions<GlobalSettings> globalOptions)
    {
        mainViewModel = viewModel;
        this.navigationService = navigationService;
        this.logger = logger;
        this.hostEnvironment = hostEnvironment;
        this.globalSettings = globalOptions.Value;

        // Initialize the window.
        InitializeComponent();

        // Buttons that can be disabled, that we want to still show tooltips for:
        ToolTip.SetShowOnDisabled(CancelTallyButton, true);
        ToolTip.SetShowOnDisabled(CopyToClipboardButton, true);
        ToolTip.SetShowOnDisabled(RemoveQuestButton, true);

        StartPost.AddHandler(PointerPressedEvent, TextBox_PointerPressed, RoutingStrategies.Tunnel);
        EndPost.AddHandler(PointerPressedEvent, TextBox_PointerPressed, RoutingStrategies.Tunnel);

        DataContext = mainViewModel;

        mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

        Title = $"{ProductInfo.Name} - {ProductInfo.Version}";
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (Design.IsDesignMode)
            return;

        if (hostEnvironment is null)
            return;

        if (hostEnvironment.IsDevelopment())
            return;

        ApplyTheme(globalSettings.AvaloniaThemeVariant);

        mainViewModel.CheckForNewRelease();
    }

    private static void ApplyTheme(AvaloniaTheme avaloniaTheme)
    {
        App.Current!.RequestedThemeVariant = avaloniaTheme switch
        {
            AvaloniaTheme.Light => ThemeVariant.Light,
            AvaloniaTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    #endregion

    #region Watched Events        
    /// <summary>
    /// Handles the PropertyChanged event of the MainViewModel control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
    private async void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        logger.PropertyChangeNotification(sender?.GetType(), e.PropertyName);

        // If a new quest was added, load the QuestOptions dialog to
        // allow setting the URL and display name.
        if (e.PropertyName == nameof(mainViewModel.AddQuestCommand))
        {
            string? clipboard = null;

            // If we have a URL in the clipboard, make use of that as
            // the default new URL for the quest.
            if (Clipboard is not null)
                clipboard = await Clipboard.TryGetTextAsync();

            string? uri = string.Empty;

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

    /// <summary>
    /// Handles the ExceptionRaised event of the MainViewModel control.
    /// This is called anytime there's an exception generated that needs
    /// to propogate up to the UI.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="ExceptionEventArgs"/> instance containing the event data.</param>
    private void MainViewModel_ExceptionRaised(object? sender, ExceptionEventArgs e)
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

        if (!ex.Data.Contains("Application"))
            logger.LogError(ex, "Exception bubbled up from the view model.");

        e.Handled = true;
    }
    #endregion

    #region UI Events
    /// <summary>
    /// Copy the current contents of the the tally results (ie: what's shown in the main text window) to the clipboard.
    /// </summary>
    public async void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Clipboard is not null)
            {
                await Clipboard.SetTextAsync(mainViewModel.Output);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error copying to clipboard");
        }
    }

    /// <summary>
    /// Open the window for handling merging votes.
    /// </summary>
    public async void OpenManageVotesWindow_Click(object sender, RoutedEventArgs e)
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

    /// <summary>
    /// Opens the global options window.
    /// </summary>
    public async void GlobalOptionsButton_Click(object sender, RoutedEventArgs e)
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

    /// <summary>
    /// Opens the quest options window.
    /// </summary>
    public async void QuestOptionsButton_Click(object sender, RoutedEventArgs e)
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

    private void TextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
        }
    }

    private void TextBox_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
        {
            tb.Focus();
            e.Handled = true;
        }
    }

    const string WikiHelp = "https://github.com/Kinematics/NetTally/wiki";
    const string NewReleasePage = "https://github.com/Kinematics/NetTally/releases/latest";

    /// <summary>
    /// Open a browser to view the wiki URL.
    /// </summary>
    public void OpenWikiHelpPage(object? sender, RoutedEventArgs e)
    {
        OpenHyperlink(WikiHelp);
    }

    /// <summary>
    /// Open a browser to view the new release page URL.
    /// </summary>
    public void OpenNewReleasePage(object? sender, RoutedEventArgs e)
    {
        OpenHyperlink(NewReleasePage);
    }

    private static void OpenHyperlink(string hyperlink)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo("cmd",
                $"/c start {hyperlink}")
            { CreateNoWindow = true });
        }

    }
    #endregion

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#if DEBUG
    /// <summary>
    /// A blank constructor is needed for Avalonia Windows. It should never be called.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
#endif
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
}
