using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetTally.Forums.ForumAdapters;
using NetTally.Configure.Legacy;
using NetTally.Types.Enums;
using Microsoft.Extensions.Options;
using NetTally.Configure;

namespace NetTally.Forums
{
    /// <summary>
    /// Class which allows getting an appropriate forum adapter for a given forum type.
    /// </summary>
    public class ForumAdapterFactory(
        IOptions<GlobalSettings> options,
        ILoggerFactory loggerFactory,
        ForumIdentifier forumIdentifier) : IDisposable
    {
        private readonly IOptions<GlobalSettings> inputOptions = options;
        private readonly ILoggerFactory loggerFactory = loggerFactory;
        private readonly ForumIdentifier forumIdentifier = forumIdentifier;
        private readonly SemaphoreSlim ss = new(1);

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
        public async Task<IForumAdapter> CreateForumAdapterAsync(Quest quest, CancellationToken token)
        {
            if (quest.ThreadUri == Quest.InvalidThreadUri)
                throw new InvalidOperationException("Quest does not have a valid thread specified.");

            if (quest.ForumType == ForumType.Unknown)
            {
                await ss.WaitAsync(token).ConfigureAwait(false);

                try
                {
                    quest.ForumType = await forumIdentifier.IdentifyForumTypeAsync(quest.ThreadUri, token);
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
        public IForumAdapter CreateForumAdapter(ForumType forumType, Uri uri)
        {
            return forumType switch
            {
                ForumType.XenForo1 => new XenForo1Adapter(inputOptions, loggerFactory.CreateLogger<XenForo1Adapter>()),
                ForumType.XenForo2 => new XenForo2Adapter(inputOptions, loggerFactory.CreateLogger<XenForo2Adapter>()),
                ForumType.vBulletin3 => new VBulletin3Adapter(inputOptions, loggerFactory.CreateLogger<VBulletin3Adapter>()),
                ForumType.vBulletin4 => new VBulletin4Adapter(inputOptions, loggerFactory.CreateLogger<VBulletin4Adapter>()),
                ForumType.vBulletin5 => new VBulletin5Adapter(inputOptions, loggerFactory.CreateLogger<VBulletin5Adapter>()),
                ForumType.phpBB => new PhpBBAdapter(inputOptions, loggerFactory.CreateLogger<PhpBBAdapter>()),
                //ForumType.NodeBB => new NodeBBAdapter2(inputOptions, loggerFactory.CreateLogger<NodeBBAdapter2>()),
                ForumType.Unknown => new UnknownForumAdapter(inputOptions, loggerFactory.CreateLogger<UnknownForumAdapter>()),
                _ => throw new ArgumentException($"Unknown forum type: {forumType} for Uri: {uri}", nameof(forumType)),
            };
        }
    }
}
