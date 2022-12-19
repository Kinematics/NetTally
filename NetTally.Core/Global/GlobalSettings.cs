using System;
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
        bool disableWebProxy = false;
        [ObservableProperty]
        bool debugMode = false;
        [ObservableProperty]
        bool trackPostAuthorsUniquely = false;
    }
}