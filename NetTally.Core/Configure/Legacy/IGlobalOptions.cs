using System.ComponentModel;
using NetTally.Enums;

namespace NetTally.Configure.Legacy
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
