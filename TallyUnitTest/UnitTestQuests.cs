using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;

namespace TallyUnitTest
{
    [TestClass]
    public class UnitTestQuests
    {
        List<string> propertyNameRaised = new List<string>();

        #region Constructor
        [TestMethod]
        public void TestDefaultObject()
        {
            var a = new Quests();

            Assert.AreEqual(null, a.CurrentQuest);
            Assert.AreEqual(0, a.QuestList.Length);
            Assert.AreEqual(0, a.QuestListNames.Count);
        }
        #endregion

        #region CurrentQuest property
        [TestMethod]
        public void TestCurrentQuest()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";

            a.AddQuest(q1);
            Assert.AreEqual(q1, a.CurrentQuest);

            a.AddQuest(q2);
            Assert.AreEqual(q1, a.CurrentQuest);

            a.CurrentQuest = q2;
            Assert.AreEqual(q2, a.CurrentQuest);
        }

        [TestMethod]
        public void TestCurrentQuestNull()
        {
            var a = new Quests();
            a.Clear();
            a.CurrentQuest = null;

            Assert.AreEqual(null, a.CurrentQuest);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestCurrentQuestNotPresent()
        {
            var a = new Quests();
            a.Clear();
            var q = new Quest();
            q.Name = "TestAddedQuest";
            a.CurrentQuest = q;
        }
        #endregion

        #region QuestList Property
        [TestMethod]
        public void TestQuestList()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";
            a.AddQuest(q1);
            a.AddQuest(q2);

            var n = a.QuestList;
            Assert.AreEqual(q1, Array.Find<Quest>(n, p => p.Name == "NewQuest1"));
            Assert.AreEqual(q2, Array.Find<Quest>(n, p => p.Name == "NewQuest2"));
        }

        [TestMethod]
        public void TestQuestListNames()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";
            a.AddQuest(q1);
            a.AddQuest(q2);

            var n = a.QuestListNames;
            Assert.IsTrue(n.Contains("NewQuest1"));
            Assert.IsTrue(n.Contains("NewQuest2"));
        }
        #endregion

        #region Add and Remove
        [TestMethod]
        public void TestClear()
        {
            var a = new Quests();
            var q = new Quest();
            a.AddQuest(q);
            a.Clear();
            Assert.AreEqual(0, a.QuestListNames.Count);
            Assert.AreEqual(null, a.CurrentQuest);
        }

        [TestMethod]
        public void TestAddQuest()
        {
            var a = new Quests();
            a.Clear();
            var q = new Quest();
            q.Name = "TestAddedQuest";
            bool result = a.AddQuest(q);

            Assert.AreEqual(true, result);
            Assert.AreEqual(1, a.QuestListNames.Count);
            Assert.AreEqual(q, a.CurrentQuest);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestAddQuestNull()
        {
            var a = new Quests();
            a.Clear();
            a.AddQuest(null);
        }

        [TestMethod]
        public void TestAddQuestDuplicate()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            var q2 = new Quest();
            bool result1 = a.AddQuest(q1);
            bool result2 = a.AddQuest(q1);
            Assert.AreEqual(true, result1);
            Assert.AreEqual(false, result2);
            Assert.AreEqual(1, a.QuestListNames.Count);
        }

        [TestMethod]
        public void TestAddNewQuest()
        {
            var a = new Quests();
            a.Clear();
            var q = a.AddNewQuest();

            Assert.AreEqual("New Entry", q.Name);
            Assert.AreEqual(q, a.CurrentQuest);
            Assert.AreEqual(1, a.QuestListNames.Count);
        }

        [TestMethod]
        public void TestAddNewQuestDuplicate()
        {
            var a = new Quests();
            a.Clear();
            var q1 = a.AddNewQuest();
            var q2 = a.AddNewQuest();

            Assert.AreEqual("New Entry", q1.Name);
            Assert.AreEqual(q1, a.CurrentQuest);
            Assert.AreEqual(1, a.QuestListNames.Count);
            Assert.AreEqual(q1, q2);
        }

        [TestMethod]
        public void TestRemoveQuest()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";

            a.AddQuest(q1);
            a.AddQuest(q2);
            bool result = a.RemoveQuest(q2);

            Assert.AreEqual(true, result);
            Assert.AreEqual(1, a.QuestListNames.Count);
            Assert.AreEqual(q1, a.CurrentQuest);
        }

        [TestMethod]
        public void TestRemoveNullQuest()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            a.AddQuest(q1);

            bool result = a.RemoveQuest(null);

            Assert.AreEqual(false, result);
            Assert.AreEqual(1, a.QuestListNames.Count);
            Assert.AreEqual(q1, a.CurrentQuest);
        }

        [TestMethod]
        public void TestRemoveNonexistantQuest()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";

            a.AddQuest(q1);
            bool result = a.RemoveQuest(q2);

            Assert.AreEqual(result, false);
            Assert.AreEqual(1, a.QuestListNames.Count);
            Assert.AreEqual(q1, a.CurrentQuest);
        }

        [TestMethod]
        public void TestRemoveCurrentQuest()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";
            a.AddQuest(q1);
            a.AddQuest(q2);

            Assert.AreEqual(q1, a.CurrentQuest);

            bool result = a.RemoveCurrentQuest();

            Assert.AreEqual(true, result);
            Assert.AreEqual(q2, a.CurrentQuest);
            Assert.AreEqual(1, a.QuestListNames.Count);
        }

        [TestMethod]
        public void TestRemoveOnlyCurrentQuest()
        {
            var a = new Quests();
            a.Clear();
            a.AddNewQuest();

            bool result = a.RemoveCurrentQuest();

            Assert.AreEqual(true, result);
            Assert.AreEqual(null, a.CurrentQuest);
            Assert.AreEqual(0, a.QuestListNames.Count);
        }

        [TestMethod]
        public void TestRemoveCurrentQuestEmpty()
        {
            var a = new Quests();
            a.Clear();

            Assert.AreEqual(null, a.CurrentQuest);
            bool result = a.RemoveCurrentQuest();
            Assert.AreEqual(false, result);
        }
        #endregion

        #region Quests by name
        [TestMethod]
        public void TestStaticGetQuestByName()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";
            a.AddQuest(q1);
            a.AddQuest(q2);

            var q3 = Quests.StaticGetQuestByName("NewQuest1");
            Assert.AreEqual(q1, q3);

            var q4 = Quests.StaticGetQuestByName("NewQuest4");
            Assert.AreEqual(null, q4);

        }

        [TestMethod]
        public void TestSetCurrentQuestByName()
        {
            var a = new Quests();
            a.Clear();
            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";
            a.AddQuest(q1);
            a.AddQuest(q2);

            a.SetCurrentQuestByName("NewQuest1");
            Assert.AreEqual("NewQuest1", a.CurrentQuest.Name);

            a.SetCurrentQuestByName("NewQuest2");
            Assert.AreEqual("NewQuest2", a.CurrentQuest.Name);

            a.SetCurrentQuestByName("NewQuest3");
            Assert.AreEqual("NewQuest2", a.CurrentQuest.Name);

            a.CurrentQuest = null;
            a.SetCurrentQuestByName("NewQuest3");
            Assert.AreEqual("NewQuest1", a.CurrentQuest.Name);
        }

        [TestMethod]
        public void TestCurrentQuestByName()
        {
            var a = new Quests();
            a.Clear();

            Assert.AreEqual(null, a.CurrentQuestName);

            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";
            a.AddQuest(q1);
            a.AddQuest(q2);

            Assert.AreEqual("NewQuest1", a.CurrentQuestName);

            a.CurrentQuest = q2;

            Assert.AreEqual("NewQuest2", a.CurrentQuestName);

            a.CurrentQuestName = q1.Name;

            Assert.AreEqual("NewQuest1", a.CurrentQuestName);
        }
        #endregion

        #region Events
        [TestMethod]
        public void TestEventRaisingQuestListNames()
        {
            var a = new Quests();
            a.PropertyChanged += A_PropertyChanged;

            a.Clear();

            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";

            propertyNameRaised.Clear();
            a.AddNewQuest();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));
            Assert.IsTrue(propertyNameRaised.Contains("CurrentQuest"));

            propertyNameRaised.Clear();
            a.AddQuest(q1);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            a.AddQuest(q2);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            a.RemoveQuest(q2);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            a.RemoveCurrentQuest();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            a.Clear();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            a.Update();
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            Quest[] a_list = new Quest[2] { q1, q2 };
            a.QuestList = a_list;
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            a.PropertyChanged -= A_PropertyChanged;
        }

        [TestMethod]
        public void TestEventRaisingCurrentQuest()
        {
            var a = new Quests();
            a.PropertyChanged += A_PropertyChanged;

            a.Clear();

            var q1 = new Quest();
            q1.Name = "NewQuest1";
            var q2 = new Quest();
            q2.Name = "NewQuest2";

            propertyNameRaised.Clear();
            a.AddQuest(q1);
            Assert.IsTrue(propertyNameRaised.Contains("CurrentQuest"));

            propertyNameRaised.Clear();
            a.AddQuest(q2);
            Assert.IsTrue(propertyNameRaised.Contains("QuestListNames"));

            propertyNameRaised.Clear();
            a.CurrentQuest = q2;
            Assert.IsTrue(propertyNameRaised.Contains("CurrentQuest"));

            a.PropertyChanged -= A_PropertyChanged;
        }


        private void A_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            propertyNameRaised.Add(e.PropertyName);
        }
        #endregion

    }
}
