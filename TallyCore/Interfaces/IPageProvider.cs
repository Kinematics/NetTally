using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IPageProvider
    {
        /// <summary>
        /// Asynchronously load pages based on the provided parameters.
        /// </summary>
        /// <param name="quest">The quest object describing which pages to load.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns a list of HTML documents defined by the requested quest.</returns>
        Task<List<HtmlDocument>> LoadPages(IQuest quest, CancellationToken token);

        /// <summary>
        /// Clear the cache of any previously loaded pages.
        /// </summary>
        void ClearPageCache();

        /// <summary>
        /// Have an event that can be watched for status messages.
        /// </summary>
        event EventHandler<MessageEventArgs> StatusChanged;

        /// <summary>
        /// Flag for whether to try to override the provided starting post by
        /// looking for the last threadmark.
        /// </summary>
        bool CheckForLastThreadmark { get; set; }
    }
}
