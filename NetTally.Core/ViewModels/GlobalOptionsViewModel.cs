using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Debugging.Logging;
using NetTally.Enums;
using NetTally.Utility.Enumerations;

namespace NetTally.ViewModels;

/// <summary>
/// The view model that provides an interface to the global program options.
/// </summary>
public partial class GlobalOptionsViewModel : ObservableObject
{
    private readonly GlobalSettings globalSettings;
    private readonly ILogger<GlobalOptionsViewModel> logger;

    private readonly AvaloniaTheme originalAvaloniaTheme;
    private readonly WPFTheme originalWPFTheme;


    public GlobalOptionsViewModel(
        IOptions<GlobalSettings> options,
        ILogger<GlobalOptionsViewModel> logger)
    {
        globalSettings = options.Value;
        this.logger = logger;

        originalAvaloniaTheme = globalSettings.AvaloniaThemeVariant;
        originalWPFTheme = globalSettings.WPFThemeVariant;

        LoadGlobalOptions();
    }

    private void LoadGlobalOptions()
    {
        DisplayMode = globalSettings.DisplayMode;
        DisplayPlansWithNoVotes = globalSettings.DisplayPlansWithNoVotes;
        GlobalSpoilers = globalSettings.GlobalSpoilers;
        RankVoteCounterMethod = globalSettings.RankVoteCounterMethod;
        AllowUsersToUpdatePlans = globalSettings.AllowUsersToUpdatePlans;
        TrackPostAuthorsUniquely = globalSettings.TrackPostAuthorsUniquely;
        DisableWebProxy = globalSettings.DisableWebProxy;
        DebugMode = globalSettings.DebugMode;
        AvaloniaThemeVariant = globalSettings.AvaloniaThemeVariant;
        WPFThemeVariant = globalSettings.WPFThemeVariant;
    }

    [RelayCommand]
    private void Reset()
    {
        DisplayMode = DisplayMode.Normal;
        DisplayPlansWithNoVotes = false;
        GlobalSpoilers = false;
        RankVoteCounterMethod = RankVoteCounterMethod.Default;
        AllowUsersToUpdatePlans = BoolEx.Unknown;
        TrackPostAuthorsUniquely = false;
        DisableWebProxy = false;
        DebugMode = false;
        AvaloniaThemeVariant = originalAvaloniaTheme;
        WPFThemeVariant = originalWPFTheme;

        logger.GlobalOptionsReset();
    }

    [RelayCommand]
    private void Save()
    {
        SaveGlobalOptions();
        OnPropertyChanged(nameof(SaveCommand));
    }

    private void SaveGlobalOptions()
    {
        globalSettings.DisplayMode = DisplayMode;
        globalSettings.DisplayPlansWithNoVotes = DisplayPlansWithNoVotes;
        globalSettings.GlobalSpoilers = GlobalSpoilers;
        globalSettings.RankVoteCounterMethod = RankVoteCounterMethod;
        globalSettings.AllowUsersToUpdatePlans = AllowUsersToUpdatePlans;
        globalSettings.TrackPostAuthorsUniquely = TrackPostAuthorsUniquely;
        globalSettings.DisableWebProxy = DisableWebProxy;
        globalSettings.DebugMode = DebugMode;
        globalSettings.AvaloniaThemeVariant = AvaloniaThemeVariant;
        globalSettings.WPFThemeVariant = WPFThemeVariant;
    }

    // Options list
    public List<string> RankVoteCountingModes { get; } = [.. EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>()];

    // Vote options
    [ObservableProperty]
    public partial RankVoteCounterMethod RankVoteCounterMethod { get; set; } = RankVoteCounterMethod.Default;

    [ObservableProperty]
    public partial BoolEx AllowUsersToUpdatePlans { get; set; } = BoolEx.Unknown;

    // Output
    [ObservableProperty]
    public partial bool GlobalSpoilers { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayPlansWithNoVotes { get; set; } = false;

    // Networking
    [ObservableProperty]
    public partial bool DisableWebProxy { get; set; } = false;

    // Debugging
    [ObservableProperty]
    public partial bool TrackPostAuthorsUniquely { get; set; } = false;

    [ObservableProperty]
    public partial bool DebugMode { get; set; } = false;

    // Obsolete. Quest option now
    [ObservableProperty]
    public partial DisplayMode DisplayMode { get; set; } = DisplayMode.Normal;

    // Theming
    public List<AvaloniaTheme> AvaloniaThemes { get; } = [AvaloniaTheme.Light, AvaloniaTheme.Dark];

    [ObservableProperty]
    public partial AvaloniaTheme AvaloniaThemeVariant { get; set; } = AvaloniaTheme.Default;

    partial void OnAvaloniaThemeVariantChanged(AvaloniaTheme value)
    {
        globalSettings.AvaloniaThemeVariant = value;
    }

    public List<WPFTheme> WPFThemes { get; } = [WPFTheme.System, WPFTheme.Light, WPFTheme.Dark];

    [ObservableProperty]
    public partial WPFTheme WPFThemeVariant { get; set; } = WPFTheme.System;

    partial void OnWPFThemeVariantChanged(WPFTheme value)
    {
        globalSettings.WPFThemeVariant = value;
    }

}
