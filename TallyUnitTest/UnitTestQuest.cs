using System;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;

namespace TallyUnitTest
{
    [TestClass]
    public class UnitTestQuest
    {
        string propertyNameRaised = string.Empty;

        #region Constructor
        [TestMethod]
        public void TestDefaultObject()
        {
            var a = new Quest();

            Assert.AreEqual("New Entry", a.Name);
            Assert.AreEqual(1, a.StartPost);
            Assert.AreEqual(0, a.EndPost);
        }
        #endregion

        #region Name
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetNameToNull()
        {
            var a = new Quest();

            a.Name = null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetNameToEmpty()
        {
            var a = new Quest();

            a.Name = string.Empty;
        }

        [TestMethod]
        public void TestSetName()
        {
            var a = new Quest();

            string testName = "Sample.Name";

            a.Name = testName;

            Assert.AreEqual(testName, a.Name);
        }

        [TestMethod]
        public void TestCleanName()
        {
            var a = new Quest();

            string testName = "awake-already-homura-nge-pmmm-fusion-quest.11111";
            string expectedName = "awake-already-homura-nge-pmmm-fusion-quest.11111";

            a.Name = testName;
            Assert.AreEqual(expectedName, a.Name);

            testName = "awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(expectedName, a.Name);

            testName = "http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111";

            a.Name = testName;
            Assert.AreEqual(expectedName, a.Name);

            testName = "http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(expectedName, a.Name);

            // This will fail the regex filter, so should just give back the original.
            testName = "/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(testName, a.Name);
        }

        [TestMethod]
        public void TestSetSiteToNull()
        {
            var a = new Quest();

            a.Site = null;

            Assert.AreEqual(string.Empty, a.Site);
        }


        [TestMethod]
        public void TestSetSiteToEmpty()
        {
            var a = new Quest();

            a.Site = string.Empty;

            Assert.AreEqual(string.Empty, a.Site);
        }

        [TestMethod]
        public void TestSetSite()
        {
            var a = new Quest();

            string testName = "http://forums.sufficientvelocity.com/";

            a.Site = testName;

            Assert.AreEqual(testName, a.Site);
        }
        #endregion

        #region StartPost
        [TestMethod]
        public void TestSetStart()
        {
            var a = new Quest();

            int testPost = 448;

            a.StartPost = testPost;

            Assert.AreEqual(testPost, a.StartPost);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetStartZero()
        {
            var a = new Quest();

            int testPost = 0;

            a.StartPost = testPost;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetStartNegative()
        {
            var a = new Quest();

            int testPost = -20;

            a.StartPost = testPost;
        }
        #endregion

        #region EndPost
        [TestMethod]
        public void TestSetEnd()
        {
            var a = new Quest();

            int testPost = 448;

            a.EndPost = testPost;

            Assert.AreEqual(testPost, a.EndPost);
        }

        [TestMethod]
        public void TestSetEndZero()
        {
            var a = new Quest();

            int testPost = 0;

            a.EndPost = testPost;

            Assert.AreEqual(testPost, a.EndPost);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetEndNegative()
        {
            var a = new Quest();

            int testPost = -20;

            a.EndPost = testPost;
        }
        #endregion

        #region End of thread
        [TestMethod]
        public void TestReadToEndOfThread()
        {
            var a = new Quest();

            a.EndPost = 1;
            Assert.AreEqual(false, a.ReadToEndOfThread);

            a.EndPost = 1000000;
            Assert.AreEqual(false, a.ReadToEndOfThread);

            a.EndPost = 0;
            Assert.AreEqual(true, a.ReadToEndOfThread);
        }

        #endregion


        #region Events
        [TestMethod]
        public void TestEventRaising()
        {
            var a = new Quest();
            a.PropertyChanged += A_PropertyChanged;

            propertyNameRaised = string.Empty;

            a.Name = "awake-already-homura-nge-pmmm-fusion-quest.11111";
            Assert.AreEqual("Name", propertyNameRaised);

            propertyNameRaised = string.Empty;

            a.StartPost = 10;
            Assert.AreEqual("StartPost", propertyNameRaised);

            propertyNameRaised = string.Empty;

            a.EndPost = 20;
            Assert.AreEqual("EndPost", propertyNameRaised);
        }

        private void A_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            propertyNameRaised = e.PropertyName;
        }
        #endregion
    }
}
