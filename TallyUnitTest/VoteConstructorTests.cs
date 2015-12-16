using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using NetTally.Utility;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteConstructorTests
    {
        static IQuest sampleQuest;
        static VoteConstructor voteConstructor;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            sampleQuest = new Quest();
            voteConstructor = new VoteConstructor();
        }

        [TestInitialize]
        public void Initialize()
        {
            VoteCounter.Instance.Reset();
            sampleQuest.AllowRankedVotes = false;
            sampleQuest.PartitionMode = PartitionMode.None;
        }


        #region Process Posts
        [TestMethod]
        public void ProcessPostContentsWholeTest()
        {
            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";
            string author = "Muramasa";
            string postId = "123456";

            sampleQuest.PartitionMode = PartitionMode.None;
            PostComponents p = new PostComponents(author, postId, testVote);
            p.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 1);
            Assert.IsTrue(voters.Count == 1);
            //Assert.IsTrue(voteCounter.VotesWithSupporters.ContainsKey(testVote));
            //Assert.IsTrue(voteCounter.VotesWithSupporters[testVote].Contains(author));
            Assert.IsTrue(voters.ContainsKey(author));
            Assert.IsTrue(voters[author] == postId);
        }

        [TestMethod]
        public void ProcessPostContentsBlockTest()
        {
            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";

            sampleQuest.PartitionMode = PartitionMode.ByBlock;
            PostComponents p = new PostComponents(author, postId, testVote);
            p.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 3);
            Assert.IsTrue(voters.Count == 1);
        }

        [TestMethod]
        public void ProcessPostContentsLineTest()
        {
            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";

            sampleQuest.PartitionMode = PartitionMode.ByLine;
            PostComponents p = new PostComponents(author, postId, testVote);
            p.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 7);
            Assert.IsTrue(voters.Count == 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ProcessPostContentsTallyTest()
        {
            string testVote = @"[b]Vote Tally[/b]
[color=transparent]##### NetTally 1.0[/color]
[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";

            PostComponents p = new PostComponents(author, postId, testVote);
            p.SetWorkingVote(voteConstructor.GetWorkingVote);

            Assert.IsFalse(p.IsVote);

            voteConstructor.ProcessPost(p, sampleQuest);
        }


        [TestMethod]
        public void ProcessPostContentsWholeWithReferralTest1()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";
            string author = "Muramasa";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);
            p2.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 1);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public void ProcessPostContentsBlockWithReferralTest1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);
            p2.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 3);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public void ProcessPostContentsLineWithReferralTest1()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);
            p2.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 7);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }


        [TestMethod]
        public void ProcessPostContentsWholeWithReferralTest2()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";
            string author = "Muramasa";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);
            p2.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 2);
            Assert.IsTrue(votes.All(v => v.Value.Count == 1));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public void ProcessPostContentsBlockWithReferralTest2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByBlock;

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);
            p2.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 4);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 2) == 3);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 1) == 1);
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public void ProcessPostContentsLineWithReferralTest2()
        {
            sampleQuest.PartitionMode = PartitionMode.ByLine;

            string testVote = @"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            string author = "Muramasa";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);
            p2.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 8);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 2) == 7);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 1) == 1);
            Assert.IsTrue(voters.Count == 2);
        }
        #endregion

        #region Partitioning
        [TestMethod]
        public void TestPartitionNone()
        {
            string testVote =
@"[X][Action] Plan One
-[X] Ambush
-[X][Decision] Kill
-[X] Run";
            List<string> expected = new List<string> { testVote };

            sampleQuest.PartitionMode = PartitionMode.None;
            string author = "Me";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }

        [TestMethod]
        public void TestPartitionLine()
        {
            string testVote =
@"[X][Action] Plan One
-[X] Ambush
-[X][Decision] Kill
-[X] Run";

            List<string> expected = new List<string>(4)
            {
                "[X][Action] Plan One",
                "-[X] Ambush",
                "-[X][Decision] Kill",
                "-[X] Run"
            };

            sampleQuest.PartitionMode = PartitionMode.ByLine;
            string author = "Me";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }

        [TestMethod]
        public void TestPartitionBlock()
        {
            string testVote =
@"[X][Action] Plan One
-[X] Ambush
-[X][Decision] Kill
-[X] Run
[X] Plan Two
-[X] Report";

            List<string> expected = new List<string>(2)
            {
@"[X][Action] Plan One
-[X] Ambush
-[X][Decision] Kill
-[X] Run",
@"[X] Plan Two
-[X] Report"
            };

            sampleQuest.PartitionMode = PartitionMode.ByBlock;
            string author = "Me";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }

        [TestMethod]
        public void TestPartitionPlanBlock()
        {
            string testVote =
@"[X][Action] Plan One
-[X] Ambush
-[X][Decision] Kill
-[X] Run
[X] Plan Two
-[X] Report";

            List<string> expected = new List<string>(3)
            {
@"[X][Action] Ambush",
@"[X][Decision] Kill",
@"[X][Action] Run",
@"[X] Report"
            };

            sampleQuest.PartitionMode = PartitionMode.ByBlockAll;
            string author = "Me";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);
            voteConstructor.PreprocessPlansPhase1(p1, sampleQuest);
            p1.SetWorkingVote(voteConstructor.GetWorkingVote);

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }
        #endregion
    }
}
