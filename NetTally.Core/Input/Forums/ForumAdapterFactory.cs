using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetTally.Forums.Adapters2;
using NetTally.Options;
using NetTally.Web;
using NetTally.Types.Enums;

namespace NetTally.Forums
{
    /// <summary>
    /// Class which allows getting an appropriate forum adapter for a given forum type.
    /// </summary>
    public class ForumAdapterFactory : IDisposable
    {
        readonly IGeneralInputOptions inputOptions;
        readonly ILoggerFactory loggerFactory;
        readonly SemaphoreSlim ss = new SemaphoreSlim(1);

        public ForumAdapterFactory(IGeneralInputOptions inputOptions, ILoggerFactory loggerFactory)
        {
            this.inputOptions = inputOptions;
            this.loggerFactory = loggerFactory;
        }

        #region Disposal
        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ForumAdapterFactory()
        {
            Dispose(false);
        }

        private void Dispose(bool managedDisposal)
        {
            if (managedDisposal && !disposed)
            {
                ss.Dispose();
                disposed = true;
            }
        }
        #endregion

        /// <summary>
        /// Create a new forum adapter appropriate to the provided quest.
        /// </summary>
        /// <param name="quest">The quest that we need a forum adapter for.</param>
        /// <param name="pageProvider">A page provider for requesting a page from the web site, if needed.</param>
        /// <param name="token">A cancellation token for if we need to make a web request.</param>
        /// <returns>Returns a forum adapter for the quest.</returns>
        public async Task<IForumAdapter2> CreateForumAdapterAsync(IQuest quest, IPageProvider pageProvider, CancellationToken token)
        {
            if (quest.ThreadUri == Quest.InvalidThreadUri)
                throw new InvalidOperationException("Quest does not have a valid thread specified.");

            if (quest.ForumType == ForumType.Unknown)
            {
                await ss.WaitAsync(token).ConfigureAwait(false);

                try
                {
                    quest.ForumType = await ForumIdentifier.IdentifyForumTypeAsync(quest.ThreadUri, pageProvider, token);
                }
                finally
                {
                    ss.Release();
                }
            }

            return CreateForumAdapter(quest.ForumType, quest.ThreadUri);
        }

        /// <summary>
        /// Get a forum adapter to match the provided forum type.
        /// </summary>
        /// <param name="forumType">The type of forum being requested.</param>
        /// <returns>Returns a forum adapter matching the requested forum type.</returns>
        public IForumAdapter2 CreateForumAdapter(ForumType forumType, Uri uri)
        {
            switch (forumType)
            {
                case ForumType.XenForo1:
                    return new XenForo1Adapter2(inputOptions, loggerFactory.CreateLogger<XenForo1Adapter2>());
                case ForumType.XenForo2:
                    return new XenForo2Adapter2(inputOptions, loggerFactory.CreateLogger<XenForo2Adapter2>());
                case ForumType.vBulletin3:
                    return new VBulletin3Adapter2(inputOptions, loggerFactory.CreateLogger<VBulletin3Adapter2>());
                case ForumType.vBulletin4:
                    return new VBulletin4Adapter2(inputOptions, loggerFactory.CreateLogger<VBulletin4Adapter2>());
                case ForumType.vBulletin5:
                    return new VBulletin5Adapter2(inputOptions, loggerFactory.CreateLogger<VBulletin5Adapter2>());
                case ForumType.phpBB:
                    return new PhpBBAdapter2(inputOptions, loggerFactory.CreateLogger<PhpBBAdapter2>());
                //case ForumType.NodeBB:
                //    return new NodeBBAdapter2(inputOptions, loggerFactory.CreateLogger<NodeBBAdapter2>());
                case ForumType.Unknown:
                    return new UnknownForumAdapter2(inputOptions, loggerFactory.CreateLogger<UnknownForumAdapter2>());
                default:
                    throw new ArgumentException($"Unknown forum type: {forumType} for Uri: {uri}", nameof(forumType));
            }
        }
    }
}
