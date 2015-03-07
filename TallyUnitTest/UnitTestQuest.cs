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

            Assert.AreEqual(a.Name, "New Entry");
            Assert.AreEqual(a.StartPost, 1);
            Assert.AreEqual(a.EndPost, 0);
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

            Assert.AreEqual(a.Name, testName);
        }

        [TestMethod]
        public void TestCleanName()
        {
            var a = new Quest();

            string testName = "awake-already-homura-nge-pmmm-fusion-quest.11111";
            string expectedName = "awake-already-homura-nge-pmmm-fusion-quest.11111";

            a.Name = testName;
            Assert.AreEqual(a.Name, expectedName);

            testName = "awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(a.Name, expectedName);

            testName = "http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111";

            a.Name = testName;
            Assert.AreEqual(a.Name, expectedName);

            testName = "http://forums.sufficientvelocity.com/threads/awake-already-homura-nge-pmmm-fusion-quest.11111/page-34#post-2943518";

            a.Name = testName;
            Assert.AreEqual(a.Name, expectedName);
        }
        #endregion

        #region StartPost
        [TestMethod]
        public void TestSetStart()
        {
            var a = new Quest();

            int testPost = 448;

            a.StartPost = testPost;

            Assert.AreEqual(a.StartPost, testPost);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetStartZero()
        {
            var a = new Quest();

            int testPost = 0;

            a.StartPost = testPost;

            Assert.AreEqual(a.StartPost, testPost);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetStartNegative()
        {
            var a = new Quest();

            int testPost = -20;

            a.StartPost = testPost;

            Assert.AreEqual(a.StartPost, testPost);
        }
        #endregion

        #region EndPost
        [TestMethod]
        public void TestSetEnd()
        {
            var a = new Quest();

            int testPost = 448;

            a.EndPost = testPost;

            Assert.AreEqual(a.EndPost, testPost);
        }

        [TestMethod]
        public void TestSetEndZero()
        {
            var a = new Quest();

            int testPost = 0;

            a.EndPost = testPost;

            Assert.AreEqual(a.EndPost, testPost);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestSetEndNegative()
        {
            var a = new Quest();

            int testPost = -20;

            a.EndPost = testPost;

            Assert.AreEqual(a.EndPost, testPost);
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
            Assert.AreEqual(propertyNameRaised, "Name");

            propertyNameRaised = string.Empty;

            a.StartPost = 10;
            Assert.AreEqual(propertyNameRaised, "StartPost");

            propertyNameRaised = string.Empty;

            a.EndPost = 20;
            Assert.AreEqual(propertyNameRaised, "EndPost");
        }

        private void A_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            propertyNameRaised = e.PropertyName;
        }
        #endregion
    }
}
