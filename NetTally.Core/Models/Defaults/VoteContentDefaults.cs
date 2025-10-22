namespace NetTally.Models;

public static class VoteContentDefaults
{
    extension(VoteContent)
    {
        public static VoteContent None => _none;
    }

    private static readonly VoteContent _none = new("", "");
}
