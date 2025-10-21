namespace NetTally.Models.Posts;

public abstract record Origin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostId PostNumber, DateTimeOffset Timestamp);

public sealed record UserOrigin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostId PostNumber, DateTimeOffset Timestamp)
    : Origin(Author, Thread, Permalink, PostId, PostNumber, Timestamp);

public sealed record PlanOrigin(Origin Origin, Author PlanName) : Origin(Origin);


