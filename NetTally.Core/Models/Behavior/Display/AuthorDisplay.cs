using NetTally.Configure;
using NetTally.Models.Mapping;

namespace NetTally.Models;

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
