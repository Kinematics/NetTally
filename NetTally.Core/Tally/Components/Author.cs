using System;

namespace NetTally.Tally.Components
{
    public record Author
    {
        public string Name { get; init; }

        /// <summary>
        /// Constructor for an author.
        /// The name must have non-whitespace characters.
        /// </summary>
        /// <param name="name">The name of the author.</param>
        /// <exception cref="ArgumentException">Throws if name is null, empty, or only whitespace.</exception>
        public Author(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            // Trim any surrounding whitespace, if it exists.
            Name = name.Trim();
        }
    }
}
