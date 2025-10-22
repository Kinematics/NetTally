using NetTally.Configure;
using NetTally.Models.Posts;

namespace NetTally.Models.Defaults;

public static class OriginDefaults
{
    extension(Origin)
    {
        public static Origin None => _none;
    }

    private static readonly Origin _none = new UserOrigin(Author.None,
        Strings.ExampleUri, Strings.ExampleUri, PostId.None, PostNumber.None, DateTimeOffset.MinValue);
}


