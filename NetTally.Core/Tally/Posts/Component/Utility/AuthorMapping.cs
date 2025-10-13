namespace NetTally.Tally.Posts.Component.Utility;
static class AuthorMapping
{
    extension(Author author)
    {
        public T Map<T>(
            Func<NamedAuthor, T> namedFunc,
            Func<UnknownAuthor, T> unknownFunc,
            Func<NoAuthor, T> noFunc)
        {
            return author switch
            {
                NamedAuthor namedAuthor => namedFunc(namedAuthor),
                UnknownAuthor unknownAuthor => unknownFunc(unknownAuthor),
                NoAuthor noAuthor => noFunc(noAuthor),
                _ => throw new InvalidOperationException("Unknown Author type.")
            };
        }
    }
}
