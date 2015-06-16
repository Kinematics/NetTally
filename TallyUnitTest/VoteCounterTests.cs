using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace NetTally.Tests
{
    [TestClass()]
    public class VoteCounterTests
    {
        static VoteCounter voteCounter;
        static PrivateObject privateVote;
        static IQuest sampleQuest;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            voteCounter = new VoteCounter();
            privateVote = new PrivateObject(voteCounter);
            sampleQuest = new Quest();
        }

        [TestInitialize()]
        public void Initialize()
        {
            voteCounter.Reset();
        }

        [TestMethod()]
        public void ResetTest()
        {
            //TODO

            voteCounter.Reset();

            Assert.AreEqual(0, voteCounter.VoterMessageId.Count);
            Assert.AreEqual(0, voteCounter.VotesWithSupporters.Count);
            Assert.AreEqual(0, voteCounter.RankedVoterMessageId.Count);
            Assert.AreEqual(0, voteCounter.RankedVotesWithSupporters.Count);
            Assert.AreEqual(0, voteCounter.PlanNames.Count);
            Assert.AreEqual("", voteCounter.Title);
        }

        [TestMethod()]
        public void TallyVotesTest()
        {
            //TODO
        }

        [TestMethod()]
        public void GetVotesCollectionTest1()
        {
            Assert.AreEqual(voteCounter.VotesWithSupporters, voteCounter.GetVotesCollection(VoteType.Vote));
        }

        [TestMethod()]
        public void GetVotesCollectionTest2()
        {
            Assert.AreEqual(voteCounter.VotesWithSupporters, voteCounter.GetVotesCollection(VoteType.Plan));
        }

        [TestMethod()]
        public void GetVotesCollectionTest3()
        {
            Assert.AreEqual(voteCounter.RankedVotesWithSupporters, voteCounter.GetVotesCollection(VoteType.Rank));
        }

        [TestMethod()]
        public void GetVotersCollectionTest1()
        {
            Assert.AreEqual(voteCounter.VoterMessageId, voteCounter.GetVotersCollection(VoteType.Vote));
        }

        [TestMethod()]
        public void GetVotersCollectionTest2()
        {
            Assert.AreEqual(voteCounter.VoterMessageId, voteCounter.GetVotersCollection(VoteType.Plan));
        }

        [TestMethod()]
        public void GetVotersCollectionTest3()
        {
            Assert.AreEqual(voteCounter.RankedVoterMessageId, voteCounter.GetVotersCollection(VoteType.Rank));
        }


        [TestMethod()]
        public void AddVoteSupportTest1()
        {
            string voteLine = "[x] First test";
            string voter = "me";
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVoteSupport(voteLine, voter, voteType, sampleQuest);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter));
        }

        [TestMethod()]
        public void AddVoteSupportTest2()
        {
            string voteLine = "[x] First test";
            string voter = "me";
            VoteType voteType = VoteType.Plan;

            voteCounter.AddVoteSupport(voteLine, voter, voteType, sampleQuest);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter));
        }

        [TestMethod()]
        public void AddVoteSupportTest3()
        {
            string voteLine = "[1] First test";
            string voter = "me";
            VoteType voteType = VoteType.Rank;

            voteCounter.AddVoteSupport(voteLine, voter, voteType, sampleQuest);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter));
        }

        [TestMethod()]
        public void AddVoteSupportTest4()
        {
            string voteLine = "[x] First test";
            string voter1 = "me";
            string voter2 = "me2";
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVoteSupport(voteLine, voter1, voteType, sampleQuest);
            voteCounter.AddVoteSupport(voteLine, voter2, voteType, sampleQuest);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter2));
        }

        [TestMethod()]
        public void RemoveSupportTest1()
        {
            string voteLine = "[x] First test";
            string voter = "me";
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVoteSupport(voteLine, voter, voteType, sampleQuest);
            voteCounter.RemoveSupport(voter, voteType);

            Assert.AreEqual(0, voteCounter.VotesWithSupporters.Count);
        }

        [TestMethod()]
        public void RemoveSupportTest2()
        {
            string voteLine = "[x] First test";
            string voter1 = "me";
            string voter2 = "me2";
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVoteSupport(voteLine, voter1, voteType, sampleQuest);
            voteCounter.AddVoteSupport(voteLine, voter2, voteType, sampleQuest);

            voteCounter.RemoveSupport(voter1, voteType);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(voteLine));
            Assert.IsFalse(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter2));
        }

        [TestMethod()]
        public void RemoveSupportTest3()
        {
            string voteLine = "[x] First test";
            string voter1 = "me";
            string voter2 = "me2";
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVoteSupport(voteLine, voter1, voteType, sampleQuest);
            voteCounter.AddVoteSupport(voteLine, voter2, voteType, sampleQuest);

            voteCounter.RemoveSupport("you", voteType);

            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(voteLine));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[voteLine].Contains(voter2));
        }


        [TestMethod()]
        public void AddVoteSupportTest5()
        {
            string vote1 = "[x] First test";
            string voter1 = "me";
            string vote2 = "[x] First [b]test[/b]";
            string voter2 = "me2";
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVoteSupport(vote1, voter1, voteType, sampleQuest);
            voteCounter.AddVoteSupport(vote2, voter2, voteType, sampleQuest);

            Assert.AreEqual(1, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(vote1));
            Assert.IsFalse(voteCounter.GetVotesCollection(voteType).ContainsKey(vote2));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[vote1].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[vote1].Contains(voter2));
        }

        [TestMethod()]
        public void AddVoteSupportTest6()
        {
            string vote1 = "[x] First test";
            string voter1 = "me";
            string vote2 = "[x] First [b]test[/b]";
            string voter2 = "me2";
            VoteType voteType = VoteType.Vote;

            voteCounter.AddVoteSupport(vote1, voter1, voteType, sampleQuest);
            voteCounter.RemoveSupport(voter1, voteType);
            voteCounter.AddVoteSupport(vote2, voter2, voteType, sampleQuest);

            Assert.AreEqual(1, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType).ContainsKey(vote1));
            Assert.IsFalse(voteCounter.GetVotesCollection(voteType).ContainsKey(vote2));
            Assert.IsFalse(voteCounter.GetVotesCollection(voteType)[vote1].Contains(voter1));
            Assert.IsTrue(voteCounter.GetVotesCollection(voteType)[vote1].Contains(voter2));
        }

        
        [TestMethod()]
        public void FindVotesForVoterTest1()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "you" };

            var votes = voteCounter.GetVotesFromReference("[x] me");
            Assert.AreEqual(1, votes.Count);
            Assert.IsTrue(votes.Contains(vote1));
        }

        [TestMethod()]
        public void FindVotesForVoterTest2()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "me", "you" };

            var votes = voteCounter.GetVotesFromReference("[x] me");
            Assert.AreEqual(2, votes.Count);
            Assert.IsTrue(votes.Contains(vote1));
            Assert.IsTrue(votes.Contains(vote2));
        }

        [TestMethod()]
        public void RemoveSupportTest4()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me", "you" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "him" };

            voteCounter.RemoveSupport("me", VoteType.Vote);
            Assert.AreEqual(2, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote1));
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote2));
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote1].Count == 1);
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Count == 1);
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote1].Contains("you"));
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Contains("him"));
        }

        [TestMethod()]
        public void RemoveSupportTest5()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "you" };

            voteCounter.RemoveSupport("me", VoteType.Vote);
            Assert.AreEqual(1, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote2));
        }

        [TestMethod()]
        public void RemoveSupportTest6()
        {
            string vote1 = "[x] Vote for stuff 1";
            string vote2 = "[x] Vote for stuff 2";
            voteCounter.VotesWithSupporters[vote1] = new HashSet<string>() { "me" };
            voteCounter.VotesWithSupporters[vote2] = new HashSet<string>() { "me", "you" };

            voteCounter.RemoveSupport("me", VoteType.Vote);
            Assert.AreEqual(1, voteCounter.VotesWithSupporters.Count);
            Assert.IsTrue(voteCounter.VotesWithSupporters.Keys.Contains(vote2));
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Count == 1);
            Assert.IsTrue(voteCounter.VotesWithSupporters[vote2].Contains("you"));
        }




    }
}