namespace NetTally.Models;

public static class VoteLineDefaults
{
    extension(VoteLine)
    {
        public static VoteLine None => _none;
    }

    private static readonly VoteLine _none =
        new(Prefix.None, Marker.None, VoteTask.None, VoteContent.None);
}
