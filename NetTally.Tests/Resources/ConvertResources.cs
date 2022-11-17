using System;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using NetTally.Forums;
using NetTally.Forums.Adapters;
using NetTally.Tests;
using NetTally.Web;

namespace NTTests.Resources
{
    [TestClass]
    public class ConvertResources
    {
        static IPageProvider pageProvider = null!;
        static IServiceProvider serviceProvider = null!;
        static ForumAdapterFactory forumAdapterFactory = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            pageProvider = serviceProvider.GetRequiredService<IPageProvider>();
            forumAdapterFactory = serviceProvider.GetRequiredService<ForumAdapterFactory>();
        }

        public void ConvertResourcePosts()
        {
        }
    }
}
