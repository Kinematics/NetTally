using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Extensions;

namespace NetTally.ViewModels
{
    /// <summary>
    /// The view model that provides an interface to the global program options.
    /// </summary>
    public partial class GlobalOptionsViewModel : ObservableObject
    {
        private readonly GlobalSettings globalSettings;
        private readonly ILogger<GlobalOptionsViewModel> logger;

        public GlobalOptionsViewModel(
            IOptions<GlobalSettings> options,
            ILogger<GlobalOptionsViewModel> logger)
        {
            globalSettings = options.Value;
            this.logger = logger;

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
            ThemeVariant = globalSettings.ThemeVariant;
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
            ThemeVariant = AvaloniaTheme.Default;

            logger.LogDebug("Global options were reset.");
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
            globalSettings.ThemeVariant = ThemeVariant;
        }

        // Options list
        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

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
        public partial AvaloniaTheme ThemeVariant { get; set; } = AvaloniaTheme.Default;
    }
}
