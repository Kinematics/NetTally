namespace NetTally.Models;

public static class PrefixProperties
{
    extension(Prefix prefix)
    {
        public int Depth => prefix.Indent.Length;
    }
}
