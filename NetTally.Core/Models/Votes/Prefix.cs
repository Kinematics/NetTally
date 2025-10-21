namespace NetTally.Models.Votes;

/// <summary>
/// Data type to store vote indentation information.
/// </summary>
/// <param name="Indent">The indent string.</param>
public sealed record Prefix(string Indent);

public static class PrefixPredefined
{
    extension(Prefix)
    {
        public static Prefix Empty => _empty;
    }

    private static readonly Prefix _empty = new("");
}
