using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using NetTally.Adapters;

namespace NetTally.Tests
{
    [TestClass]
    public class ForumAdapterFactoryUnitTest
    {
        [TestMethod]
        public async Task DefaultQuest()
        {
            IQuest quest = new Quest();
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task SufficientVelocityInvalidThread()
        {
            IQuest quest = new Quest();
            // Invalid thread name
            quest.ThreadName = "http://forums.sufficientvelocity.com/";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public async Task SufficientVelocity()
        {
            IQuest quest = new Quest();
            // Invalid thread name
            quest.ThreadName = "https://forums.sufficientvelocity.com/threads/vote-tally-program.199/page-19#post-4889303";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public async Task SpaceBattles()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "https://forums.spacebattles.com/threads/vote-tally-program-v3.260204/";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public async Task QuestionableQuesting()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "https://forum.questionablequesting.com/threads/qq-vote-tally-program.1065/";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public async Task vBulletin3()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://forums.animesuki.com/showthread.php?t=128882";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(vBulletinAdapter3));
        }

        [TestMethod]
        public async Task vBulletin4()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://www.fandompost.com/oldforums/showthread.php?48716-One-Punch-Man-Discussion-Thread/page1";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(vBulletinAdapter4));
        }

        [TestMethod]
        public async Task vBulletin5()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://www.vbulletin.com/forum/forum/vbulletin-announcements/vbulletin-announcements_aa/4333101-vbulletin-5-1-10-connect-is-now-available";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(vBulletinAdapter5));
        }

        [TestMethod]
        public async Task NodeBB()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "https://community.nodebb.org/topic/6298/nodebb-v0-7-3";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsNull(adapter);
            //Assert.IsInstanceOfType(adapter, typeof(NodeBBAdapter));
        }

        [TestMethod]
        public async Task phpBB()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://www.ilovephilosophy.com/viewtopic.php?f=1&t=175054";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsNull(adapter);
            //Assert.IsInstanceOfType(adapter, typeof(phpBBAdapter));
        }

    }
}
