using NetTally.Models.Defaults;
using NetTally.Models.Votes;

namespace NetTally.Models.Behavior;

public static class VoteTaskDisplay
{
    extension(VoteTask voteTask)
    {
        public string Display => voteTask.Name;

        public string BracketedDisplay => voteTask == VoteTask.None ?
            "" :
            $"[{voteTask.Name}]";
    }
}
