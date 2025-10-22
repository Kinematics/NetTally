namespace NetTally.Models;

public abstract record Origin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostNumber PostNumber, DateTimeOffset Timestamp);

public sealed record UserOrigin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostNumber PostNumber, DateTimeOffset Timestamp)
    : Origin(Author, Thread, Permalink, PostId, PostNumber, Timestamp);

public sealed record PlanOrigin(Origin Origin, Author PlanName) : Origin(Origin);


