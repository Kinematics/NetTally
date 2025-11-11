using NetTally.Models.Mapping;

namespace NetTally.Models;

public static class AuthorUtility
{
    extension(Author author)
    {
        public Author? Rename(string input) => author.Map(
                namedAuthor => Author.Create(input),
                unknownAuthor => Author.Create(input),
                noAuthor => Author.Create(input));
    }
}
