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
        static IPageProvider pageProvider;
        static IServiceProvider serviceProvider;
        static ForumAdapterFactory forumAdapterFactory;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            pageProvider = serviceProvider.GetRequiredService<IPageProvider>();
            forumAdapterFactory = serviceProvider.GetRequiredService<ForumAdapterFactory>();
        }

        [TestMethod]
        [Ignore]
        public async Task ConvertResourcePosts()
        {
            var resourceContent = await LoadResource.Read("Resources/RenascenceSV.html");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resourceContent);
            var forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);
            var adapter = forumAdapterFactory.CreateForumAdapter(forumType, new Uri("http://example.com"));
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));

            Quest quest = new Quest
            {
                DisplayName = "Convert Quest"
            };

            var posts = adapter.GetPosts(doc, quest);

            StringBuilder sb = new StringBuilder();

            foreach (var post in posts)
            {
                sb.AppendLine(post.Text);
                sb.AppendLine("~!~");
            }

            await LoadResource.Write("Resources/RenascenceSV.txt", sb.ToString());
        }
    }
}
