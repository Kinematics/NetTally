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
            Agnostic.InitStringComparers(UnicodeHashFunction.HashFunction);
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
            VotingRecords.Instance.AddVoter("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoter("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoter("Names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("Names"));
        }

        [TestMethod]
        public void AddVoter_2()
        {
            VotingRecords.Instance.AddVoter("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoter("name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoter("names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("names"));
        }

        [TestMethod]
        public void AddVoter_3()
        {
            VotingRecords.Instance.AddVoter("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoter("NAME"));
            Assert.IsFalse(VotingRecords.Instance.HasVoter("NAMES"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("NAMES"));
        }

        [TestMethod]
        public void AddVoter_4()
        {
            VotingRecords.Instance.AddVoter("Name");
            Assert.IsTrue(VotingRecords.Instance.HasVoter("n-ame"));
            Assert.IsFalse(VotingRecords.Instance.HasVoter("n-ames"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetVoterName("n-ame"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("n-ames"));
        }

        [TestMethod]
        public void AddVoter_5()
        {
            VotingRecords.Instance.AddVoter("N'ame");
            Assert.IsTrue(VotingRecords.Instance.HasVoter("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoter("Names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetVoterName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("Names"));
        }

        [TestMethod]
        public void AddVoter_6()
        {
            VotingRecords.Instance.AddVoter("N'ame");
            Assert.IsTrue(VotingRecords.Instance.HasVoter("name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoter("names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetVoterName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetVoterName("names"));
        }
        #endregion

    }
}
