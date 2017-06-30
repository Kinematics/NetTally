using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Web;

namespace NetTally.Tests
{
    [TestClass]
    public class WebPageProviderTests
    {
        static IPageProvider pageProvider;
        static PrivateObject privateWeb;


        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            pageProvider = PageProviderBuilder.Instance.Build();
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