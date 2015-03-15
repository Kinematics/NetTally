using System;
using NetTally.Adapters;

namespace NetTally
{
    public static class ForumAdapterFactory
    {
        /// <summary>
        /// Get a forum adapter for the site a quest is on.
        /// </summary>
        /// <param name="quest">The quest that a forum adapter is needed for.</param>
        /// <returns>Returns a forum adapter for the site the quest is on.</returns>
        public static IForumAdapter GetAdapter(IQuest quest)
        {
            IForumAdapter adapter = GetExplicitAdapter(quest);

            if (adapter == null)
                adapter = GetImplicitAdapter(quest);

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
            switch (quest.Site)
            {
                case "http://forums.sufficientvelocity.com/":
                case "":
                    // Sufficient Velocity is the default if no site name is given
                    return new SVForumAdapter();
                case "http://forums.spacebattles.com/":
                    return new SpaceBattlesAdapter();
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
        private static IForumAdapter GetImplicitAdapter(IQuest quest)
        {
            throw new NotImplementedException("There is no forum adapter implemented for " + quest.Site);
        }
    }
}
