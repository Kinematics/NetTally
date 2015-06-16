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
        public void TallyVotesTest()
        {
            var a = new VoteCounter();
            Assert.AreEqual(0, a.VoterMessageId.Count);
            Assert.AreEqual(0, a.VotesWithSupporters.Count);

            privateVote.Invoke("Reset");
        }

        [TestMethod()]
        public void GetVoteKeyTest1()
        {
            string myVote = "[x] Vote for stuff";

            string key = (string)privateVote.Invoke("GetVoteKey", myVote, sampleQuest, VoteType.Vote);
            Assert.AreEqual(myVote, key);
        }

        [TestMethod()]
        public void GetVoteKeyTest2()
        {
            string myVote = "[x] Vote for stuff";
            voteCounter.VotesWithSupporters[myVote] = new HashSet<string>() { "me" };

            string key = (string)privateVote.Invoke("GetVoteKey", myVote, sampleQuest, VoteType.Vote);
            Assert.AreEqual(myVote, key);
        }

        [TestMethod()]
        public void GetVoteKeyTest3()
        {
            string myVote = "[x] Vote for stuff";

            string key = (string)privateVote.Invoke("GetVoteKey", myVote, sampleQuest, VoteType.Vote);
            Assert.AreEqual(myVote, key);
            string key2 = (string)privateVote.Invoke("GetVoteKey", myVote, sampleQuest, VoteType.Vote);
            Assert.AreEqual(myVote, key2);

            string myBoldVote = "[x] Vote for [b]stuff[/b] ";
            string key3 = (string)privateVote.Invoke("GetVoteKey", myBoldVote, sampleQuest, VoteType.Vote);
            Assert.AreEqual(myVote, key3);
        }

        [TestMethod()]
        public void GetVoteKeyTest4()
        {
            string myVote = "[x] Vote for stuff";
            voteCounter.VotesWithSupporters[myVote] = new HashSet<string>() { "me" };

            List<string> votes = (List<string>)privateVote.Invoke("RemoveSupport", "me", VoteType.Vote);

            string key = (string)privateVote.Invoke("GetVoteKey", myVote, sampleQuest, VoteType.Vote);
            Assert.AreEqual(myVote, key);
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
        public void RemoveSupportTest1()
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
        public void RemoveSupportTest2()
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
        public void RemoveSupportTest3()
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