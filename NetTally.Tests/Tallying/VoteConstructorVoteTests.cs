using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Tally.Components;
using NetTally.Types.Enums;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Tallying
{
    [TestClass]
    public class VoteConstructorVoteTests
    {
        #region Setup
        static IServiceProvider serviceProvider = null!;
        static IVoteCounter voteCounter = null!;
        static Quest quest = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            serviceProvider = TestStartup.ConfigureServices();
            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
        }

        [TestInitialize]
        public void TestInit()
        {
            quest = new Quest
            {
                VoteCounter = voteCounter
            };
        }
        #endregion

        #region Sample Posts
        static Post GetPost1()
        {
            Origin origin = new("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
            string postText =
@"Tentative vote idea:
[x][Action] Go to the warehouse~

But might include something else...
[x] Loot the boxes";

            Post post = new(origin, postText);
            VoteConstructor.ConfigureWorkingVote(post, quest);

            return post;
        }

        static Post GetPost2()
        {
            Origin origin = new("Karma1", "123457", 11, new Uri("http://www.example.com/"), "http://www.example.com");
            string postText =
@"I agree.
[x][Action] Go to the warehouse~
[x] Loot the boxes";

            Post post = new(origin, postText);
            VoteConstructor.ConfigureWorkingVote(post, quest);

            return post;
        }

        static Post GetPost3()
        {
            Origin origin = new("Quincy", "123458", 12, new Uri("http://www.example.com/"), "http://www.example.com");
            string postText =
@"I have a better idea.
[x][Action] Go to the docks
-[x] With the motorcycle
[x] And catch them in the act.";

            Post post = new(origin, postText);
            VoteConstructor.ConfigureWorkingVote(post, quest);

            return post;
        }

        static Post GetPost4()
        {
            Origin origin = new("Muramasa", "9321568", 8816, new Uri("http://www.example.com/"), "http://www.example.com");
            string postText =
@"[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            Post post = new(origin, postText);
            VoteConstructor.ConfigureWorkingVote(post, quest);

            return post;
        }

        static Post GetPost5()
        {
            Origin origin = new("Muramasa", "9321568", 8816, new Uri("http://www.example.com/"), "http://www.example.com");
            string postText =
@"『b』Vote Tally『/b』
『color=transparent』##### NetTally 1.0『/color』
[x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
[x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
[x] Head over to Oriko's.
-[x] 20 minutes roof hopping practice. Then fly the rest of the way.
-[x] Cleansing.
-[x] Take both of them food shopping (or whoever wants to go.)
-[x] Light conversation. No need for serious precog questions right now.";

            Post post = new(origin, postText);
            VoteConstructor.ConfigureWorkingVote(post, quest);

            return post;
        }

        static (string name, VoteLineBlock block) GetBasePlan1()
        {
            VoteLine line1 = new("", "X", "", "Base Plan Sound of Music", MarkerType.Vote, 100);
            VoteLine line2 = new("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
            VoteLine line3 = new("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
            VoteLine line4 = new("-", "X", "", "Return home", MarkerType.Vote, 100);

            List<VoteLine> lines = [line1, line2, line3, line4];

            VoteLineBlock block = new(lines);

            return ("Sound of Music", block);
        }

        static (string name, VoteLineBlock block) GetBasePlan2()
        {
            VoteLine line1 = new("", "X", "", "Proposed Plan: Sound of Music", MarkerType.Vote, 100);
            VoteLine line2 = new("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
            VoteLine line3 = new("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
            VoteLine line4 = new("-", "X", "", "Return home", MarkerType.Vote, 100);

            List<VoteLine> lines = [line1, line2, line3, line4];

            VoteLineBlock block = new(lines);

            return ("Sound of Music", block);
        }

        static (string name, VoteLineBlock block) GetBasePlan3()
        {
            VoteLine line1 = new("", "X", "", "Plan Sound of Music", MarkerType.Vote, 100);
            VoteLine line2 = new("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
            VoteLine line3 = new("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
            VoteLine line4 = new("-", "X", "", "Return home", MarkerType.Vote, 100);

            List<VoteLine> lines = [line1, line2, line3, line4];

            VoteLineBlock block = new(lines);

            return ("Sound of Music", block);
        }
        #endregion

        #region Test Sample Posts
        [TestMethod]
        public void Process_Post1_NoPartitioning()
        {
            quest.PartitionMode = PartitionMode.None;
            Post post = GetPost1();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(2, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post2_NoPartitioning()
        {
            quest.PartitionMode = PartitionMode.None;
            Post post = GetPost2();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(2, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_NoPartitioning()
        {
            quest.PartitionMode = PartitionMode.None;
            Post post = GetPost3();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(3, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post1_BlockPartitioning()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            Post post = GetPost1();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post2_BlockPartitioning()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            Post post = GetPost2();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_BlockPartitioning()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            Post post = GetPost3();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(2, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post1_LinePartitioning()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            Post post = GetPost1();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post2_LinePartitioning()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            Post post = GetPost2();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_LinePartitioning()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            Post post = GetPost3();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(3, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_LinePartitioning_TaskFilter()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            quest.UseCustomTaskFilters = true;
            quest.CustomTaskFilters = "Action";
            Post post = GetPost3();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_LineTaskPartitioning_TaskFilter()
        {
            quest.PartitionMode = PartitionMode.ByLineTask;
            quest.UseCustomTaskFilters = true;
            quest.CustomTaskFilters = "Action";
            Post post = GetPost3();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Normalize_1()
        {
            var (name, block) = GetBasePlan1();

            var (outName, results) = VoteConstructor.NormalizePlan(name, block);

            Assert.AreEqual(name, outName);
            Assert.AreEqual("", results.Task);
            Assert.AreEqual(4, results.Lines.Count);
            Assert.AreEqual("Plan: Sound of Music", results.Lines[0].CleanContent);
        }

        [TestMethod]
        public void Normalize_2()
        {
            var (name, block) = GetBasePlan2();

            var (outName, results) = VoteConstructor.NormalizePlan(name, block);

            Assert.AreEqual(name, outName);
            Assert.AreEqual("", results.Task);
            Assert.AreEqual(4, results.Lines.Count);
            Assert.AreEqual("Plan: Sound of Music", results.Lines[0].CleanContent);
        }

        [TestMethod]
        public void Normalize_3()
        {
            var (name, block) = GetBasePlan3();

            var (outName, results) = VoteConstructor.NormalizePlan(name, block);

            Assert.AreEqual(name, outName);
            Assert.AreEqual("", results.Task);
            Assert.AreEqual(4, results.Lines.Count);
            Assert.AreEqual("Plan Sound of Music", results.Lines[0].CleanContent);
        }
        #endregion

        #region Test More Posts
        [TestMethod]
        public void Process_Post4_ByLine()
        {
            quest.PartitionMode = PartitionMode.ByLine;
            Post post = GetPost4();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(7, results!.Count);
            Assert.AreEqual("", results[0].Task);
            Assert.AreEqual(1, results[0].Lines.Count);
        }

        [TestMethod]
        public void Process_Post4_ByBlock()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            Post post = GetPost4();

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(3, results!.Count);
            Assert.AreEqual("", results.First().Task);
            Assert.AreEqual(1, results[0].Lines.Count);
            Assert.AreEqual(1, results[1].Lines.Count);
            Assert.AreEqual(5, results[2].Lines.Count);
        }

        [TestMethod]
        public void Process_Post5_ByBlock()
        {
            quest.PartitionMode = PartitionMode.ByBlock;
            Post post = GetPost5();

            Assert.IsFalse(post.HasVote);

            var results = VoteConstructor.ProcessPostGetVotes(post, quest);

            if (results != null)
                Assert.AreEqual(0, results.Count);

            Assert.AreNotEqual(null, results);
        }
        #endregion
    }
}
