using System;
using System.ComponentModel;
using NetTally.Enums;

namespace NetTally.Configure.Legacy
{
    public interface IGeneralInputOptions : INotifyPropertyChanged
    {
        bool DisableWebProxy { get; }

        [Obsolete("No longer used")]
        bool AllowRankedVotes { get; }
        RankVoteCounterMethod RankVoteCounterMethod { get; }
        BoolEx AllowUsersToUpdatePlans { get; }

        bool TrackPostAuthorsUniquely { get; }
        bool DebugMode { get; }

    }
}
