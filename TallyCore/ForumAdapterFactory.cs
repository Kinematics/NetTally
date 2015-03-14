using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTally
{
    public static class ForumAdapterFactory
    {
        public static IForumAdapter GetAdapter(IQuest quest)
        {
            IForumAdapter adapter = GetExplicitAdapter(quest);

            if (adapter == null)
                adapter = GetImplicitAdapter(quest);

            return adapter;
        }

        private static IForumAdapter GetExplicitAdapter(IQuest quest)
        {
            switch (quest.Site)
            {
                case "http://forums.sufficientvelocity.com/":
                case "":
                case null:
                    // Sufficient Velocity is the default if no site name is given
                    return new SVForumAdapter();
                default:
                    return null;
            }
        }

        private static IForumAdapter GetImplicitAdapter(IQuest quest)
        {
            throw new NotImplementedException("There is no forum adapter implemented for " + quest.Site);
        }
    }
}
