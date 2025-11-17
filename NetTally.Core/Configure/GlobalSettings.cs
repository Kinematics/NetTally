using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using NetTally.Enums;

namespace NetTally.Configure;

public partial class GlobalSettings : ObservableObject
{
    [ObservableProperty]
    public partial DisplayMode DisplayMode { get; set; }

    [ObservableProperty]
    public partial bool DisplayPlansWithNoVotes { get; set; } = false;

    [ObservableProperty]
    public partial bool GlobalSpoilers { get; set; } = false;

    [ObservableProperty]
    public partial RankVoteCounterMethod RankVoteCounterMethod { get; set; } = RankVoteCounterMethod.Default;

    [ObservableProperty]
    public partial BoolEx AllowUsersToUpdatePlans { get; set; } = BoolEx.Unknown;

    [ObservableProperty]
    [JsonIgnore]
    public partial bool TrackPostAuthorsUniquely { get; set; } = false;

    [ObservableProperty]
    public partial bool DisableWebProxy { get; set; } = false;

    [ObservableProperty]
    [JsonIgnore]
    public partial bool DebugMode { get; set; } = false;

    [ObservableProperty]
    public partial AvaloniaTheme AvaloniaThemeVariant { get; set; } = AvaloniaTheme.Default;

    [ObservableProperty]
    public partial WPFTheme WPFThemeVariant { get; set; } = WPFTheme.None;

    public void UpdateFromLegacySettings(GlobalSettings legacySettings)
    {
        DisplayMode = legacySettings.DisplayMode;
        DisplayPlansWithNoVotes = legacySettings.DisplayPlansWithNoVotes;
        GlobalSpoilers = legacySettings.GlobalSpoilers;
        RankVoteCounterMethod = legacySettings.RankVoteCounterMethod;
        AllowUsersToUpdatePlans = legacySettings.AllowUsersToUpdatePlans;
        TrackPostAuthorsUniquely = legacySettings.TrackPostAuthorsUniquely;
        DisableWebProxy = legacySettings.DisableWebProxy;
    }
}