using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class PrefixDefaults
{
    extension(Prefix)
    {
        public static Prefix Empty => _empty;
    }

    private static readonly Prefix _empty = new("");
}
