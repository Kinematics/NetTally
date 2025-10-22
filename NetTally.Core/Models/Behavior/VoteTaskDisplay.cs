using NetTally.Configure;

namespace NetTally.Models;

public static class VoteTaskDisplay
{
    extension(VoteTask voteTask)
    {
        public string Display => voteTask.Name;

        public string BracketedDisplay => voteTask == VoteTask.None ?
            "" :
            $"[{voteTask.Name}]";

        public string HeaderDisplay => voteTask == VoteTask.None ?
            Strings.NoTask :
            voteTask.Name;
    }
}
