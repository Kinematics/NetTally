using System.Threading;
using System.Threading.Tasks;

namespace NetTally.Output
{
    public interface ITextResultsProvider
    {
        /// <summary>
        /// Public function to generate the full output for the tally.
        /// </summary>
        /// <param name="displayMode">The mode requested for how to format the output.</param>
        /// <param name="token">Cancellation token so that processing can be cancelled.</param>
        /// <returns>Returns the full string to be displayed.</returns>
        Task<string> BuildOutputAsync(DisplayMode displayMode, CancellationToken token);
    }
}
