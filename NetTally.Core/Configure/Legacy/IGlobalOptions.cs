using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NetTally.Output;
using NetTally.VoteCounting;
using NetTally.Types.Enums;

namespace NetTally.Options
{
    public interface IGlobalOptions : INotifyPropertyChanged
    {
        RankVoteCounterMethod RankVoteCounterMethod { get; set; }
        BoolEx AllowUsersToUpdatePlans { get; set; }

        DisplayMode DisplayMode { get; set; }
        bool GlobalSpoilers { get; set; }
        bool DisplayPlansWithNoVotes { get; set; }

        bool TrackPostAuthorsUniquely { get; set; }
        bool DebugMode { get; set; }
        bool DisableWebProxy { get; set; }
    }
}
