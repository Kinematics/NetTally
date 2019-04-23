using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Forums.Adapters;

namespace NetTally.Forums
{
    /// <summary>
    /// Class which allows getting an appropriate forum adapter for a given forum type.
    /// </summary>
    class ForumAdapterSelector
    {
        /// <summary>
        /// A dictionary cache for created forum adapters.
        /// </summary>
        static readonly Dictionary<ForumType, IForumAdapter> cachedAdapters = new Dictionary<ForumType, IForumAdapter>();

        /// <summary>
        /// Get a forum adapter to match the provided forum type.
        /// </summary>
        /// <param name="forumType">The type of forum being requested.</param>
        /// <returns>Returns a forum adapter matching the requested forum type.</returns>
        public static IForumAdapter GetForumAdapter(ForumType forumType)
        {
            if (!cachedAdapters.TryGetValue(forumType, out IForumAdapter adapter))
            {
                switch (forumType)
                {
                    case ForumType.XenForo1:
                        adapter = new XenForo1Adapter(Utility.DefaultUri.Default);
                        break;
                    case ForumType.XenForo2:
                        adapter = new XenForo2Adapter(Utility.DefaultUri.Default);
                        break;
                    case ForumType.vBulletin3:
                        adapter = new vBulletin3Adapter(Utility.DefaultUri.Default);
                        break;
                    case ForumType.vBulletin4:
                        adapter = new vBulletin4Adapter(Utility.DefaultUri.Default);
                        break;
                    case ForumType.vBulletin5:
                        adapter = new vBulletin5Adapter(Utility.DefaultUri.Default);
                        break;
                    default:
                        throw new ArgumentException($"Unknown forum type: {forumType}", nameof(forumType));
                }

                cachedAdapters[forumType] = adapter;
            }

            return adapter;
        }

        /// <summary>
        /// Gets a forum adapter to match the provided Uri.
        /// </summary>
        /// <param name="site">The Uri of the site the adapter is for.</param>
        /// <returns>Returns a forum adapter matching the forum type of the page at the provided Uri.</returns>
        public static async Task<IForumAdapter> GetForumAdapterAsync(Uri? site, CancellationToken token)
        {
            if (site == null)
                throw new ArgumentNullException(nameof(site));

            ForumType forumType = await ForumIdentifier.IdentifyForumTypeAsync(site, token);

            if (!cachedAdapters.TryGetValue(forumType, out IForumAdapter adapter))
            {
                switch (forumType)
                {
                    case ForumType.XenForo1:
                        adapter = new XenForo1Adapter(site);
                        break;
                    case ForumType.XenForo2:
                        adapter = new XenForo2Adapter(site);
                        break;
                    case ForumType.vBulletin3:
                        adapter = new vBulletin3Adapter(site);
                        break;
                    case ForumType.vBulletin4:
                        adapter = new vBulletin4Adapter(site);
                        break;
                    case ForumType.vBulletin5:
                        adapter = new vBulletin5Adapter(site);
                        break;
                    default:
                        throw new ArgumentException($"Unknown forum type: {forumType} for site {site.Host}", nameof(forumType));
                }

                cachedAdapters[forumType] = adapter;
            }
            else
            {
                adapter.Site = site;
            }

            return adapter;
        }
    }
}
