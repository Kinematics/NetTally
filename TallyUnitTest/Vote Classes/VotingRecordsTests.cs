using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tests.Platform;
using NetTally.Votes.Experiment;
using NetTally.Utility;
using NetTally.Votes;

namespace NetTally.Tests
{
    [TestClass]
    public class VotingRecordsTests
    {
        static Identity defaultIdentity = new Identity("Name", "1");
        static VotePartition defaultPlanPartition = new VotePartition(
            new VoteLine("[X] Plan Name\n-[X] Some content"), VoteType.Plan);

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
            VotingRecords.Instance.AddVoterIdentity(defaultIdentity);
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("Names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetLastVoterIdentity("Name")?.Name);
            Assert.AreEqual(null, VotingRecords.Instance.GetLastVoterIdentity("Names")?.Name);
        }

        [TestMethod]
        public void AddVoter_2()
        {
            VotingRecords.Instance.AddVoterIdentity(defaultIdentity);
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetLastVoterIdentity("name")?.Name);
            Assert.AreEqual(null, VotingRecords.Instance.GetLastVoterIdentity("names")?.Name);
        }

        [TestMethod]
        public void AddVoter_3()
        {
            VotingRecords.Instance.AddVoterIdentity(defaultIdentity);
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("NAME"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("NAMES"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetLastVoterIdentity("NAME")?.Name);
            Assert.AreEqual(null, VotingRecords.Instance.GetLastVoterIdentity("NAMES")?.Name);
        }

        [TestMethod]
        public void AddVoter_4()
        {
            VotingRecords.Instance.AddVoterIdentity(defaultIdentity);
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("n-ame"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("n-ames"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetLastVoterIdentity("n-ame")?.Name);
            Assert.AreEqual(null, VotingRecords.Instance.GetLastVoterIdentity("n-ames")?.Name);
        }

        [TestMethod]
        public void AddVoter_5()
        {
            Identity identity = new Identity("N'ame", "1");

            VotingRecords.Instance.AddVoterIdentity(identity);
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("Names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetLastVoterIdentity("Name")?.Name);
            Assert.AreEqual(null, VotingRecords.Instance.GetLastVoterIdentity("Names")?.Name);
        }

        [TestMethod]
        public void AddVoter_6()
        {
            Identity identity = new Identity("N'ame", "1");

            VotingRecords.Instance.AddVoterIdentity(identity);
            Assert.IsTrue(VotingRecords.Instance.HasVoterName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasVoterName("names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetLastVoterIdentity("NAME")?.Name);
            Assert.AreEqual(null, VotingRecords.Instance.GetLastVoterIdentity("names")?.Name);
        }
        #endregion

        #region Add Voters
        [TestMethod]
        public void AddPlan_1()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("Names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("Names"));
        }

        [TestMethod]
        public void AddPlan_2()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("names"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("names"));
        }

        [TestMethod]
        public void AddPlan_3()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("NAME"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("NAMES"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("NAMES"));
        }

        [TestMethod]
        public void AddPlan_4()
        {
            Plan plan = new Plan("Name", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["Name"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("n-ame"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("n-ames"));
            Assert.AreEqual("Name", VotingRecords.Instance.GetPlanName("n-ame"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("n-ames"));
        }

        [TestMethod]
        public void AddPlan_5()
        {
            Plan plan = new Plan("N'ame", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["N'ame"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("Name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("Names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetPlanName("Name"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("Names"));
        }

        [TestMethod]
        public void AddPlan_6()
        {
            Plan plan = new Plan("N'ame", defaultIdentity, defaultPlanPartition, PlanType.Content);
            var plans = new List<Plan> { plan };
            Dictionary<string, List<Plan>> planDict = new Dictionary<string, List<Plan>> { ["N'ame"] = plans };
            VotingRecords.Instance.AddPlans(planDict);
            Assert.IsTrue(VotingRecords.Instance.HasPlanName("name"));
            Assert.IsFalse(VotingRecords.Instance.HasPlanName("names"));
            Assert.AreEqual("N'ame", VotingRecords.Instance.GetPlanName("NAME"));
            Assert.AreEqual(null, VotingRecords.Instance.GetPlanName("names"));
        }
        #endregion
    }
}
