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
        public async Task TestDefault()
        {
            IQuest quest = new Quest();
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public async Task TestSufficientVelocity()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://forums.sufficientvelocity.com/";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public async Task TestSpaceBattles()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://forums.spacebattles.com/";
            IForumAdapter adapter = await ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }
    }
}
