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
        /// <param name="forumAdapter">The forum adapter to use when getting/analyzing pages.</param>
        /// <param name="quest">The quest object describing which pages to load.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns a list of HTML documents defined by the requested quest.</returns>
        Task<List<HtmlDocument>> LoadPages(IForumAdapter forumAdapter, IQuest quest, CancellationToken token);

        /// <summary>
        /// Clear the cache of any previously loaded pages.
        /// </summary>
        void ClearPageCache();

        /// <summary>
        /// Have an event that can be watched for status messages.
        /// </summary>
        event EventHandler<MessageEventArgs> StatusChanged;
    }
}
