using NetTally.Configure;
using NetTally.Models.Defaults;
using NetTally.Models.Posts;

namespace NetTally.Models.Defaults;

public static class OriginDefaults
{
    extension(Origin)
    {
        public static Origin None => _none;
    }

    static readonly Uri ExampleUri = new(Strings.ExampleHostUrl);

    private static readonly Origin _none = new UserOrigin(Author.None,
        ExampleUri, ExampleUri, PostId.Zero, PostNumber.Zero, DateTimeOffset.MinValue);
}


