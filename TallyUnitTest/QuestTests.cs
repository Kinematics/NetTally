using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetTally.Tests
{
    /// <summary>
    /// Class that tests the implementation of the Quest class against the IQuest interface.
    /// </summary>
    [TestClass]
    public class QuestTest : IQuestTestBase
    {
        [TestInitialize]
        public void Setup()
        {
            quest = new Quest();
            Init();
        }
    }

    /// <summary>
    /// Class that tests a given IQuest object.
    /// </summary>
    [TestClass]
    public abstract class IQuestTestBase
    {
        #region Local vars, setup, and teardown
        protected IQuest quest;
        bool notified = false;
        readonly List<string> propertiesRaised = new List<string>();

        /// <summary>
        /// General initialization for the test, in addition to whatever the
        /// implmentation class does.
        /// </summary>
        public void Init()
        {
            quest.PropertyChanged += IQuest_PropertyChanged;
        }

        /// <summary>
        /// Cleanup per test.
        /// </summary>
        [TestCleanup]
        void Reset()
        {
            notified = false;
            propertiesRaised.Clear();
            quest.PropertyChanged -= IQuest_PropertyChanged;
        }
        #endregion

        #region Stuff for handling checking event notification
        void IQuest_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            notified = true;
            propertiesRaised.Add(e.PropertyName);
        }

        void VerifyNoNotification()
        {
            Assert.IsFalse(notified);
            Assert.AreEqual(0, propertiesRaised.Count);
        }

        void VerifyNotification(string property)
        {
            Assert.IsTrue(notified);
            Assert.IsTrue(propertiesRaised.Contains(property));
        }

        void VerifyNotification(List<string> properties)
        {
            Assert.IsTrue(notified);
            CollectionAssert.IsSubsetOf(properties, propertiesRaised);
        }
        #endregion

        #region Basic Construction
        [TestMethod]
        public void IQuest_Construction_State()
        {
            Assert.AreEqual(Quest.NewThreadEntry, quest.ThreadName);
            Assert.AreEqual("fake-thread.00000", quest.DisplayName);
            Assert.AreEqual(0, quest.PostsPerPage);
            Assert.AreEqual(1, quest.StartPost);
            Assert.AreEqual(0, quest.EndPost);
            Assert.AreEqual(true, quest.ReadToEndOfThread);
            Assert.AreEqual(false, quest.CheckForLastThreadmark);
            Assert.AreEqual(false, quest.AllowRankedVotes);
            Assert.AreEqual(0, quest.ThreadmarkPost);
            Assert.AreEqual(PartitionMode.None, quest.PartitionMode);
            Assert.IsNull(quest.ForumAdapter);

            Assert.AreEqual(quest.DisplayName, quest.ToString());
        }
        #endregion

        #region Thread Name
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IQuest_ThreadName_Invalid_Null()
        {
            quest.ThreadName = null;
        }

        [TestMethod]
        public void IQuest_ThreadName_Invalid_Null_NoChange()
        {
            try
            {
                quest.ThreadName = null;
                Assert.Fail("An exception should have been thrown.");
            }
            catch (ArgumentNullException)
            {

            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception caught:\n" + e.Message);
            }

            Assert.AreEqual(Quest.NewThreadEntry, quest.ThreadName);
            VerifyNoNotification();
        }

        [TestMethod]
        public void IQuest_ThreadName_Invalid_Bad_NoChange()
        {
            try
            {
                quest.ThreadName = "/forums.sufficientvelocity.com/";
                Assert.Fail("An exception should have been thrown.");
            }
            catch (ArgumentException)
            {

            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception caught:\n" + e.Message);
            }

            Assert.AreEqual(Quest.NewThreadEntry, quest.ThreadName);
            VerifyNoNotification();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IQuest_ThreadName_Invalid_Blank()
        {
            quest.ThreadName = "";
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IQuest_ThreadName_Invalid_Empty()
        {
            quest.ThreadName = "  ";
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IQuest_ThreadName_Invalid_Host()
        {
            quest.ThreadName = "/forums.sufficientvelocity.com/";
        }

        [TestMethod]
        public void IQuest_ThreadName_ValidHost()
        {
            quest.ThreadName = "http://forums.sufficientvelocity.com/";
            Assert.AreEqual("http://forums.sufficientvelocity.com/", quest.ThreadName);
            VerifyNotification("ThreadName");
        }

        [TestMethod]
        public void IQuest_ThreadName_WithThread()
        {
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", quest.ThreadName);
            VerifyNotification("ThreadName");
        }

        [TestMethod]
        public void IQuest_ThreadName_WithPage()
        {
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/page-221";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", quest.ThreadName);
            VerifyNotification("ThreadName");
        }

        [TestMethod]
        public void IQuest_ThreadName_WithPost()
        {
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/page-221#post-19942121";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", quest.ThreadName);
            VerifyNotification("ThreadName");
            quest.ThreadName = "http://www.fandompost.com/oldforums/showthread.php?39239-Yurikuma-Arashi-Discussion-Thread&p=288335#post288335";
            Assert.AreEqual("http://www.fandompost.com/oldforums/showthread.php?39239-Yurikuma-Arashi-Discussion-Thread", quest.ThreadName);
        }

        [TestMethod]
        public void IQuest_ThreadName_RemoveInvalidUnicode()
        {
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-\u200bhomura-quest.10402/page-221#post-19942121";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", quest.ThreadName);
        }

        #endregion

        #region Display Name
        [TestMethod]
        public void IQuest_DisplayName_Null()
        {
            quest.DisplayName = null;
            Assert.AreEqual(quest.ThreadTitle, quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Blank()
        {
            quest.DisplayName = "";
            Assert.AreEqual(quest.ThreadTitle, quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Empty()
        {
            quest.DisplayName = "   ";
            Assert.AreEqual(quest.ThreadTitle, quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Normal()
        {
            quest.DisplayName = "My Quest";
            Assert.AreEqual("My Quest", quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Normal_CleanUnicode()
        {
            quest.DisplayName = "My\u200bQuest";
            Assert.AreEqual("MyQuest", quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Normal_Trim()
        {
            quest.DisplayName = " My Quest  ";
            Assert.AreEqual("My Quest", quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_ResetNull()
        {
            quest.DisplayName = "My Quest";
            quest.DisplayName = null;
            Assert.AreEqual(quest.ThreadTitle, quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_ResetEmpty()
        {
            quest.DisplayName = "My Quest";
            quest.DisplayName = null;
            Assert.AreEqual(quest.ThreadTitle, quest.DisplayName);
            VerifyNotification("DisplayName");
        }
        #endregion

        #region Start and End Post Numbers
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IQuest_StartPost_Zero()
        {
            quest.StartPost = 0;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IQuest_StartPost_Negative()
        {
            quest.StartPost = -1;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IQuest_StartPost_Min()
        {
            quest.StartPost = int.MinValue;
        }

        [TestMethod]
        public void IQuest_StartPost_One()
        {
            quest.StartPost = 1;
            VerifyNotification("StartPost");
        }

        [TestMethod]
        public void IQuest_StartPost_Positive()
        {
            quest.StartPost = 45000;
            VerifyNotification("StartPost");
        }

        [TestMethod]
        public void IQuest_StartPost_Max()
        {
            quest.StartPost = int.MaxValue;
            VerifyNotification("StartPost");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IQuest_EndPost_Negative()
        {
            quest.EndPost = -1;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IQuest_EndPost_Min()
        {
            quest.EndPost = int.MinValue;
        }

        [TestMethod]
        public void IQuest_EndPost_Zero()
        {
            quest.EndPost = 0;
            VerifyNotification("EndPost");
        }

        [TestMethod]
        public void IQuest_EndPost_One()
        {
            quest.EndPost = 1;
            VerifyNotification("EndPost");
        }

        [TestMethod]
        public void IQuest_EndPost_Positive()
        {
            quest.EndPost = 45000;
            VerifyNotification("EndPost");
        }

        [TestMethod]
        public void IQuest_EndPost_Max()
        {
            quest.EndPost = int.MaxValue;
            VerifyNotification("EndPost");
        }
        #endregion

        #region Various property notification checks
        [TestMethod]
        public void IQuest_PostsPerPage_Notify()
        {
            quest.PostsPerPage = 25;
            VerifyNotification("PostsPerPage");
        }

        [TestMethod]
        public void IQuest_CheckForLastThreadmark_Notify()
        {
            quest.CheckForLastThreadmark = true;
            VerifyNotification("CheckForLastThreadmark");
        }

        [TestMethod]
        public void IQuest_PartitionMode_Notify()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            VerifyNotification("PartitionMode");
        }

        [TestMethod]
        public void IQuest_AllowRankedVotes_Notify()
        {
            quest.AllowRankedVotes = true;
            VerifyNotification("AllowRankedVotes");
        }
        #endregion

        #region Read to End of Thread
        [TestMethod]
        public void IQuest_ReadToEndOfThread_NoThreadmarks_ZeroEnd()
        {
            quest.EndPost = 0;
            Assert.IsTrue(quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_NoThreadmarks_PosEnd()
        {
            quest.EndPost = 100;
            Assert.IsFalse(quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_Threadmarks_ZeroEnd()
        {
            quest.EndPost = 0;
            quest.CheckForLastThreadmark = true;
            Assert.IsTrue(quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_Threadmarks_PosEnd()
        {
            quest.EndPost = 100;
            quest.CheckForLastThreadmark = true;
            Assert.IsFalse(quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_Threadmarks_FoundThreadmark()
        {
            quest.EndPost = 100;
            quest.CheckForLastThreadmark = true;
            quest.ThreadmarkPost = 448100;
            Assert.IsTrue(quest.ReadToEndOfThread);
        }
        #endregion

        #region Forum Adapter
        [TestMethod]
        public async Task IQuest_InitForumAdapter()
        {
            Assert.IsNull(quest.ForumAdapter);
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            await quest.InitForumAdapter();
            Assert.IsNotNull(quest.ForumAdapter);
        }

        [TestMethod]
        public async Task IQuest_InitForumAdapter_ReInit()
        {
            Assert.IsNull(quest.ForumAdapter);
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            await quest.InitForumAdapter();
            Assert.IsNotNull(quest.ForumAdapter);
            var fa = quest.ForumAdapter;
            await quest.InitForumAdapter();
            Assert.ReferenceEquals(fa, quest.ForumAdapter);
        }

        [TestMethod]
        public async Task IQuest_InitForumAdapter_Change_SameHost()
        {
            Assert.IsNull(quest.ForumAdapter);
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            await quest.InitForumAdapter();
            Assert.IsNotNull(quest.ForumAdapter);
            var fa = quest.ForumAdapter;
            quest.ThreadName = "https://forums.sufficientvelocity.com/threads/vote-tally-program.199/page-19#post-4889303";
            Assert.ReferenceEquals(fa, quest.ForumAdapter);
        }

        [TestMethod]
        public async Task IQuest_InitForumAdapter_Change_DiffHost()
        {
            Assert.IsNull(quest.ForumAdapter);
            quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            await quest.InitForumAdapter();
            Assert.IsNotNull(quest.ForumAdapter);
            var fa = quest.ForumAdapter;
            quest.ThreadName = "https://forums.spacebattles.com/threads/vote-tally-program-v3.260204/page-24";
            Assert.IsNull(quest.ForumAdapter);
        }
        #endregion

    }
}
