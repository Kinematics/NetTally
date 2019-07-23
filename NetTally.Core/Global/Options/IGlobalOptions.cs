using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NetTally.Output;
using NetTally.VoteCounting;

namespace NetTally.Options
{
    public interface IGlobalOptions : INotifyPropertyChanged
    {
        bool DisableWebProxy { get; set; }

        [Obsolete("No longer used")]
        bool AllowRankedVotes { get; set; }
        RankVoteCounterMethod RankVoteCounterMethod { get; set; }

        BoolEx AllowUsersToUpdatePlans { get; set; }

        DisplayMode DisplayMode { get; set; }
        bool GlobalSpoilers { get; set; }
        bool DisplayPlansWithNoVotes { get; set; }

        bool DebugMode { get; set; }

    }
}
