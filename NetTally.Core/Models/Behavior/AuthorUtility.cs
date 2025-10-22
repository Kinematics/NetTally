using NetTally.Models.Behavior;
using NetTally.Models.Creation;
using NetTally.Models.Defaults;
using NetTally.Models.Mapping;
using NetTally.Models.Posts;

namespace NetTally.Models.Behavior;

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
