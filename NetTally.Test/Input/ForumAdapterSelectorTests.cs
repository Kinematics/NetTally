using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Forums.Adapters;
using NetTally.ViewModels;

namespace NetTally.Tests
{
    [TestClass]
    public class ForumAdapterSelectorTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ViewModelService.Instance.Build();
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Select_XenForo_NoUri()
        {
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(null, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task Select_XenForo_BaseUri()
        {
            Uri uri = new Uri("http://forums.sufficientvelocity.com/");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));
        }

        [TestMethod]
        public async Task Select_XenForo_ThreadUri()
        {
            Uri uri = new Uri("http://forums.sufficientvelocity.com/threads/vote-tally-program.199/");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));
        }


        [TestMethod]
        public async Task Select_XenForo_DefaultUri()
        {
            IQuest quest = new Quest();
            Uri uri = quest.ThreadUri;
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));
        }

        [TestMethod]
        public async Task Select_SpaceBattles()
        {
            Uri uri = new Uri("https://forums.spacebattles.com/threads/vote-tally-program-v3.260204/");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));
        }

        [TestMethod]
        public async Task Select_QuestionableQuesting()
        {
            Uri uri = new Uri("https://forum.questionablequesting.com/threads/qq-vote-tally-program.1065/");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(XenForo1Adapter));
        }

        [TestMethod]
        [Ignore]
        public async Task Select_vBulletin3()
        {
            Uri uri = new Uri("http://forums.animesuki.com/showthread.php?t=128882");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(vBulletin3Adapter));
        }

        [TestMethod]
        [Ignore]
        public async Task Select_vBulletin4()
        {
            Uri uri = new Uri("http://www.fandompost.com/oldforums/showthread.php?48716-One-Punch-Man-Discussion-Thread/page1");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(vBulletin4Adapter));
        }

        [TestMethod]
        [Ignore]
        public async Task Select_vBulletin5()
        {
            Uri uri = new Uri("http://www.vbulletin.com/forum/forum/vbulletin-announcements/vbulletin-announcements_aa/4333101-vbulletin-5-1-10-connect-is-now-available");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(vBulletin5Adapter));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Ignore]
        public async Task Select_NodeBB()
        {
            Uri uri = new Uri("https://community.nodebb.org/topic/6298/nodebb-v0-7-3");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(NodeBBAdapter));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Ignore]
        public async Task Select_phpBB()
        {
            Uri uri = new Uri("http://www.ilovephilosophy.com/viewtopic.php?f=1&t=175054");
            var adapter = await ForumAdapterSelector.GetForumAdapterAsync(uri, CancellationToken.None);
            Assert.IsInstanceOfType(adapter, typeof(phpBBAdapter));
        }
        
    }
}
