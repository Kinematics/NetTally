﻿using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using NetTally.Types.Enums;

namespace NetTally.Global
{
    public partial class GlobalSettings : ObservableObject
    {
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
        [property: JsonIgnore]
        bool debugMode = false;

        public void UpdateFromLegacySettings(GlobalSettings legacySettings)
        {
            DisplayMode = legacySettings.DisplayMode;
            DisplayPlansWithNoVotes= legacySettings.DisplayPlansWithNoVotes;
            GlobalSpoilers= legacySettings.GlobalSpoilers;
            RankVoteCounterMethod = legacySettings.RankVoteCounterMethod;
            AllowUsersToUpdatePlans = legacySettings.AllowUsersToUpdatePlans;
            TrackPostAuthorsUniquely = legacySettings.TrackPostAuthorsUniquely;
            DisableWebProxy = legacySettings.DisableWebProxy;
        }
    }
}