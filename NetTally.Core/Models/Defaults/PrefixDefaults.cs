using NetTally.Models.Votes;

namespace NetTally.Models.Defaults;

public static class PrefixDefaults
{
    extension(Prefix)
    {
        public static Prefix None => _none;
    }

    private static readonly Prefix _none = new("");
}
