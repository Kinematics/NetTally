using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Web;

namespace NetTally.Tests.Input
{
    [TestClass]
    public class WebPageProviderTests
    {
        static IPageProvider pageProvider;
        static IServiceProvider serviceProvider;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            pageProvider = serviceProvider.GetRequiredService<IPageProvider>();
        }


        [TestMethod]
        public void LoadPagesTest()
        {
        }


    }
}