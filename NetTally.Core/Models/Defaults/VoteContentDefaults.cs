using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class VoteContentDefaults
{
    extension(VoteContent)
    {
        public static VoteContent Empty => _empty;
    }

    private static readonly VoteContent _empty = new("", "");
}
