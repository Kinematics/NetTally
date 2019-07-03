using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NetTally.Output;
using NetTally.VoteCounting;

namespace NetTally.Options
{
    public interface IGeneralOutputOptions : INotifyPropertyChanged
    {
        DisplayMode DisplayMode { get; }
        bool GlobalSpoilers { get; }
        bool DisplayPlansWithNoVotes { get; }
        RankVoteCounterMethod RankVoteCounterMethod { get; }

        bool DebugMode { get; }
    }
}
