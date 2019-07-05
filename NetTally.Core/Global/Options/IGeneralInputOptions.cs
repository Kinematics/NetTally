using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NetTally.VoteCounting;

namespace NetTally.Options
{
    public interface IGeneralInputOptions : INotifyPropertyChanged
    {
        bool DisableWebProxy { get; }

        bool AllowRankedVotes { get; }
        RankVoteCounterMethod RankVoteCounterMethod { get; }

        bool DebugMode { get; }

    }
}
