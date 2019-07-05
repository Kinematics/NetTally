using System;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Forums.Adapters;
using NetTally.Web;

namespace NetTally.Forums
{
    /// <summary>
    /// Class which allows getting an appropriate forum adapter for a given forum type.
    /// </summary>
    public class ForumAdapterFactory
    {
        /// <summary>
        /// Create a new forum adapter appropriate to the provided quest.
        /// </summary>
        /// <param name="quest">The quest that we need a forum adapter for.</param>
        /// <param name="pageProvider">A page provider for requesting a page from the web site, if needed.</param>
        /// <param name="token">A cancellation token for if we need to make a web request.</param>
        /// <returns>Returns a forum adapter for the quest.</returns>
        public async Task<IForumAdapter> CreateForumAdapterAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest.ThreadUri == null)
                throw new InvalidOperationException("Quest has no valid web host.");

            if (quest.ForumType == ForumType.Unknown)
            {
                quest.ForumType = await ForumIdentifier.IdentifyForumTypeAsync(quest.ThreadUri, pageProvider, token);
            }

            return CreateForumAdapter(quest.ForumType, quest.ThreadUri);
        }

        /// <summary>
        /// Get a forum adapter to match the provided forum type.
        /// </summary>
        /// <param name="forumType">The type of forum being requested.</param>
        /// <returns>Returns a forum adapter matching the requested forum type.</returns>
        public IForumAdapter CreateForumAdapter(ForumType forumType, Uri uri)
        {
            switch (forumType)
            {
                case ForumType.XenForo1:
                    return new XenForo1Adapter(uri);
                case ForumType.XenForo2:
                    return new XenForo2Adapter(uri);
                case ForumType.vBulletin3:
                    return new vBulletin3Adapter(uri);
                case ForumType.vBulletin4:
                    return new vBulletin4Adapter(uri);
                case ForumType.vBulletin5:
                    return new vBulletin5Adapter(uri);
                case ForumType.phpBB:
                    return new phpBBAdapter(uri);
                case ForumType.NodeBB:
                    return new NodeBBAdapter(uri);
                default:
                    throw new ArgumentException($"Unknown forum type: {forumType} for Uri: {uri}", nameof(forumType));
            }
        }
    }
}
