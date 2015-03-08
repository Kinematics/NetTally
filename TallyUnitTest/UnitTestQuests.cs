using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;

namespace TallyUnitTest
{
    [TestClass]
    public class UnitTestQuests
    {
        List<string> propertyNameRaised = new List<string>();
        static Quests quests;
        static Quest q1;
        static Quest q2;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            quests = new Quests();
            q1 = new Quest();
            q2 = new Quest();
            q1.Name = "NewQuest1";
            q2.Name = "NewQuest2";
        }

        [TestInitialize()]
        public void Initialize()
        {
            quests.Clear();
        }


        #region Constructor
        [TestMethod]
        public void TestDefaultObject()
        {
            var a = new Quests();
            var f = a.QuestListList.FirstOrDefault();

            Assert.AreEqual(f, quests.CurrentQuest);

            Assert.AreEqual(quests.QuestList.Length, a.QuestList.Length);
            Assert.AreEqual(quests.QuestListNames.Count, a.QuestListNames.Count);
        }
        #endregion

        #region CurrentQuest property
        [TestMethod]
        public void TestCurrentQuest()
        {
            quests.AddQuest(q1);
            Assert.AreEqual(q1, quests.CurrentQuest);

            quests.AddQuest(q2);
            Assert.AreEqual(q1, quests.CurrentQuest);

            quests.CurrentQuest = q2;
            Assert.AreEqual(q2, quests.CurrentQuest);
        }

        [TestMethod]
        public void TestCurrentQuestNull()
        {
            quests.CurrentQuest = null;
            Assert.AreEqual(null, quests.CurrentQuest);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestCurrentQuestNotPresent()
        {
            quests.CurrentQuest = q1;
        }
        #endregion

        #region QuestList Property
        [TestMethod]
        public void TestQuestList()
        {
            quests.AddQuest(q1);
            quests.AddQuest(q2);

            var n = quests.QuestList;
            Assert.AreEqual(q1, Array.Find<Quest>(n, p => p.Name == "NewQuest1"));
            Assert.AreEqual(q2, Array.Find<Quest>(n, p => p.Name == "NewQuest2"));
        }

        [TestMethod]
        public void TestQuestListNames()
        {
            quests.AddQuest(q1);
            quests.AddQuest(q2);

            Assert.IsTrue(quests.QuestListNames.Contains("NewQuest1"));
            Assert.IsTrue(quests.QuestListNames.Contains("NewQuest2"));
        }
        #endregion

        #region Add and Remove
        [TestMethod]
        public void TestClear()
        {
            quests.AddQuest(q1);
            quests.Clear();
            Assert.AreEqual(0, quests.QuestListNames.Count);
            Assert.AreEqual(null, quests.CurrentQuest);
        }

        [TestMethod]
        public void TestAddQuest()
        {
            bool result = quests.AddQuest(q1);

            Assert.AreEqual(true, result);
            Assert.AreEqual(1, quests.QuestListNames.Count);
            Assert.AreEqual(q1, quests.CurrentQuest);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestAddQuestNull()
        {
            quests.AddQuest(null);
        }

        [TestMethod]
        public void TestAddQuestDuplicate()
        {
            bool result1 = quests.AddQuest(q1);
            bool result2 = quests.AddQuest(q1);
            Assert.AreEqual(true, result1);
            Assert.AreEqual(false, result2);
            Assert.AreEqual(1, quests.QuestListNames.Count);
        }

        [TestMethod]
        public void TestAddNewQuest()
        {
            var q = quests.AddNewQuest();

            Assert.AreEqual("New Entry", q.Name);
            Assert.AreEqual(q, quests.CurrentQuest);
            Assert.AreEqual(1, quests.QuestListNames.Count);
        }

        [TestMethod]
        public void TestAddNewQuestDuplicate()
        {
            var q1 = quests.AddNewQuest();
            var q2 = quests.AddNewQuest();

            Assert.AreEqual("New Entry", q1.Name);
            Assert.AreEqual(q1, quests.CurrentQuest);
            Assert.AreEqual(1, quests.QuestListNames.Count);
            Assert.AreEqual(q1, q2);
        }

        [TestMethod]
        public void TestRemoveQuest()
        {
            quests.AddQuest(q1);
            quests.AddQuest(q2);
            bool result = quests.RemoveQuest(q2);

            Assert.AreEqual(true, result);
            Assert.AreEqual(1, quests.QuestListNames.Count);
            Assert.AreEqual(q1, quests.CurrentQuest);
        }

        [TestMethod]
        public void TestRemoveNullQuest()
        {
            quests.AddQuest(q1);

            bool result = quests.RemoveQuest(null);

            Assert.AreEqual(false, result);
            Assert.AreEqual(1, quests.QuestListNames.Count);
            Assert.AreEqual(q1, quests.CurrentQuest);
        }

        [TestMethod]
        public void TestRemoveNonexistantQuest()
        {
            quests.AddQuest(q1);
            bool result = quests.RemoveQuest(q2);

            Assert.AreEqual(result, false);
            Assert.AreEqual(1, quests.QuestListNames.Count);
            Assert.AreEqual(q1, quests.CurrentQuest);
        }

        [TestMethod]
        public void TestRemoveCurrentQuest()
        {
            quests.AddQuest(q1);
            quests.AddQuest(q2);

            Assert.AreEqual(q1, quests.CurrentQuest);

            bool result = quests.RemoveCurrentQuest();

            Assert.AreEqual(true, result);
            Assert.AreEqual(q2, quests.CurrentQuest);
            Assert.AreEqual(1, quests.QuestListNames.Count);
        }

        [TestMethod]
        public void TestRemoveOnlyCurrentQuest()
        {
            quests.AddNewQuest();

            bool result = quests.RemoveCurrentQuest();

            Assert.AreEqual(true, result);
            Assert.AreEqual(null, quests.CurrentQuest);
            Assert.AreEqual(0, quests.QuestListNames.Count);
        }

        [TestMethod]
        public void TestRemoveCurrentQuestEmpty()
        {
            Assert.AreEqual(null, quests.CurrentQuest);
            bool result = quests.RemoveCurrentQuest();
            Assert.AreEqual(false, result);
        }
        #endregion

        #region Quests by name
        [TestMethod]
        public void TestStaticGetQuestByName()
        {
            quests.AddQuest(q1);
            quests.AddQuest(q2);

            var q3 = Quests.StaticGetQuestByName("NewQuest1");
            Assert.AreEqual(q1, q3);

            var q4 = Quests.StaticGetQuestByName("NewQuest4");
            Assert.AreEqual(null, q4);
        }

        [TestMethod]
        public void TestSetCurrentQuestByName()
        {
            quests.AddQuest(q1);
            quests.AddQuest(q2);

            quests.SetCurrentQuestByName("NewQuest1");
            Assert.AreEqual("NewQuest1", quests.CurrentQuest.Name);

            quests.SetCurrentQuestByName("NewQuest2");
            Assert.AreEqual("NewQuest2", quests.CurrentQuest.Name);
        }

        [TestMethod]
        public void TestSetCurrentNonexistQuestByName()
        {
            quests.AddQuest(q1);
            quests.AddQuest(q2);

            quests.SetCurrentQuestByName("NewQuest3");
            Assert.AreEqual("NewQuest1", quests.CurrentQuest.Name);

            quests.CurrentQuest = null;
            quests.SetCurrentQuestByName("NewQuest3");
            Assert.AreEqual("NewQuest1", quests.CurrentQuest.Name);
        }

        [TestMethod]
        public void TestCurrentQuestName()
        {
            Assert.AreEqual(null, quests.CurrentQuestName);

            quests.AddQuest(q1);
            quests.AddQuest(q2);

            Assert.AreEqual("NewQuest1", quests.CurrentQuestName);

            quests.CurrentQuest = q2;

            Assert.AreEqual("NewQuest2", quests.CurrentQuestName);

            quests.CurrentQuestName = q1.Name;

            Assert.AreEqual("NewQuest1", quests.CurrentQuestName);
        }
        #endregion

        #region Events
        [TestMethod]
        public void TestEventRaisingQuestListNames()
        {
            var a = new Quests();
            quests.PropertyChanged += A_PropertyChanged;

            quests.Clear();

            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";

            propertyNameRaised.Clear();
            quests.AddNewQuest();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));
            Assert.IsTrue(propertyNameRaised.Contains("CurrentQuest"));

            propertyNameRaised.Clear();
            quests.AddQuest(q1);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            quests.AddQuest(q2);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            quests.RemoveQuest(q2);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            quests.RemoveCurrentQuest();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            quests.Clear();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            quests.Update();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            Quest[] a_list = new Quest[2] { q1, q2 };
            quests.QuestList = a_list;
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            quests.PropertyChanged -= A_PropertyChanged;
        }

        [TestMethod]
        public void TestEventRaisingCurrentQuest()
        {
            var a = new Quests();
            quests.PropertyChanged += A_PropertyChanged;

            quests.Clear();

            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";

            propertyNameRaised.Clear();
            quests.AddQuest(q1);
            Assert.IsTrue(propertyNameRaised.Contains("CurrentQuest"));

            propertyNameRaised.Clear();
            quests.AddQuest(q2);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            quests.CurrentQuest = q2;
            Assert.IsTrue(propertyNameRaised.Contains("CurrentQuest"));

            quests.PropertyChanged -= A_PropertyChanged;
        }


        private void A_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            propertyNameRaised.Add(e.PropertyName);
        }
        #endregion

    }
}
