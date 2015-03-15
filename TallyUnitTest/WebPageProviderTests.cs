using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using NetTally.Adapters;

namespace NetTally.Tests
{
    [TestClass()]
    public class WebPageProviderTests
    {
        static WebPageProvider pageProvider;
        static PrivateObject privateWeb;
        static IForumAdapter forumAdapter;


        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            forumAdapter = new SufficientVelocityAdapter();
            pageProvider = new WebPageProvider();
            privateWeb = new PrivateObject(pageProvider);
        }


        [TestMethod()]
        public void ClearPageCacheTest()
        {
            pageProvider.ClearPageCache();

            Dictionary<string, CachedPage> accessPageCache = (Dictionary<string, CachedPage>)privateWeb.GetField("pageCache");
            Assert.IsTrue(accessPageCache.Count == 0);

            Dictionary<string, int> accessLoadedPages = (Dictionary<string, int>)privateWeb.GetField("lastPageLoadedFor");
            Assert.IsTrue(accessLoadedPages.Count == 0);
        }

        [TestMethod()]
        public void LoadPagesTest()
        {
        }


    }
}