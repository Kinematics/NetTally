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
            forumAdapter = new XenForoAdapter();
            pageProvider = new WebPageProvider();
            privateWeb = new PrivateObject(pageProvider);
        }


        [TestMethod()]
        public void ClearPageCacheTest()
        {
            pageProvider.ClearPageCache();
        }

        [TestMethod()]
        public void LoadPagesTest()
        {
        }


    }
}