using NetTally.Configure;
using NetTally.Models.Creation;
using NetTally.Models.Defaults;
using NetTally.Models.Mapping;
using NetTally.Models.Posts;

namespace NetTally.Models.Behavior;

public static class AuthorDisplay
{
    extension(Author author)
    {
        public string DisplayName => author.Map(
                namedAuthor => namedAuthor.Name,
                unknownAuthor => Strings.UnknownAuthor,
                noAuthor => Strings.NoAuthor);
    }
}
