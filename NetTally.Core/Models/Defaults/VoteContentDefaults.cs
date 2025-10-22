using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class VoteContentDefaults
{
    extension(VoteContent)
    {
        public static VoteContent None => _none;
    }

    private static readonly VoteContent _none = new("", "");
}
