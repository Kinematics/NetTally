using System.Threading;
using System.Threading.Tasks;
using NetTally.VoteCounting;
using NetTally.Types.Enums;

namespace NetTally.Output
{
    public interface ITextResultsProvider
    {
        /// <summary>
        /// Public function to generate the full output for the tally.
        /// </summary>
        /// <param name="quest">The quest to generate a tally output for.</param>
        /// <param name="token">Cancellation token so that processing can be cancelled.</param>
        /// <returns>Returns the full string to be displayed.</returns>
        Task<string> BuildOutputAsync(Quest quest, CancellationToken token);
    }
}
