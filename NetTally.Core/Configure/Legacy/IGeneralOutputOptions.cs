using System.ComponentModel;
using NetTally.Enums;

namespace NetTally.Configure.Legacy;

public interface IGeneralOutputOptions : INotifyPropertyChanged
{
    DisplayMode DisplayMode { get; }
    bool GlobalSpoilers { get; }
    bool DisplayPlansWithNoVotes { get; }
    RankVoteCounterMethod RankVoteCounterMethod { get; }

    bool DebugMode { get; }
}
