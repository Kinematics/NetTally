using System;
using System.Collections.Generic;
using NetTally.Adapters;
using NetTally.Web;

namespace NetTally.Forums
{
    /// <summary>
    /// Class which allows getting an appropriate forum adapter for a given forum type.
    /// </summary>
    public class ForumAdapterSelector
    {
        static Dictionary<ForumType, IForumAdapter> cachedAdapters = new Dictionary<ForumType, IForumAdapter>();

        /// <summary>
        /// Gets a forum adapter to match the provided forum type and Uri.
        /// </summary>
        /// <param name="forumType">The type of forum being requested.</param>
        /// <param name="site">The Uri of the site the adapter is for.</param>
        /// <returns>Returns a forum adapter matching the forum type, initialized to the provided Uri.</returns>
        public static IForumAdapter GetForumAdapter(ForumType forumType, Uri site)
        {
            if (cachedAdapters.TryGetValue(forumType, out IForumAdapter adapter))
            {
                adapter.Site = site;
                return adapter;
            }

            switch (forumType)
            {
                case ForumType.XenForo:
                    adapter = new XenForoAdapter(site);
                    break;
                case ForumType.vBulletin3:
                    adapter =  new vBulletinAdapter3(site);
                    break;
                case ForumType.vBulletin4:
                    adapter = new vBulletinAdapter4(site);
                    break;
                case ForumType.vBulletin5:
                    adapter = new vBulletinAdapter5(site);
                    break;
                default:
                    throw new ArgumentException($"Unknown forum type: {forumType}", nameof(forumType));
            }

            cachedAdapters[forumType] = adapter;

            return adapter;
        }
    }
}
