using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTally.Utility;

namespace NetTally
{
    [TestClass()]
    public class VoteConstructorTests
    {
        static IVoteCounter voteCounter;
        static IQuest sampleQuest;
        static VoteConstructor voteConstructor;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            voteCounter = new VoteCounter();
            sampleQuest = new Quest();
            voteConstructor = new VoteConstructor(voteCounter);
        }

        [TestInitialize()]
        public void Initialize()
        {
            voteCounter.Reset();
            sampleQuest.AllowRankedVotes = false;
            sampleQuest.PartitionMode = PartitionMode.None;
        }


        #region Process Posts
        [TestMethod()]
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

            voteConstructor.ProcessPost(p, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 1);
            Assert.IsTrue(voters.Count == 1);
            //Assert.IsTrue(voteCounter.VotesWithSupporters.ContainsKey(testVote));
            //Assert.IsTrue(voteCounter.VotesWithSupporters[testVote].Contains(author));
            Assert.IsTrue(voters.ContainsKey(author));
            Assert.IsTrue(voters[author] == postId);
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 3);
            Assert.IsTrue(voters.Count == 1);
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 7);
            Assert.IsTrue(voters.Count == 1);
        }

        [TestMethod()]
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

            Assert.IsFalse(p.IsVote);

            voteConstructor.ProcessPost(p, sampleQuest);
        }


        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 1);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 3);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 7);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }


        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 2);
            Assert.IsTrue(votes.All(v => v.Value.Count == 1));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 4);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 2) == 3);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 1) == 1);
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote);

            voteConstructor.ProcessPost(p2, sampleQuest);

            var votes = voteCounter.GetVotesCollection(VoteType.Vote);
            var voters = voteCounter.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 8);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 2) == 7);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 1) == 1);
            Assert.IsTrue(voters.Count == 2);
        }
        #endregion

        #region Formatting
        [TestMethod()]
        public void CloseFormattingTagsTest()
        {
            List<string> partitions = new List<string>();

            string line1 = "[x] Vote for stuff 1\r\n";
            string line2 = "[x] Vote for stuff 2\r\n";
            string line2a = "[x] Vote for [b]stuff[/b] 2\r\n";
            string line3 = "[x] Vote for stuff 3\r\n";
            string line3a = "[color=blue][x] Vote for stuff 3[/color]\r\n";
            string line4 = "[x] Vote for stuff 4\r\n";
            string line4a = "[color=blue][x] Vote for stuff 4\r\n";
            string line4b = "[color=blue][x] Vote for stuff 4[/color]\r\n";
            string line5 = "[x] Vote for stuff 5\r\n";
            string line5a = "[color=blue][b][x] Vote for stuff 5[/color]\r\n";
            string line5b = "[color=blue][b][x] Vote for stuff 5[/color][/b]\r\n";

            string line6 = "[x] Vote for stuff 6\r\n";
            string line6a = "[x] Vote for stuff 6[/b]\r\n";
            string line7 = "[x] [b]Vote[/b] for stuff 7\r\n";
            string line7a = "[x] [b]Vote[/b] for stuff 7[/b]\r\n";
            string line7b = "[b][x] [b]Vote[/b] for stuff 7[/b]\r\n";
            string line7c = "[x] [b]Vote for stuff 7[/b]\r\n";
            string line8 = "[x] [i]Vote[/i] for stuff 8\r\n";
            string line8a = "[x] [i]Vote[/i] for stuff 8[/color]\r\n";


            partitions.Add(line1);
            partitions.Add(line2a);
            partitions.Add(line3a);
            partitions.Add(line4a);
            partitions.Add(line5a);
            partitions.Add(line6a);
            partitions.Add(line7a);
            partitions.Add(line8a);

            voteConstructor.CloseFormattingTags(partitions);

            Assert.AreEqual(8, partitions.Count);
            Assert.IsTrue(partitions.Contains(line1));
            Assert.IsTrue(partitions.Contains(line2a));
            Assert.IsTrue(partitions.Contains(line3a));
            Assert.IsTrue(partitions.Contains(line4b));
            Assert.IsTrue(partitions.Contains(line5b));
            Assert.IsTrue(partitions.Contains(line6));
            Assert.IsTrue(partitions.Contains(line7c));
            Assert.IsTrue(partitions.Contains(line8));

            Assert.IsFalse(partitions.Contains(line2));
            Assert.IsFalse(partitions.Contains(line3));
            Assert.IsFalse(partitions.Contains(line4));
            Assert.IsFalse(partitions.Contains(line4a));
            Assert.IsFalse(partitions.Contains(line5));
            Assert.IsFalse(partitions.Contains(line5a));
            Assert.IsFalse(partitions.Contains(line6a));
            Assert.IsFalse(partitions.Contains(line7));
            Assert.IsFalse(partitions.Contains(line7a));
            Assert.IsFalse(partitions.Contains(line7b));
            Assert.IsFalse(partitions.Contains(line8a));
        }
        #endregion

        #region Partitioning
        [TestMethod()]
        public void TestPartitionNone()
        {
            string testVote =
@"[X][Action] Plan One
-[X] Ambush
-[X][Decision] Kill
-[X] Run";
            List<string> expected = new List<string>() { testVote };

            sampleQuest.PartitionMode = PartitionMode.None;
            string author = "Me";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = voteCounter.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = voteCounter.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }

        [TestMethod()]
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

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = voteCounter.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }


        [TestMethod()]
        public void TestPartitionTask()
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
-[X] Ambush",
@"-[X][Decision] Kill
-[X] Run
[X] Plan Two
-[X] Report"
            };

            sampleQuest.PartitionMode = PartitionMode.ByTask;
            string author = "Me";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = voteCounter.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }

        [TestMethod()]
        public void TestPartitionTaskBlock()
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
@"[X][Action] Plan One
-[X] Ambush",
@"-[X][Decision] Kill
-[X] Run",
@"[X][Decision] Plan Two
-[X] Report"
            };

            sampleQuest.PartitionMode = PartitionMode.ByTaskBlock;
            string author = "Me";
            string postId = "123456";
            PostComponents p1 = new PostComponents(author, postId, testVote);

            voteConstructor.ProcessPost(p1, sampleQuest);
            var votes = voteCounter.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Text.AgnosticStringComparer));
        }
        #endregion
    }
}
