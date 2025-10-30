namespace NetTally.Models;

public abstract record Source();

public sealed record NoSource() : Source;

public sealed record SourceLocation(Uri Thread, Uri Permalink, PostId PostId, PostNumber PostNumber) : Source;
