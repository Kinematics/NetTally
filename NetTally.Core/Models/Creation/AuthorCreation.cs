using NetTally.Models.Creation;
using NetTally.Models.Posts;
using NetTally.Utility;

namespace NetTally.Models.Creation;

public static class AuthorCreation
{
    extension(Author)
    {
        /// <summary>
        /// Create a new <see cref="Author"/> with the given name.
        /// </summary>
        /// <param name="name">The name of the author.</param>
        /// <returns>An <see cref="Author"/>. If no name is provided, 
        /// returns <see cref="Author.None"/></returns>
        public static Author Create(string name)
        {
            name = name.RemoveUnsafeCharacters().Trim();

            if (string.IsNullOrEmpty(name))
                return Author.None;

            return new NamedAuthor(name);
        }
    }
}

