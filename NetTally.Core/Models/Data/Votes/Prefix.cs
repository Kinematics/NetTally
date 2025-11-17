namespace NetTally.Models;

/// <summary>
/// Data type to store vote indentation information (hyphens before the vote line).
/// </summary>
/// <param name="Indent">The indent string.</param>
public sealed record Prefix(string Indent);
