using NetTally.Models.Creation;
using NetTally.Models.Posts;
using NetTally.Models.Utility;

namespace NetTally.Models.Utility;

public static class AuthorUtility
{
    extension(Author author)
    {
        public Author Rename(string input) => author.Map(
                namedAuthor => namedAuthor with { Name = input },
                unknownAuthor => Author.Create(input),
                noAuthor => Author.Create(input));

        public string DisplayName => author.Map(
                namedAuthor => namedAuthor.Name,
                unknownAuthor => "⟦Unknown⟧",
                noAuthor => "⟦None⟧");
    }
}
