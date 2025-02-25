using Microsoft.Extensions.DependencyInjection;
using NetTally.Enums;

namespace NetTally.Input.Forums.ForumAdapters
{
    /// <summary>
    /// Class which allows getting an appropriate forum adapter for a given forum type.
    /// </summary>
    public class ForumAdapterFactory(
        IServiceProvider serviceProvider,
        ForumIdentifier forumIdentifier) : IDisposable
    {
        private readonly IServiceProvider serviceProvider = serviceProvider;
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
                await ss.WaitAsync(token).ConfigureAwait(ConfigureAwaitOptions.None);

                try
                {
                    quest.ForumType = await forumIdentifier.IdentifyForumTypeAsync(quest.ThreadUri, token)
                        .ConfigureAwait(ConfigureAwaitOptions.None);
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
                ForumType.XenForo1 => serviceProvider.GetRequiredService<XenForo1Adapter>(),
                ForumType.XenForo2 => serviceProvider.GetRequiredService<XenForo2Adapter>(),
                ForumType.vBulletin3 => serviceProvider.GetRequiredService<VBulletin3Adapter>(),
                ForumType.vBulletin4 => serviceProvider.GetRequiredService<VBulletin4Adapter>(),
                ForumType.vBulletin5 => serviceProvider.GetRequiredService<VBulletin5Adapter>(),
                //ForumType.vBulletin6 => serviceProvider.GetRequiredService<VBulletin5Adapter>(),
                ForumType.phpBB => serviceProvider.GetRequiredService<PhpBBAdapter>(),
                ForumType.Unknown => serviceProvider.GetRequiredService<UnknownForumAdapter>(),
                _ => throw new ArgumentException($"Unknown forum type: {forumType} for Uri: {uri}", nameof(forumType)),
            };
        }
    }
}
