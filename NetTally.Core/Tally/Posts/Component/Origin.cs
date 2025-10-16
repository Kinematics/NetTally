using NetTally.Configure;

namespace NetTally.Tally.Posts.Component;

public abstract record Origin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostId PostNumber, DateTimeOffset Timestamp);

public sealed record UserOrigin(Author Author, Uri Thread, Uri Permalink,
    PostId PostId, PostId PostNumber, DateTimeOffset Timestamp)
    : Origin(Author, Thread, Permalink, PostId, PostNumber, Timestamp);

public sealed record PlanOrigin(Origin Origin, Author PlanName) : Origin(Origin);

public static class PredefinedOrigins
{
    extension(Origin)
    {
        public static Origin None => _none;
    }

    static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);

    private static readonly Origin _none = new UserOrigin(Author.None,
        ExampleUri, ExampleUri, PostId.Zero, PostId.Zero, DateTimeOffset.MinValue);
}


