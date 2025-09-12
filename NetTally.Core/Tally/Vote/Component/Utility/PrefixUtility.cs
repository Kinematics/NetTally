using NetTally.Tally.Vote.Component.Creation;

namespace NetTally.Tally.Vote.Component;

/// <summary>
/// Extension class containing methods to manpulate a <see cref="Prefix"/>.
/// </summary>
public static class PrefixUtility
{
    extension(Prefix prefix)
    {
        public int Depth => prefix.Indent.Length;

        public Prefix Promote(int levels = 1)
        {
            if (levels < 1)
                return prefix;

            int newDepth = prefix.Depth - levels;

            return Prefix.Create(newDepth);
        }
    }
}
