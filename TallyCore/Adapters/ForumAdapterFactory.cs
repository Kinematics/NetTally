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
            IForumAdapter adapter = GetExplicitAdapter(quest) ?? await GetImplicitAdapter(quest, token);

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
            switch (quest.SiteName)
            {
                case "http://forums.sufficientvelocity.com/":
                case "http://forums.spacebattles.com/":
                case "https://forum.questionablequesting.com/":
                    // Known XenForo sites
                    return new XenForoAdapter(quest.SiteName);
                case "":
                    // Sufficient Velocity is the default if no site name is given
                    return new XenForoAdapter("http://forums.sufficientvelocity.com/");
                default:
                    return null;
            }
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

            var page = await webPageProvider.GetPage(quest.ThreadName, quest.SiteName, Caching.UseCache, token);

            if (token.IsCancellationRequested)
                return null;

            if (CheckForXenForo(page))
                return new XenForoAdapter(quest.SiteName);

            if (CheckForVBulletin5(page))
                return new vBulletinAdapter5(quest.SiteName);

            if (CheckForVBulletin4(page))
                return new vBulletinAdapter4(quest.SiteName);

            if (CheckForVBulletin3(page))
                return new vBulletinAdapter3(quest.SiteName);

            return null;
        }

        /// <summary>
        /// Determine if the provided page has the signature indicating that it
        /// is a XenForo forum site.
        /// </summary>
        /// <param name="page">Page to check.</param>
        /// <returns>Returns true if it's a XenForo forum.</returns>
        private static bool CheckForXenForo(HtmlDocument page)
        {
            return page?.DocumentNode.Element("html")?.Id == "XenForo";
        }

        /// <summary>
        /// Determine if the provided page has the signature indicating that it
        /// is a XenForo forum site.
        /// </summary>
        /// <param name="page">Page to check.</param>
        /// <returns>Returns true if it's a XenForo forum.</returns>
        private static bool CheckForVBulletin5(HtmlDocument page)
        {
            return page?.DocumentNode.Element("html")?.Element("body")?.Id == "vb-page-body";
        }

        /// <summary>
        /// Determine if the provided page has the signature indicating that it
        /// is a XenForo forum site.
        /// </summary>
        /// <param name="page">Page to check.</param>
        /// <returns>Returns true if it's a XenForo forum.</returns>
        private static bool CheckForVBulletin4(HtmlDocument page)
        {
            return page?.DocumentNode.Element("html")?.Id == "vbulletin_html";
        }

        /// <summary>
        /// Determine if the provided page has the signature indicating that it
        /// is a vBulletin forum site.
        /// </summary>
        /// <param name="page">Page to check.</param>
        /// <returns>Returns true if it's a vBulletin forum.</returns>
        private static bool CheckForVBulletin3(HtmlDocument page)
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
