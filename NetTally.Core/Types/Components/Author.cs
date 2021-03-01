using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally.Types.Components
{
    public record Author
    {
        public string Name { get; }

        /// <summary>
        /// Constructor for an author.
        /// The name must have non-whitespace characters.
        /// </summary>
        /// <param name="name">The name of the author.</param>
        /// <exception cref="ArgumentException">Throws if name is null, empty, or only whitespace.</exception>
        public Author(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"Invalid author name: '{name}'");

            // Trim any surrounding whitespace, if it exists.
            Name = name.Trim();
        }
    }
}
