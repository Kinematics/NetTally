using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Web;

namespace NTTests.Input
{
    [TestClass]
    public class WebPageProviderTests
    {
        static IPageProvider pageProvider;
#if !NETCOREAPP
        static PrivateObject privateWeb;
#endif

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            pageProvider = PageProviderBuilder.Instance.Build();
#if !NETCOREAPP
            privateWeb = new PrivateObject(pageProvider);
#endif
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