using System;
using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using NetTally.Adapters;

namespace TallyUnitTest
{
    [TestClass]
    public class UnitTestQuest
    {
// Disable obsolete warnings for fields that we're still testing
#pragma warning disable 0618

        static List<string> propertiesRaised = new List<string>();
        Quest a;

        [TestInitialize()]
        public void Initialize()
        {
            a = new Quest();
        }


        #region Constructor
        [TestMethod]
        public void TestDefaultObject()
        {
            // Obsolete
            Assert.AreEqual(Quest.NewEntryName, a.Name);
            Assert.AreEqual("", a.Site);

            // Normal
            Assert.AreEqual(Quest.NewThreadEntry, a.ThreadName);
            Assert.AreEqual("fake-thread", a.DisplayName);
            Assert.AreEqual("http://forums.sufficientvelocity.com/", a.SiteName);
            Assert.AreEqual(0, a.RawPostsPerPage);
            Assert.AreEqual(25, a.PostsPerPage);
            Assert.AreEqual(1, a.StartPost);
            Assert.AreEqual(0, a.EndPost);
            Assert.AreEqual(true, a.ReadToEndOfThread);
            Assert.AreEqual(false, a.UseVotePartitions);
            Assert.AreEqual(true, a.PartitionByLine);
            Assert.AreEqual(false, a.CheckForLastThreadmark);
            Assert.AreEqual(0, a.ThreadmarkPost);
            Assert.AreEqual(1, a.FirstTallyPost);

            Assert.IsInstanceOfType(a.GetForumAdapter(), typeof(XenForoAdapter));
            Assert.AreEqual(Quest.NewThreadEntry, a.ToString());
        }

        [TestMethod]
        public void TestToString()
        {
            a.DisplayName = "Test Display";
            Assert.AreEqual("Test Display", a.ToString());
            // If display name is empty, fall back to the thread name
            a.DisplayName = "";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/fake-thread", a.ToString());
        }
        #endregion

        #region Display Name and Thread Name
        [TestMethod]
        public void TestDisplayName()
        {
            Assert.AreEqual("fake-thread", a.DisplayName);
            a.DisplayName = "testing-thread";
            Assert.AreEqual("testing-thread", a.DisplayName);
            a.DisplayName = "";
            Assert.AreEqual("fake-thread", a.DisplayName);
            a.ThreadName = "http://forums.sufficientvelocity.com/";
            Assert.AreEqual("forums.sufficientvelocity.com", a.DisplayName);
            a.DisplayName = "/";
            Assert.AreEqual("/", a.DisplayName);

            // Check that unicode control and formatting characters are removed
            a.DisplayName = "MonkeyPhone";
            Assert.AreEqual("MonkeyPhone", a.DisplayName);
            a.DisplayName = "Monkey\u200bPhone";
            Assert.AreEqual("MonkeyPhone", a.DisplayName);

        }

        public void TestCleanURL()
        {
            // Largely tested via ThreadName and SiteName
        }

        [TestMethod]
        public void TestThreadName()
        {
            a.ThreadName = "http://forums.sufficientvelocity.com/";
            Assert.AreEqual("http://forums.sufficientvelocity.com/", a.ThreadName);
            a.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", a.ThreadName);
            a.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/page-221";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", a.ThreadName);
            a.ThreadName = "http://www.fandompost.com/oldforums/showthread.php?39239-Yurikuma-Arashi-Discussion-Thread&p=288335#post288335";
            Assert.AreEqual("http://www.fandompost.com/oldforums/showthread.php?39239-Yurikuma-Arashi-Discussion-Thread", a.ThreadName);
        }

        [TestMethod]
        public void TestSiteName()
        {
            Assert.AreEqual("http://forums.sufficientvelocity.com/", a.SiteName);
            a.ThreadName = "http://www.fandompost.com/oldforums/showthread.php?39239-Yurikuma-Arashi-Discussion-Thread&p=288335#poast288335";
            Assert.AreEqual("http://www.fandompost.com/", a.SiteName);
            a.ThreadName = "renascence-a-homura-quest.10402";
            Assert.AreEqual("http://forums.sufficientvelocity.com/", a.SiteName);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullThreadName()
        {
            a.ThreadName = null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestEmptyThreadName()
        {
            a.ThreadName = "";
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullDisplayName()
        {
            a.DisplayName = null;
        }

        #endregion

        #region Name and Site (Obsolete)
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetNameToNull()
        {
            a.Name = null;
        }

        [TestMethod]
        public void TestSetName()
        {
            string testName = "Sample.Name";

            a.Name = testName;

            Assert.AreEqual(testName, a.Name);
        }

        [TestMethod]
        public void TestCleanName()
        {
            string testName = "awake-already-homura-nge-pmmm-fusion-quest.11111";
            //string expectedName = "awake-already-homura-nge-pmmm-fusion-quest.11111";

            a.Name = testName;
            Assert.AreEqual(testName, a.Name);

            testName = "awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(testName, a.Name);

            testName = "http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111";

            a.Name = testName;
            Assert.AreEqual(testName, a.Name);

            testName = "http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(testName, a.Name);

            // This will fail the regex filter, so should just give back the original.
            testName = "/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(testName, a.Name);
        }

        [TestMethod]
        public void TestSetSiteToNull()
        {
            a.Site = null;

            Assert.AreEqual(string.Empty, a.Site);
        }


        [TestMethod]
        public void TestSetSiteToEmpty()
        {
            a.Site = string.Empty;

            Assert.AreEqual(string.Empty, a.Site);
        }

        [TestMethod]
        public void TestSetSite()
        {
            string testName = "http://forums.sufficientvelocity.com/";

            a.Site = testName;

            Assert.AreEqual(testName, a.Site);
        }
        #endregion

        #region StartPost
        [TestMethod]
        public void TestSetStart()
        {
            int testPost = 448;

            a.StartPost = testPost;

            Assert.AreEqual(testPost, a.StartPost);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetStartZero()
        {
            int testPost = 0;

            a.StartPost = testPost;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetStartNegative()
        {
            int testPost = -20;

            a.StartPost = testPost;
        }
        #endregion

        #region EndPost
        [TestMethod]
        public void TestSetEnd()
        {
            int testPost = 448;

            a.EndPost = testPost;

            Assert.AreEqual(testPost, a.EndPost);
        }

        [TestMethod]
        public void TestSetEndZero()
        {
            int testPost = 0;

            a.EndPost = testPost;

            Assert.AreEqual(testPost, a.EndPost);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetEndNegative()
        {
            int testPost = -20;

            a.EndPost = testPost;
        }
        #endregion

        #region Flags
        [TestMethod]
        public void TestReadToEndOfThread()
        {
            a.EndPost = 1;
            Assert.AreEqual(false, a.ReadToEndOfThread);

            a.EndPost = 1000000;
            Assert.AreEqual(false, a.ReadToEndOfThread);

            a.EndPost = 0;
            Assert.AreEqual(true, a.ReadToEndOfThread);

            a.EndPost = Int32.MaxValue;
            Assert.AreEqual(false, a.ReadToEndOfThread);

            a.ThreadmarkPost = 517;
            Assert.AreEqual(true, a.ReadToEndOfThread);
        }

        [TestMethod]
        public void TestUseVotePartitions()
        {
            a.UseVotePartitions = true;
            Assert.AreEqual(true, a.UseVotePartitions);
        }

        [TestMethod]
        public void TestPartitionByLine()
        {
            a.PartitionByLine = false;
            Assert.AreEqual(false, a.PartitionByLine);
        }

        [TestMethod]
        public void TestCheckForLastThreadmark()
        {
            a.CheckForLastThreadmark = true;
            Assert.AreEqual(true, a.CheckForLastThreadmark);
        }
        #endregion

        #region Other Properties
        [TestMethod]
        public void TestFirstTallyPost()
        {
            a.StartPost = 448;

            Assert.AreEqual(448, a.FirstTallyPost);

            a.CheckForLastThreadmark = true;

            Assert.AreEqual(448, a.FirstTallyPost);

            a.ThreadmarkPost = 517;

            Assert.AreEqual(517, a.FirstTallyPost);

            a.CheckForLastThreadmark = false;

            Assert.AreEqual(448, a.FirstTallyPost);
        }

        #endregion

        #region ForumAdapters
        [TestMethod]
        public void TestForumAdapters()
        {
            var adapter = a.GetForumAdapter();
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));

            a.ThreadName = "http://forums.sufficientvelocity.com/";
            var adapterTask = a.GetForumAdapterAsync(System.Threading.CancellationToken.None);
            adapter = adapterTask.Result;
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));

            a.Site = "http://forums.spacebattles.com/";
            adapterTask = a.GetForumAdapterAsync(System.Threading.CancellationToken.None);
            adapter = adapterTask.Result;
            Assert.IsInstanceOfType(adapter, typeof(XenForoAdapter));
        }
        #endregion

        #region Events
        [TestMethod]
        public void TestEventRaising()
        {
            a.PropertyChanged += A_PropertyChanged;

            propertiesRaised.Clear();

            a.Name = "awake-already-homura-nge-pmmm-fusion-quest.11111";
            Assert.IsTrue(propertiesRaised.Contains("Name"));

            propertiesRaised.Clear();

            a.StartPost = 10;
            Assert.IsTrue(propertiesRaised.Contains("StartPost"));

            propertiesRaised.Clear();

            a.EndPost = 20;
            Assert.IsTrue(propertiesRaised.Contains("EndPost"));

            propertiesRaised.Clear();

            a.DisplayName = "Display";
            Assert.IsTrue(propertiesRaised.Contains("DisplayName"));

            propertiesRaised.Clear();

            a.ThreadName = "http://www.example.com";
            Assert.IsTrue(propertiesRaised.Contains("ThreadName"));

            propertiesRaised.Clear();

            a.UseVotePartitions = true;
            Assert.IsTrue(propertiesRaised.Contains("UseVotePartitions"));

            propertiesRaised.Clear();

            a.PartitionByLine = false;
            Assert.IsTrue(propertiesRaised.Contains("PartitionByLine"));

            propertiesRaised.Clear();

            a.CheckForLastThreadmark = true;
            Assert.IsTrue(propertiesRaised.Contains("CheckForLastThreadmark"));
        }

        private void A_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            propertiesRaised.Add(e.PropertyName);
        }
        #endregion

#pragma warning restore 0618
    }
}
