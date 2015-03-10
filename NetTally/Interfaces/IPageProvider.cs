using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    public interface IPageProvider
    {
        /// <summary>
        /// Asynchronously load pages based on the provided parameters.
        /// </summary>
        /// <param name="questTitle">The title of the quest.</param>
        /// <param name="startPost">The starting post.</param>
        /// <param name="endPost">The ending post.</param>
        /// <returns>Returns a list of HTML documents.</returns>
        Task<List<HtmlDocument>> LoadPages(string questTitle, int startPost, int endPost);

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
