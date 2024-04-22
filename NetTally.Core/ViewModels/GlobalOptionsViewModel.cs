using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTally.Extensions;
using NetTally.Configure;
using NetTally.Enums;

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
        }


        // Options list
        public List<string> RankVoteCountingModes { get; } = EnumExtensions.EnumDescriptionsList<RankVoteCounterMethod>().ToList();

        // Vote options
        [ObservableProperty]
        RankVoteCounterMethod rankVoteCounterMethod = RankVoteCounterMethod.Default;

        [ObservableProperty]
        BoolEx allowUsersToUpdatePlans = BoolEx.Unknown;

        // Output
        [ObservableProperty]
        bool globalSpoilers = false;

        [ObservableProperty]
        bool displayPlansWithNoVotes = false;

        // Networking
        [ObservableProperty]
        bool disableWebProxy = false;

        // Debugging
        [ObservableProperty]
        bool trackPostAuthorsUniquely = false;

        [ObservableProperty]
        bool debugMode = false;

        // Obsolete. Quest option now
        [ObservableProperty]
        DisplayMode displayMode = DisplayMode.Normal;
    }
}
