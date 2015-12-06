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
            var adapter = ForumAdapterFactory2.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter2));
        }

        [TestMethod]
        public void TestSufficientVelocity()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://forums.sufficientvelocity.com/";
            var adapter = ForumAdapterFactory2.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter2));
        }

        [TestMethod]
        public void TestSpaceBattles()
        {
            IQuest quest = new Quest();
            quest.ThreadName = "http://forums.spacebattles.com/";
            var adapter = ForumAdapterFactory2.GetAdapter(quest);

            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter2));
        }
    }
}
