using NetTally.Utility;

namespace NetTally.Models;

/// <summary>
/// Provides static methods for creating instances of <see cref="Author"/> with validated and sanitized names.
/// </summary>
/// <remarks>This class is intended for use when constructing <see cref="Author"/> objects from user input or
/// external sources. All methods ensure that author names are free of unsafe characters and are properly trimmed before
/// instantiation.</remarks>
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

