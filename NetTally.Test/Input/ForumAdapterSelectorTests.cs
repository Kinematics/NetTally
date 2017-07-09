using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Forums.Adapters;

namespace NetTally.Tests
{
    [TestClass]
    public class ForumAdapterSelectorTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Select_XenForo_NoUri()
        {
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.XenForo1, null);
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Select_XenForo_BaseUri()
        {
            Uri uri = new Uri("http://forums.sufficientvelocity.com/");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.XenForo1, uri);
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public void Select_XenForo_ThreadUri()
        {
            Uri uri = new Uri("http://forums.sufficientvelocity.com/threads/vote-tally-program.199/");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.XenForo1, uri);
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }


        [TestMethod]
        public void Select_XenForo_DefaultUri()
        {
            IQuest quest = new Quest();
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.XenForo1, quest.ThreadUri);
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public void Select_SpaceBattles()
        {
            Uri uri = new Uri("https://forums.spacebattles.com/threads/vote-tally-program-v3.260204/");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.XenForo1, uri);
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public void Select_QuestionableQuesting()
        {
            Uri uri = new Uri("https://forum.questionablequesting.com/threads/qq-vote-tally-program.1065/");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.XenForo1, uri);
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }

        [TestMethod]
        public void Select_vBulletin3()
        {
            Uri uri = new Uri("http://forums.animesuki.com/showthread.php?t=128882");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.vBulletin3, uri);
            Assert.IsInstanceOfType(adapter, typeof(vBulletinAdapter3));
        }

        [TestMethod]
        public void Select_vBulletin4()
        {
            Uri uri = new Uri("http://www.fandompost.com/oldforums/showthread.php?48716-One-Punch-Man-Discussion-Thread/page1");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.vBulletin4, uri);
            Assert.IsInstanceOfType(adapter, typeof(vBulletinAdapter4));
        }

        [TestMethod]
        public void Select_vBulletin5()
        {
            Uri uri = new Uri("http://www.vbulletin.com/forum/forum/vbulletin-announcements/vbulletin-announcements_aa/4333101-vbulletin-5-1-10-connect-is-now-available");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.vBulletin5, uri);
            Assert.IsInstanceOfType(adapter, typeof(vBulletinAdapter5));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Select_NodeBB()
        {
            Uri uri = new Uri("https://community.nodebb.org/topic/6298/nodebb-v0-7-3");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.NodeBB, uri);
            Assert.IsInstanceOfType(adapter, typeof(NodeBBAdapter));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Select_phpBB()
        {
            Uri uri = new Uri("http://www.ilovephilosophy.com/viewtopic.php?f=1&t=175054");
            var adapter = ForumAdapterSelector.GetForumAdapter(ForumType.phpBB, uri);
            Assert.IsInstanceOfType(adapter, typeof(phpBBAdapter));
        }
        
    }
}
