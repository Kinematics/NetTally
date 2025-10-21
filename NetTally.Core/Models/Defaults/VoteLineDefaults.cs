using NetTally.Models.Defaults;
using NetTally.Models.Display;
using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class VoteLineDefaults
{
    extension(VoteLine)
    {
        public static VoteLine Empty => _empty;
    }

    private static readonly VoteLine _empty =
        new(Prefix.Empty, Marker.None, VoteTask.Empty, VoteContent.Empty);
}
