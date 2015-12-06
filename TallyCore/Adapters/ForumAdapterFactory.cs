using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally.Adapters
{
    public static class ForumAdapterFactory
    {
        #region Public functions
        public async static Task<IForumAdapter2> GetAdapter(IQuest quest) => await GetAdapter(quest, CancellationToken.None);

        public async static Task<IForumAdapter2> GetAdapter(IQuest quest, CancellationToken token)
        {
            if (!Uri.IsWellFormedUriString(quest.ThreadName, UriKind.Absolute))
                throw new InvalidOperationException($"Thread is not valid:\n{quest.ThreadName}\n\nPlease enter a valid URL.");

            Uri uri = new Uri(quest.ThreadName);

            IForumAdapter2 adapter = GetKnownForumAdapter(uri);

            if (adapter == null)
                adapter = await GetUnknownForumAdapter(uri, token);

            return adapter;
        }
        #endregion

        #region Private methods
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

                if (page == null)
                    return null;

                if (token.IsCancellationRequested)
                    return null;

                //if (XenForoAdapter2.CanHandlePage(page))
                //    return new XenForoAdapter2(uri);

                //if (vBulletinAdapter3_2.CanHandlePage(page))
                //    return new vBulletinAdapter3_2(uri);


                const string detectMethodName = "CanHandlePage";

                Type ti = typeof(IForumAdapter2);

                //from asm in AppDomain.CurrentDomain.GetAssemblies()

                var adapterList = from t in Assembly.GetExecutingAssembly().GetTypes()
                                  where (!t.IsInterface && ti.IsAssignableFrom(t))
                                  select t;

                foreach (var adapterClass in adapterList)
                {
                    try
                    {
                        var detect = adapterClass.FindMembers(MemberTypes.Method, BindingFlags.Static | BindingFlags.Public, Type.FilterName, detectMethodName);

                        if (detect.Any())
                        {
                            bool result = (bool)adapterClass.InvokeMember(detectMethodName,
                                BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                null, null, new object[] { page });

                            if (result)
                            {
                                var adapter = (IForumAdapter2)Activator.CreateInstance(adapterClass, new object[] { uri });

                                return adapter;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        { }
                    }
                }

            }
            catch (OperationCanceledException)
            { }
            finally
            {
                webPageProvider?.Dispose();
            }

            return null;
        }
        #endregion
    }
}
