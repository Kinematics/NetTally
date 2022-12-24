using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.Types.Enums;
using NetTally.VoteCounting;
using NetTally.Web;

namespace NetTally.Tests.QuestTests
{
    /// <summary>
    /// Class that tests the implementation of the Quest class against the IQuest interface.
    /// </summary>
    /// <summary>
    /// Class that tests a given IQuest object.
    /// </summary>
    [TestClass]
    public class QuestTests
    {
        #region Setup
        static IServiceProvider serviceProvider = null!;
        static IPageProvider pageProvider = null!;

        Quest Quest { get; set; } = null!;
        bool notified;
        readonly List<string> propertiesRaised = new();


        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();
            pageProvider = serviceProvider.GetRequiredService<IPageProvider>();
        }

        [TestInitialize]
        public void Initialize()
        {
            Quest = new Quest()
            {
                VoteCounter = serviceProvider.GetRequiredService<IVoteCounter>()
            };

            Quest.PropertyChanged += IQuest_PropertyChanged;
        }

        /// <summary>
        /// Cleanup per test.
        /// </summary>
        [TestCleanup]
        public void Reset()
        {
            notified = false;
            propertiesRaised.Clear();
            Quest.PropertyChanged -= IQuest_PropertyChanged;
        }
        #endregion

        #region Stuff for handling checking event notification
        void IQuest_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            notified = true;

            propertiesRaised.Add(e?.PropertyName ?? "");
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
            Assert.AreEqual(Quest.NewThreadEntry, Quest.ThreadName);
            Assert.AreEqual("fake-thread.00000", Quest.DisplayName);
            Assert.AreEqual(Quest.NewThreadEntry, Quest.ThreadUri?.AbsoluteUri);

            Assert.AreEqual(0, Quest.PostsPerPage);
            Assert.AreEqual(1, Quest.StartPost);
            Assert.AreEqual(0, Quest.EndPost);
            Assert.AreEqual(true, Quest.ReadToEndOfThread);
            Assert.AreEqual(true, Quest.CheckForLastThreadmark);

            Assert.AreEqual(PartitionMode.None, Quest.PartitionMode);

            Assert.IsFalse(Quest.UseCustomThreadmarkFilters);
            Assert.IsFalse(Quest.UseCustomTaskFilters);
            Assert.AreEqual("", Quest.CustomThreadmarkFilters);
            Assert.AreEqual("", Quest.CustomTaskFilters);

            Assert.AreEqual(Quest.DisplayName, Quest.ToString());
        }
        #endregion

        #region Thread Name
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IQuest_ThreadName_Invalid_Null()
        {
            Quest.ThreadName = null!;
        }

        [TestMethod]
        public void IQuest_ThreadName_Invalid_Null_NoChange()
        {
            try
            {
                Quest.ThreadName = null!;
                Assert.Fail("An exception should have been thrown.");
            }
            catch (ArgumentException)
            {

            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception caught:\n" + e.Message);
            }

            Assert.AreEqual(Quest.NewThreadEntry, Quest.ThreadName);
            VerifyNoNotification();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IQuest_ThreadName_Invalid_Blank()
        {
            Quest.ThreadName = "";
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IQuest_ThreadName_Invalid_Empty()
        {
            Quest.ThreadName = "  ";
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IQuest_ThreadName_Invalid_Host()
        {
            Quest.ThreadName = "/forums.sufficientvelocity.com/";
            Assert.AreEqual("/forums.sufficientvelocity.com/", Quest.ThreadName);
            VerifyNotification("ThreadName");
        }

        [TestMethod]
        public void IQuest_ThreadName_ValidHost()
        {
            Quest.ThreadName = "http://forums.sufficientvelocity.com/";
            Assert.AreEqual("http://forums.sufficientvelocity.com/", Quest.ThreadName);
            VerifyNotification("ThreadName");
        }

        [TestMethod]
        public void IQuest_ThreadName_WithThread()
        {
            Quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", Quest.ThreadName);
            VerifyNotification("ThreadName");
        }

        [TestMethod]
        public void IQuest_ThreadName_WithPage()
        {
            Quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/page-221";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", Quest.ThreadName);
            VerifyNotification("ThreadName");
        }

        [TestMethod]
        public void IQuest_ThreadName_WithPost()
        {
            Quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/page-221#post-19942121";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", Quest.ThreadName);
            VerifyNotification("ThreadName");
            Quest.ThreadName = "http://www.fandompost.com/oldforums/showthread.php?39239-Yurikuma-Arashi-Discussion-Thread&p=288335#post288335";
            Assert.AreEqual("http://www.fandompost.com/oldforums/showthread.php?39239-Yurikuma-Arashi-Discussion-Thread", Quest.ThreadName);
        }

        [TestMethod]
        public void IQuest_ThreadName_RemoveInvalidUnicode()
        {
            Quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-\u200bhomura-quest.10402/page-221#post-19942121";
            Assert.AreEqual("http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/", Quest.ThreadName);
        }

        #endregion

        #region Display Name
        [TestMethod]
        public void IQuest_DisplayName_Null()
        {
#nullable disable
            Quest.DisplayName = null;
#nullable enable
            Assert.AreEqual("fake-thread.00000", Quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Blank()
        {
            Quest.DisplayName = "";
            Assert.AreEqual("fake-thread.00000", Quest.DisplayName);
            VerifyNoNotification();
        }

        [TestMethod]
        public void IQuest_DisplayName_Empty()
        {
            Quest.DisplayName = "   ";
            Assert.AreEqual("   ", Quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Normal()
        {
            Quest.DisplayName = "My Quest";
            Assert.AreEqual("My Quest", Quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Normal_CleanUnicode()
        {
            Quest.DisplayName = "My\u200bQuest";
            Assert.AreEqual("MyQuest", Quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_Normal_Trim()
        {
            Quest.DisplayName = " My Quest  ";
            Assert.AreEqual(" My Quest  ", Quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_ResetNull()
        {
            Quest.DisplayName = "My Quest";
#nullable disable
            Quest.DisplayName = null;
#nullable enable
            Assert.AreEqual("fake-thread.00000", Quest.DisplayName);
            VerifyNotification("DisplayName");
        }

        [TestMethod]
        public void IQuest_DisplayName_ResetEmpty()
        {
            Quest.DisplayName = "My Quest";
            Quest.DisplayName = "";
            Assert.AreEqual("fake-thread.00000", Quest.DisplayName);
            VerifyNotification("DisplayName");
        }
        #endregion

        #region Start and End Post Numbers
        [TestMethod]
        public void IQuest_StartPost_Zero()
        {
            Quest.StartPost = 0;
            VerifyNotification("HasErrors");
        }

        [TestMethod]
        public void IQuest_StartPost_Negative()
        {
            Quest.StartPost = -1;
            VerifyNotification("HasErrors");
        }

        [TestMethod]
        public void IQuest_StartPost_Min()
        {
            Quest.StartPost = int.MinValue;
            VerifyNotification("HasErrors");
        }

        [TestMethod]
        public void IQuest_StartPost_One()
        {
            Quest.StartPost = 1;
            VerifyNoNotification();
        }

        [TestMethod]
        public void IQuest_StartPost_Positive()
        {
            Quest.StartPost = 45000;
            VerifyNotification("StartPost");
        }

        [TestMethod]
        public void IQuest_StartPost_Max()
        {
            Quest.StartPost = int.MaxValue;
            VerifyNotification("StartPost");
        }

        [TestMethod]
        public void IQuest_EndPost_Negative()
        {
            Quest.EndPost = -1;
            VerifyNotification("HasErrors");
        }

        [TestMethod]
        public void IQuest_EndPost_Min()
        {
            Quest.EndPost = int.MinValue;
            VerifyNotification("HasErrors");
        }

        [TestMethod]
        public void IQuest_EndPost_Zero()
        {
            Quest.EndPost = 0;
            VerifyNoNotification();
        }

        [TestMethod]
        public void IQuest_EndPost_One()
        {
            Quest.EndPost = 1;
            VerifyNotification("EndPost");
        }

        [TestMethod]
        public void IQuest_EndPost_Positive()
        {
            Quest.EndPost = 45000;
            VerifyNotification("EndPost");
        }

        [TestMethod]
        public void IQuest_EndPost_Max()
        {
            Quest.EndPost = int.MaxValue;
            VerifyNotification("EndPost");
        }
        #endregion

        #region Various property notification checks
        [TestMethod]
        public void IQuest_PostsPerPage_Notify()
        {
            Quest.PostsPerPage = 25;
            VerifyNotification("PostsPerPage");
        }

        [TestMethod]
        public void IQuest_CheckForLastThreadmark_Notify()
        {
            Quest.CheckForLastThreadmark = false;
            VerifyNotification("CheckForLastThreadmark");
        }

        [TestMethod]
        public void IQuest_PartitionMode_Notify()
        {
            Quest.PartitionMode = PartitionMode.ByBlock;
            VerifyNotification("PartitionMode");
        }
        #endregion

        #region Read to End of Thread
        [TestMethod]
        public void IQuest_ReadToEndOfThread_NoThreadmarks_ZeroEnd()
        {
            Quest.EndPost = 0;
            Assert.IsTrue(Quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_NoThreadmarks_PosEnd()
        {
            Quest.EndPost = 100;
            Assert.IsFalse(Quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_Threadmarks_ZeroEnd()
        {
            Quest.EndPost = 0;
            Quest.CheckForLastThreadmark = true;
            Assert.IsTrue(Quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_Threadmarks_PosEnd()
        {
            Quest.EndPost = 100;
            Quest.CheckForLastThreadmark = true;
            Assert.IsFalse(Quest.ReadToEndOfThread);
        }

        [TestMethod]
        public void IQuest_ReadToEndOfThread_Threadmarks_FoundThreadmark()
        {
            Quest.EndPost = 100;
            Quest.CheckForLastThreadmark = true;
            Assert.IsFalse(Quest.ReadToEndOfThread);
        }
        #endregion

        #region Forum Adapter
        [TestMethod]
        public async Task IQuest_SetThreadName()
        {
            Quest.ThreadName = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            await Task.Delay(1);
        }

        [TestMethod]
        public async Task IQuest_IdentifyThread()
        {
            Quest.ThreadName = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            var forumType = await ForumIdentifier.IdentifyForumTypeAsync(Quest.ThreadUri, pageProvider, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(ForumType.XenForo2, forumType);
        }

        [TestMethod]
        public async Task IQuest_IdentifyThread_Change_SameHost()
        {
            Quest.ThreadName = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            var forumType = await ForumIdentifier.IdentifyForumTypeAsync(Quest.ThreadUri, pageProvider, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(ForumType.XenForo2, forumType);
            Quest.ThreadName = "https://forums.sufficientvelocity.com/threads/vote-tally-program.199/page-19#post-4889303";
            forumType = await ForumIdentifier.IdentifyForumTypeAsync(Quest.ThreadUri, pageProvider, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(ForumType.XenForo2, forumType);
        }

        [TestMethod]
        public async Task IQuest_InitForumAdapter_Change_DiffHost()
        {
            Quest.ThreadName = "http://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
            var forumType = await ForumIdentifier.IdentifyForumTypeAsync(Quest.ThreadUri, pageProvider, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(ForumType.XenForo2, forumType);
            Quest.ThreadName = "https://forums.spacebattles.com/threads/vote-tally-program-v3.260204/page-24";
            forumType = await ForumIdentifier.IdentifyForumTypeAsync(Quest.ThreadUri, pageProvider, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(ForumType.XenForo2, forumType);
        }
        #endregion

    }
}
