using NetTally.Models.Mapping;

namespace NetTally.Models;

public static class AuthorUtility
{
    extension(Author author)
    {
        public Author Rename(string input) => author.Map(
                namedAuthor => namedAuthor with { Name = input },
                unknownAuthor => Author.Create(input) ?? Author.Unknown,
                noAuthor => Author.Create(input) ?? Author.None);
    }
}
