using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NetTally.VoteCounting;
using NetTally.Types.Enums;

namespace NetTally.Options
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
