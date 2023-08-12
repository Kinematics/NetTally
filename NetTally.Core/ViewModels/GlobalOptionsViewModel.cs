using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Extensions;
using NetTally.Global;
using NetTally.Types.Enums;

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

            logger.LogDebug("Global options were reset.");
        }

        [RelayCommand]
        private void Save()
        {
            SaveGlobalOptions();
            SaveCompleted?.Invoke();
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
        }

        public event Action? SaveCompleted;


        //public GlobalSettings GlobalSettings { get; }
        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();


        [ObservableProperty]
        DisplayMode displayMode = DisplayMode.Normal;
        [ObservableProperty]
        bool displayPlansWithNoVotes = false;
        [ObservableProperty]
        bool globalSpoilers = false;
        [ObservableProperty]
        RankVoteCounterMethod rankVoteCounterMethod = RankVoteCounterMethod.Default;

        [ObservableProperty]
        BoolEx allowUsersToUpdatePlans = BoolEx.Unknown;
        [ObservableProperty]
        bool trackPostAuthorsUniquely = false;

        [ObservableProperty]
        bool disableWebProxy = false;
        [ObservableProperty]
        bool debugMode = false;

    }
}
