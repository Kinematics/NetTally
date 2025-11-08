using NetTally.Models;

namespace NetTally.Output;

public interface ITextResultsProvider
{
    /// <summary>
    /// Public function to generate the full output for the tally.
    /// </summary>
    /// <param name="quest">The quest to generate a tally output for.</param>
    /// <returns>Returns the full string to be displayed.</returns>
    string BuildOutput(Quest quest);
}
