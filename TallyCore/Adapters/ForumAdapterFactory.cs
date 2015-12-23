using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Web;

namespace NetTally.Adapters
{
    public static class ForumAdapterFactory
    {
        #region Public functions
        /// <summary>
        /// Overload to get an adapter for a quest without specifying a cancellation token.
        /// </summary>
        /// <param name="quest">The quest to get the adapter for.</param>
        /// <returns>Returns a forum adapter for the quest.</returns>
        /// <exception cref="ArgumentNullException">If quest is null.</exception>
        /// <exception cref="InvalidOperationException">If the quest's thread is not a proper URL.</exception>
        public async static Task<IForumAdapter> GetAdapter(IQuest quest) => await GetAdapter(quest, CancellationToken.None, null);

        /// <summary>
        /// Overload to get an adapter for a quest without specifying a cancellation token.
        /// </summary>
        /// <param name="quest">The quest to get the adapter for.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Returns a forum adapter for the quest.</returns>
        /// <exception cref="ArgumentNullException">If quest is null.</exception>
        /// <exception cref="InvalidOperationException">If the quest's thread is not a proper URL.</exception>
        public async static Task<IForumAdapter> GetAdapter(IQuest quest, CancellationToken token) => await GetAdapter(quest, token, null);

        /// <summary>
        /// Function to generate an appropriate forum adapter for the specified quest.
        /// Generates an exception if the quest's thread URL is not well-formed.
        /// </summary>
        /// <param name="quest">The quest to get the adapter for.  Cannot be null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="pageProvider">The page provider to use if we need to query the web.  If it's null, will create a new one.</param>
        /// <returns>Returns an appropriate forum adapter for the quest, if found. Otherwise, null.</returns>
        /// <exception cref="ArgumentNullException">If quest is null.</exception>
        /// <exception cref="InvalidOperationException">If the quest's thread is not a proper URL.</exception>
        public async static Task<IForumAdapter> GetAdapter(IQuest quest, CancellationToken token, IPageProvider pageProvider)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (!Uri.IsWellFormedUriString(quest.ThreadName, UriKind.Absolute))
                throw new InvalidOperationException($"Thread URL is not valid:\n{quest.ThreadName}\n\nPlease enter a valid URL.");

            Uri uri = new Uri(quest.ThreadName);

            IForumAdapter adapter = GetKnownForumAdapter(uri);

            if (adapter == null)
                adapter = await GetUnknownForumAdapter(uri, token, pageProvider);

            return adapter;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Generate a forum adapter object for certain known forums,
        /// without needing to check the contents of the forum page.
        /// </summary>
        /// <param name="uri">The URI for the forum thread.</param>
        /// <returns>Returns a forum adapter for certain known forums. Otherwise, null.</returns>
        private static IForumAdapter GetKnownForumAdapter(Uri uri)
        {
            switch (uri.Host)
            {
                // Known XenForo sites
                case "forums.sufficientvelocity.com":
                case "forums.spacebattles.com":
                case "forum.questionablequesting.com":
                    return new XenForoAdapter(uri);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Generate a forum adapter for any unknown forum.
        /// Need to load a forum page (asynchronously) to determine which forum adapter is needed.
        /// </summary>
        /// <param name="uri">The URI of the forum thread to check.  Cannot be null.  Must be absolute.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="pageProvider">The page provider to use to load pages.  If it's null, will create a new one.</param>
        /// <returns>Returns a forum adapter for the forum thread if one can be determined.
        /// Otherwise, null.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="uri"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="uri"/> is not an absolute uri.</exception>
        private async static Task<IForumAdapter> GetUnknownForumAdapter(Uri uri, CancellationToken token, IPageProvider pageProvider)
        {
            bool localPageProvider = pageProvider == null;

            if (localPageProvider)
                pageProvider = new WebPageProvider2();

            try
            {
                var page = await pageProvider.GetPage(uri.AbsoluteUri, uri.Host, Caching.UseCache, token);

                if (page == null)
                    return null;

                if (token.IsCancellationRequested)
                    return null;

                // Do automatic checks against any class in this library that implements
                // IForumAdapter, and the static method, "CanHandlePage".
                const string detectMethodName = "CanHandlePage";

                Type ti = typeof(IForumAdapter);

                // Get the list of all adapter classes built off of the IForumAdapter interface.
                var adapterList = from t in Assembly.GetExecutingAssembly().GetTypes()
                                    where (!t.IsInterface && ti.IsAssignableFrom(t))
                                    select t;

                foreach (var adapterClass in adapterList)
                {
                    try
                    {
                        // Check the class for a static member named "CanHandlePage"
                        var detectMethodArray = adapterClass.FindMembers(MemberTypes.Method, BindingFlags.Static | BindingFlags.Public,
                            Type.FilterName, detectMethodName);

                        // If it finds any such methods, check the class against the currently loaded page to
                        // see if this the correct adapter to return.
                        if (detectMethodArray.Any())
                        {
                            bool result = (bool)adapterClass.InvokeMember(detectMethodName,
                                BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
                                null, null, new object[] { page });

                            if (result)
                            {
                                var adapter = (IForumAdapter)Activator.CreateInstance(adapterClass, new object[] { uri });

                                return adapter;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorLog.Log(e);
                    }
                }
            }
            finally
            {
                if (localPageProvider)
                    pageProvider?.Dispose();
            }

            // If nothing was found, return null.
            return null;
        }
        #endregion
    }
}
