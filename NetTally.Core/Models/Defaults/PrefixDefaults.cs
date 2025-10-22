namespace NetTally.Models;

public static class PrefixDefaults
{
    extension(Prefix)
    {
        public static Prefix None => _none;
    }

    private static readonly Prefix _none = new("");
}
