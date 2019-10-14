using System;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Forums.Adapters;
using NetTally.Forums.Adapters2;
using NetTally.Web;

namespace NetTally.Tests.Forums
{
    [TestClass]
    [Ignore]
    public class ForumAdapterFactoryTests
    {
        static IPageProvider? pageProvider;
        static IServiceProvider? serviceProvider;
        static ForumAdapterFactory? forumAdapterFactory;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            pageProvider = serviceProvider.GetRequiredService<IPageProvider>();
            forumAdapterFactory = serviceProvider.GetRequiredService<ForumAdapterFactory>();
        }


        [TestMethod]
        public async Task Select_XenForo_SV()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/vote-tally-program.199/";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(XenForo2Adapter2));
        }

        [TestMethod]
        public async Task Select_XenForo_SB()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "https://forums.spacebattles.com/threads/vote-tally-program-v3.260204/";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter2));
        }

        [TestMethod]
        public async Task Select_XenForo_QQ()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "https://forum.questionablequesting.com/threads/qq-vote-tally-program.1065/";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter2));
        }

        [TestMethod]
        public async Task Select_vBulletin3()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://forums.animesuki.com/showthread.php?t=128882";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(VBulletin3Adapter2));
        }

        [TestMethod]
        [Ignore]
        public async Task Select_vBulletin4()
        {
            IQuest quest = new Quest();
            // Fandompost changed to vBulletin 5.  Need to find another vBulletin 4 for testing.
            quest.ThreadName = "http://www.fandompost.com/oldforums/showthread.php?48716-One-Punch-Man-Discussion-Thread/page1";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(VBulletin4Adapter2));
        }

        [TestMethod]
        public async Task Select_vBulletin5()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://www.vbulletin.com/forum/forum/vbulletin-announcements/vbulletin-announcements_aa/4333101-vbulletin-5-1-10-connect-is-now-available";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(VBulletin5Adapter2));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Ignore]
        public async Task Select_NodeBB()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "https://community.nodebb.org/topic/6298/nodebb-v0-7-3";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(NodeBBAdapter));
        }

        [TestMethod]
        [Ignore]
        public async Task Select_phpBB()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://www.ilovephilosophy.com/viewtopic.php?f=1&t=175054";
            var adapter = await forumAdapterFactory.CreateForumAdapterAsync(quest, pageProvider, CancellationToken.None);

            Assert.IsInstanceOfType(adapter, typeof(phpBBAdapter));
        }

        [TestMethod]
        public async Task Select_Explicit()
        {
            Uri uri = new Uri("https://example.com/threads/RenascenceSV.html.100/");
            var resourceContent = await LoadResource.Read("Resources/RenascenceSV.html");
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resourceContent);
            var forumType = ForumIdentifier.IdentifyForumTypeFromHtmlDocument(doc);

            var adapter = forumAdapterFactory.CreateForumAdapter(forumType, uri);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter2));
        }
    }
}
