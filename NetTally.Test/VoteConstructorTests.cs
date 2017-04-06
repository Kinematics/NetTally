using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetTally.Tests.Platform;
using NetTally.Utility;
using NetTally.ViewModels;
using NetTally.Votes;
using NetTally.VoteCounting;

namespace NetTally.Tests
{
    [TestClass]
    public class VoteConstructorTests
    {
        static IQuest sampleQuest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            Agnostic.HashStringsUsing(UnicodeHashFunction.HashFunction);

            sampleQuest = new Quest();

            ViewModelService.Instance.Build();
        }

        [TestInitialize]
        public void Initialize()
        {
            VoteCounter.Instance.Reset();
            VoteCounter.Instance.PostsList.Clear();
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
            p.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p, sampleQuest, CancellationToken.None);

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
            p.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p, sampleQuest, CancellationToken.None);

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
            p.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p, sampleQuest, CancellationToken.None);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 7);
            Assert.IsTrue(voters.Count == 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ProcessPostContentsTallyTest()
        {
            string testVote = @"『b』Vote Tally『/b』
『color=transparent』##### NetTally 1.0『/color』
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
            p.SetWorkingVote(VoteConstructor.GetWorkingVote);

            Assert.IsFalse(p.IsVote);

            VoteConstructor.ProcessPost(p, sampleQuest, CancellationToken.None);
        }


        [TestMethod]
        public async Task ProcessPostContentsWholeWithReferralTest1()
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
            int postNumber = 100;
            PostComponents p1 = new PostComponents(author, postId, testVote, postNumber);
            p1.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p1, sampleQuest, CancellationToken.None);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            int refPostNum = 101;
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote, refPostNum);

            VoteCounter.Instance.PostsList.Add(p1);
            VoteCounter.Instance.PostsList.Add(p2);

            List<PostComponents> posts = new List<PostComponents>();
            posts.Add(p1);
            posts.Add(p2);

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 1);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public async Task ProcessPostContentsBlockWithReferralTest1()
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
            int postNumber = 100;
            PostComponents p1 = new PostComponents(author, postId, testVote, postNumber);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            int refPostNum = 101;
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote, refPostNum);

            List<PostComponents> posts = new List<PostComponents>();
            posts.Add(p1);
            posts.Add(p2);

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 3);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public async Task ProcessPostContentsLineWithReferralTest1()
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
            int postNumber = 100;
            PostComponents p1 = new PostComponents(author, postId, testVote, postNumber);

            string referralVote = @"[x] Muramasa";
            string refAuthor = "Gerbil";
            string refID = "123457";
            int refPostNum = 101;
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote, refPostNum);

            List<PostComponents> posts = new List<PostComponents>();
            posts.Add(p1);
            posts.Add(p2);

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 7);
            Assert.IsTrue(votes.All(v => v.Value.Count == 2));
            Assert.IsTrue(voters.Count == 2);
        }


        [TestMethod]
        public async Task ProcessPostContentsWholeWithReferralTest2()
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
            int postNumber = 100;
            PostComponents p1 = new PostComponents(author, postId, testVote, postNumber);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            int refPostNum = 101;
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote, refPostNum);

            List<PostComponents> posts = new List<PostComponents>();
            posts.Add(p1);
            posts.Add(p2);

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 2);
            Assert.IsTrue(votes.All(v => v.Value.Count == 1));
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public async Task ProcessPostContentsBlockWithReferralTest2()
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
            int postNumber = 100;
            PostComponents p1 = new PostComponents(author, postId, testVote, postNumber);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            int refPostNum = 101;
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote, refPostNum);

            List<PostComponents> posts = new List<PostComponents>();
            posts.Add(p1);
            posts.Add(p2);

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);
            var voters = VoteCounter.Instance.GetVotersCollection(VoteType.Vote);
            Assert.IsTrue(votes.Count == 4);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 2) == 3);
            Assert.IsTrue(votes.Count(v => v.Value.Count == 1) == 1);
            Assert.IsTrue(voters.Count == 2);
        }

        [TestMethod]
        public async Task ProcessPostContentsLineWithReferralTest2()
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
            int postNumber = 100;
            PostComponents p1 = new PostComponents(author, postId, testVote, postNumber);

            string referralVote = @"[x] Muramasa
[x] With Cake";
            string refAuthor = "Gerbil";
            string refID = "123457";
            int refPostNum = 101;
            PostComponents p2 = new PostComponents(refAuthor, refID, referralVote, refPostNum);

            List<PostComponents> posts = new List<PostComponents>();
            posts.Add(p1);
            posts.Add(p2);

            await VoteCounter.Instance.TallyPosts(posts, sampleQuest, CancellationToken.None);

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
            p1.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p1, sampleQuest, CancellationToken.None);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Agnostic.StringComparer));
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
            p1.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p1, sampleQuest, CancellationToken.None);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Agnostic.StringComparer));
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
            p1.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p1, sampleQuest, CancellationToken.None);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Agnostic.StringComparer));
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
            VoteConstructor.PreprocessPlansWithContent(p1, sampleQuest);
            p1.SetWorkingVote(VoteConstructor.GetWorkingVote);

            VoteConstructor.ProcessPost(p1, sampleQuest, CancellationToken.None);
            var votes = VoteCounter.Instance.GetVotesCollection(VoteType.Vote);

            Assert.IsTrue(votes.Keys.SequenceEqual(expected, Agnostic.StringComparer));
        }
        #endregion
    }
}
