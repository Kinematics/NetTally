using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Forums;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class VoterStorageTests
    {
        #region Setup
        static IServiceProvider serviceProvider;
        static VoteLineBlock vote;
        static readonly VoterStorage voterStorage = new VoterStorage();

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            VoteLine voteLine = new VoteLine("", "X", "", "A sample vote line", MarkerType.Vote, 100);
            vote = new VoteLineBlock(voteLine);
        }

        [TestInitialize]
        public void Initialize()
        {
            voterStorage.Reset();
        }
        #endregion

        [TestMethod]
        public void Store_One_Vote()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            voterStorage.Add(origin, vote);

            Assert.IsTrue(voterStorage.HasIdentity(origin));
            Assert.IsTrue(voterStorage.HasVoter(username));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Store_Same_Vote()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            voterStorage.Add(origin, vote);
            voterStorage.Add(origin, vote);

            Assert.IsTrue(voterStorage.HasIdentity(origin));
            Assert.IsTrue(voterStorage.HasVoter(username));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        public void Store_Same_Vote_Index()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            voterStorage.Add(origin, vote);
            voterStorage[origin] = vote;

            Assert.IsTrue(voterStorage.HasIdentity(origin));
            Assert.IsTrue(voterStorage.HasVoter(username));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        public void Try_Store_One_Vote()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            Assert.IsTrue(voterStorage.TryAdd(origin, vote));

            Assert.IsTrue(voterStorage.HasIdentity(origin));
            Assert.IsTrue(voterStorage.HasVoter(username));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        public void Try_Store_Same_Vote()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            Assert.IsTrue(voterStorage.TryAdd(origin, vote));
            Assert.IsFalse(voterStorage.TryAdd(origin, vote));

            Assert.IsTrue(voterStorage.HasIdentity(origin));
            Assert.IsTrue(voterStorage.HasVoter(username));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        public void Remove_Vote()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            voterStorage.Add(origin, vote);
            Assert.IsTrue(voterStorage.Remove(origin));

            Assert.IsFalse(voterStorage.HasIdentity(origin));
            Assert.IsFalse(voterStorage.HasVoter(username));
            Assert.AreEqual(0, voterStorage.Count);
        }


        [TestMethod]
        public void Remove_And_Get_Vote()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            voterStorage.Add(origin, vote);
            Assert.IsTrue(voterStorage.Remove(origin, out var removedVote));
            Assert.AreEqual(vote, removedVote);

            Assert.IsFalse(voterStorage.HasIdentity(origin));
            Assert.IsFalse(voterStorage.HasVoter(username));
            Assert.AreEqual(0, voterStorage.Count);
        }

        [TestMethod]
        public void Check_Voter()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");

            voterStorage.Add(origin, vote);

            Assert.IsTrue(voterStorage.HasIdentity(origin));
            Assert.IsTrue(voterStorage.HasVoter(username));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        public void Check_Plan()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");
            string planname = "Zoom";
            var planOrigin = origin.GetPlanOrigin(planname);

            voterStorage.Add(planOrigin, vote);

            Assert.IsFalse(voterStorage.HasIdentity(origin));
            Assert.IsTrue(voterStorage.HasIdentity(planOrigin));
            Assert.IsFalse(voterStorage.HasVoter(username));
            Assert.IsTrue(voterStorage.HasPlan(planname));
            Assert.AreEqual(1, voterStorage.Count);
        }


        [TestMethod]
        public void Check_Voter_Simple()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");
            Origin simpleOrigin = new Origin(username, IdentityType.User);

            voterStorage.Add(origin, vote);

            Assert.IsTrue(voterStorage.HasIdentity(simpleOrigin));
            Assert.IsTrue(voterStorage.HasVoter(username));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        public void Check_Plan_Simple()
        {
            string username = "Kinematics";
            Origin origin = new Origin(username, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");
            string planname = "Zoom";
            var planOrigin = origin.GetPlanOrigin(planname);
            Origin simpleOrigin = new Origin(planname, IdentityType.Plan);

            voterStorage.Add(planOrigin, vote);

            Assert.IsTrue(voterStorage.HasIdentity(simpleOrigin));
            Assert.IsTrue(voterStorage.HasPlan(planname));
            Assert.AreEqual(1, voterStorage.Count);
        }

        [TestMethod]
        public void Check_Complex_1()
        {
            string user1 = "Kinematics";
            Origin origin1 = new Origin(user1, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");
            string planname = "Zoom";
            var planOrigin = origin1.GetPlanOrigin(planname);
            string user2 = "Atreya";
            Origin origin2 = new Origin(user2, "123457", 101, new Uri("http://www.example.com/"), "http://www.example.com");
            string user3 = "Kimberly";
            Origin origin3 = new Origin(user3, "123458", 102, new Uri("http://www.example.com/"), "http://www.example.com");
            string user4 = "Biigoh";
            Origin origin4 = new Origin(user4, "123459", 103, new Uri("http://www.example.com/"), "http://www.example.com");
            string user5 = "Muramasa";
            Origin origin5 = new Origin(user5, "123460", 104, new Uri("http://www.example.com/"), "http://www.example.com");

            voterStorage.Add(planOrigin, vote);
            voterStorage.Add(origin1, vote);
            voterStorage.Add(origin2, vote);
            voterStorage.Add(origin3, vote);
            voterStorage.Add(origin4, vote);
            voterStorage.Add(origin5, vote);

            Assert.IsTrue(voterStorage.HasPlan(planname));
            Assert.IsTrue(voterStorage.HasVoter(user1));
            Assert.IsTrue(voterStorage.HasVoter(user2));
            Assert.IsTrue(voterStorage.HasVoter(user3));
            Assert.IsTrue(voterStorage.HasVoter(user4));
            Assert.IsTrue(voterStorage.HasVoter(user5));
            Assert.AreEqual(6, voterStorage.Count);
        }

        [TestMethod]
        public void Check_Complex_2()
        {
            string user1 = "Kinematics";
            Origin origin1 = new Origin(user1, "123456", 100, new Uri("http://www.example.com/"), "http://www.example.com");
            string planname = "Zoom";
            var planOrigin = origin1.GetPlanOrigin(planname);
            string user2 = "Atreya";
            Origin origin2 = new Origin(user2, "123457", 101, new Uri("http://www.example.com/"), "http://www.example.com");
            string user3 = "Kimberly";
            Origin origin3 = new Origin(user3, "123458", 102, new Uri("http://www.example.com/"), "http://www.example.com");
            string user4 = "Biigoh";
            Origin origin4 = new Origin(user4, "123459", 103, new Uri("http://www.example.com/"), "http://www.example.com");
            string user5 = "Muramasa";
            Origin origin5 = new Origin(user5, "123460", 104, new Uri("http://www.example.com/"), "http://www.example.com");
            Origin simpleOrigin = new Origin(user4, IdentityType.User);

            voterStorage.Add(planOrigin, vote);
            voterStorage.Add(origin1, vote);
            voterStorage.Add(origin2, vote);
            voterStorage.Add(origin3, vote);
            voterStorage.Remove(origin1);
            voterStorage.Add(origin4, vote);
            voterStorage.Add(origin5, vote);
            voterStorage.Remove(simpleOrigin);

            Assert.IsTrue(voterStorage.HasPlan(planname));
            Assert.IsFalse(voterStorage.HasVoter(user1));
            Assert.IsTrue(voterStorage.HasVoter(user2));
            Assert.IsTrue(voterStorage.HasVoter(user3));
            Assert.IsFalse(voterStorage.HasVoter(user4));
            Assert.IsTrue(voterStorage.HasVoter(user5));
            Assert.AreEqual(4, voterStorage.Count);
        }
    }
}
