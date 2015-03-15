using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using NetTally.Adapters;

namespace NetTally.Tests
{
    [TestClass]
    public class ForumAdapterFactoryUnitTest
    {
        [TestMethod]
        public void TestDefault()
        {
            IQuest quest = new Quest();
            var adapter = ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(SVForumAdapter));
        }

        [TestMethod]
        public void TestSufficientVelocity()
        {
            IQuest quest = new Quest();
            quest.Site = "http://forums.sufficientvelocity.com/";
            var adapter = ForumAdapterFactory.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(SVForumAdapter));
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void TestSpaceBattles()
        {
            IQuest quest = new Quest();
            quest.Site = "http://forums.spacebattles.com/";
            var adapter = ForumAdapterFactory.GetAdapter(quest);

            //Assert.IsInstanceOfType(adapter, typeof(SBForumAdapter));
        }
    }
}
