using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Platform;
using NetTally.Votes.Experiment;
using NetTally.Utility;

namespace NetTally.Tests
{
    [TestClass]
    public class VotingRecordsTests
    {
        #region Setup
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            Agnostic.HashStringsUsing(UnicodeHashFunction.HashFunction);
        }

        [TestInitialize]
        public void Initialize()
        {
            VotingRecords.Instance.Reset();
        }
        #endregion

        #region Add Voters
        [TestMethod]
        public void AddVoter_1()
        {
            VotingRecords.Instance.AddVoterName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("Names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("Names"));
        }

        [TestMethod]
        public void AddVoter_2()
        {
            VotingRecords.Instance.AddVoterName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("names"));
        }

        [TestMethod]
        public void AddVoter_3()
        {
            VotingRecords.Instance.AddVoterName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("NAME"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("NAMES"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("NAMES"));
        }

        [TestMethod]
        public void AddVoter_4()
        {
            VotingRecords.Instance.AddVoterName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("n-ame"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("n-ames"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("n-ame"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("n-ames"));
        }

        [TestMethod]
        public void AddVoter_5()
        {
            VotingRecords.Instance.AddVoterName("N'ame");
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("Names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetVoterName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("Names"));
        }

        [TestMethod]
        public void AddVoter_6()
        {
            VotingRecords.Instance.AddVoterName("N'ame");
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetVoterName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("names"));
        }
        #endregion

        #region Add Voters
        [TestMethod]
        public void AddPlan_1()
        {
            VotingRecords.Instance.AddPlanName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("Names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("Names"));
        }

        [TestMethod]
        public void AddPlan_2()
        {
            VotingRecords.Instance.AddPlanName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("names"));
        }

        [TestMethod]
        public void AddPlan_3()
        {
            VotingRecords.Instance.AddPlanName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("NAME"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("NAMES"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("NAMES"));
        }

        [TestMethod]
        public void AddPlan_4()
        {
            VotingRecords.Instance.AddPlanName("Name");
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("n-ame"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("n-ames"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("n-ame"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("n-ames"));
        }

        [TestMethod]
        public void AddPlan_5()
        {
            VotingRecords.Instance.AddPlanName("N'ame");
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("Names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetPlanName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("Names"));
        }

        [TestMethod]
        public void AddPlan_6()
        {
            VotingRecords.Instance.AddPlanName("N'ame");
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetPlanName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("names"));
        }
        #endregion
    }
}
