using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class VoteTaskDefaults
{
    extension(VoteTask)
    {
        public static VoteTask None => _none;
    }

    private static readonly VoteTask _none = new("");
}
