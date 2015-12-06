using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally.Adapters
{
    public static class ForumAdapterFactory2
    {
        public async static Task<IForumAdapter2> GetAdapter(IQuest quest) => await GetAdapter(quest, CancellationToken.None);

        public async static Task<IForumAdapter2> GetAdapter(IQuest quest, CancellationToken token)
        {
            Uri uri = new Uri(quest.ThreadName);

            IForumAdapter2 fa = GetKnownForumAdapter(uri);

            if (fa == null)
                fa = await GetUnknownForumAdapter(uri, token);

            return fa;
        }

        private static IForumAdapter2 GetKnownForumAdapter(Uri uri)
        {
            switch (uri.Host)
            {
                // Known XenForo sites
                case "forums.sufficientvelocity.com":
                case "forums.spacebattles.com":
                case "forum.questionablequesting.com":
                    return new XenForoAdapter2(uri);
                default:
                    return null;
            }
        }

        private async static Task<IForumAdapter2> GetUnknownForumAdapter(Uri uri, CancellationToken token)
        {
            IPageProvider webPageProvider = null;

            try
            {
                webPageProvider = new WebPageProvider();

                var page = await webPageProvider.GetPage(uri.AbsoluteUri, uri.Host, Caching.UseCache, token);

                if (XenForoAdapter2.CanHandlePage(page))
                    return new XenForoAdapter2(uri);
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                webPageProvider?.Dispose();
            }

            return null;
        }
    }
}
