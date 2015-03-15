using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Adapters;
using HtmlAgilityPack;

namespace NetTally
{
    public static class ForumAdapterFactory
    {
        /// <summary>
        /// Get a forum adapter for the site a quest is on.
        /// If not given a token, only do explicit checks.
        /// </summary>
        /// <param name="quest">The quest that a forum adapter is needed for.</param>
        /// <returns>Returns a forum adapter for the site the quest is on.</returns>
        public static IForumAdapter GetAdapter(IQuest quest)
        {
            IForumAdapter adapter = GetExplicitAdapter(quest);

            return adapter;
        }

        /// <summary>
        /// Get a forum adapter for the site a quest is on.
        /// If given a token, do implicit checks.
        /// </summary>
        /// <param name="quest">The quest that a forum adapter is needed for.</param>
        /// <returns>Returns a forum adapter for the site the quest is on.</returns>
        public static async Task<IForumAdapter> GetAdapter(IQuest quest, CancellationToken token)
        {
            IForumAdapter adapter = GetExplicitAdapter(quest);

            if (adapter == null)
            {
                adapter = await GetImplicitAdapter(quest, token);
            }

            return adapter;
        }

        /// <summary>
        /// Get adapter for site that we have explicitly built adapters for.
        /// An empty string gets the default forum adapter.
        /// </summary>
        /// <param name="quest">The quest that a forum adapter is needed for.</param>
        /// <returns>Returns a forum adapter for the site the quest is on.</returns>
        private static IForumAdapter GetExplicitAdapter(IQuest quest)
        {
            if (quest.Site.StartsWith("http://forums.sufficientvelocity.com/"))
                return new SufficientVelocityAdapter();
            else if (quest.Site.StartsWith("http://forums.sufficientvelocity.com/"))
                return new SpaceBattlesAdapter();
            else if (quest.Site == string.Empty)
                // Sufficient Velocity is the default if no site name is given
                return new SufficientVelocityAdapter();
            else return null;
        }

        /// <summary>
        /// Gets adapter for a site we can only implicitly guess the structure of.
        /// </summary>
        /// <param name="quest">The quest that a forum adapter is needed for.</param>
        /// <returns>Returns a forum adapter for the site the quest is on.
        /// Throws an exception if it is unable to identify a site's forums.</returns>
        private async static Task<IForumAdapter> GetImplicitAdapter(IQuest quest, CancellationToken token)
        {
            IPageProvider webPageProvider = new WebPageProvider();

            string url = quest.Site + quest.Name;

            var page = await webPageProvider.GetPage(url, quest.Site, false, token);

            bool found = CheckForVBulletin(page);
            if (found)
                return new vBulletinAdapter(quest.Site);

            return null;
        }

        private static bool CheckForVBulletin(HtmlDocument page)
        {
            if (page == null)
                return false;

            var root = page.DocumentNode;
            var head = root.Descendants("head").FirstOrDefault();
            if (head != null)
            {
                var generator = head.Elements("meta").FirstOrDefault(a => a.GetAttributeValue("name", "") == "generator");
                if (generator != null)
                {
                    if (generator.GetAttributeValue("content", "").StartsWith("vBulletin"))
                        return true;
                }
            }

            return false;
        }
    }
}
