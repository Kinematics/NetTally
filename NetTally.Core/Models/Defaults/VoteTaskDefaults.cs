using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class VoteTaskDefaults
{
    extension(VoteTask)
    {
        public static VoteTask Empty => _empty;
    }

    private static readonly VoteTask _empty = new("");
}
