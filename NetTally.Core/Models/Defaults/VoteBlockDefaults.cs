using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class VoteBlockDefaults
{
    extension(VoteBlock)
    {
        public static VoteBlock Empty => _empty;
    }

    private static readonly VoteBlock _empty = new([], Marker.None, VoteTask.Empty);
}
