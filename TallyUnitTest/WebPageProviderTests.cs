using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using NetTally.Adapters;

namespace NetTally.Tests
{
    [TestClass]
    public class WebPageProviderTests
    {
        static WebPageProvider pageProvider;
        static PrivateObject privateWeb;


        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            pageProvider = new WebPageProvider();
            privateWeb = new PrivateObject(pageProvider);
        }


        [TestMethod]
        public void ClearPageCacheTest()
        {
            pageProvider.ClearPageCache();
        }

        [TestMethod]
        public void LoadPagesTest()
        {
        }


    }
}