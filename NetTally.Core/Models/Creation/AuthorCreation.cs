using NetTally.Utility;

namespace NetTally.Models;

public static class AuthorCreation
{
    extension(Author)
    {
        /// <summary>
        /// Create a new <see cref="Author"/> with the given name.
        /// Unsafe characters are removed, and the name is trimmed.
        /// </summary>
        /// <param name="name">The name of the author.</param>
        /// <returns>An <see cref="Author"/>. If no valid name is provided, 
        /// returns <c>null</c>.</returns>
        public static Author? Create(string? name)
        {
            if (name is null)
                return null;

            name = name.RemoveUnsafeCharacters().Trim();

            if (string.IsNullOrEmpty(name))
                return null;

            return new NamedAuthor(name);
        }
    }
}

