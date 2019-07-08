using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class VoteConstructorVoteTests
    {
        static IServiceProvider serviceProvider;
        static VoteConstructor voteConstructor;
        static IVoteCounter voteCounter;
        static IQuest quest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();
            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();

            quest = new Quest();
            voteCounter.Quest = quest;
        }

        #region Sample Posts
        Post GetPost1()
        {
            string author = "Kinematics";
            string postId = "123456";
            int postNumber = 10;
            string postText =
@"Tentative vote idea:
[x][Action] Go to the warehouse~

But might include something else...
[x] Loot the boxes";

            Post post = new Post(author, postId, postText, postNumber);
            post.WorkingVoteLines.AddRange(post.VoteLines);

            return post;
        }

        Post GetPost2()
        {
            string author = "Karma1";
            string postId = "123457";
            int postNumber = 11;
            string postText =
@"I agree.
[x][Action] Go to the warehouse~
[x] Loot the boxes";

            Post post = new Post(author, postId, postText, postNumber);
            post.WorkingVoteLines.AddRange(post.VoteLines);

            return post;
        }

        Post GetPost3()
        {
            string author = "Quincy";
            string postId = "123458";
            int postNumber = 12;
            string postText =
@"I have a better idea.
[x][Action] Go to the docks
-[x] With the motorcycle
[x] And catch them in the act.";

            Post post = new Post(author, postId, postText, postNumber);
            post.WorkingVoteLines.AddRange(post.VoteLines);

            return post;
        }

        (string name, VoteLineBlock block) GetBasePlan1()
        {
            VoteLine line1 = new VoteLine("", "X", "", "Base Plan Sound of Music", MarkerType.Vote, 100);
            VoteLine line2 = new VoteLine("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
            VoteLine line3 = new VoteLine("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
            VoteLine line4 = new VoteLine("-", "X", "", "Return home", MarkerType.Vote, 100);

            List<VoteLine> lines = new List<VoteLine>() { line1, line2, line3, line4 };

            VoteLineBlock block = new VoteLineBlock(lines);

            return ("Sound of Music", block);
        }

        (string name, VoteLineBlock block) GetBasePlan2()
        {
            VoteLine line1 = new VoteLine("", "X", "", "Proposed Plan: Sound of Music", MarkerType.Vote, 100);
            VoteLine line2 = new VoteLine("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
            VoteLine line3 = new VoteLine("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
            VoteLine line4 = new VoteLine("-", "X", "", "Return home", MarkerType.Vote, 100);

            List<VoteLine> lines = new List<VoteLine>() { line1, line2, line3, line4 };

            VoteLineBlock block = new VoteLineBlock(lines);

            return ("Sound of Music", block);
        }

        (string name, VoteLineBlock block) GetBasePlan3()
        {
            VoteLine line1 = new VoteLine("", "X", "", "Plan Sound of Music", MarkerType.Vote, 100);
            VoteLine line2 = new VoteLine("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
            VoteLine line3 = new VoteLine("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
            VoteLine line4 = new VoteLine("-", "X", "", "Return home", MarkerType.Vote, 100);

            List<VoteLine> lines = new List<VoteLine>() { line1, line2, line3, line4 };

            VoteLineBlock block = new VoteLineBlock(lines);

            return ("Sound of Music", block);
        }
        #endregion


        [TestMethod]
        public void Process_Post1_NoPartitioning()
        {
            Post post = GetPost1();
            quest.PartitionMode = PartitionMode.None;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(2, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post2_NoPartitioning()
        {
            Post post = GetPost2();
            quest.PartitionMode = PartitionMode.None;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(2, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_NoPartitioning()
        {
            Post post = GetPost3();
            quest.PartitionMode = PartitionMode.None;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(3, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post1_BlockPartitioning()
        {
            Post post = GetPost1();
            quest.PartitionMode = PartitionMode.ByBlock;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post2_BlockPartitioning()
        {
            Post post = GetPost2();
            quest.PartitionMode = PartitionMode.ByBlock;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_BlockPartitioning()
        {
            Post post = GetPost3();
            quest.PartitionMode = PartitionMode.ByBlock;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(2, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post1_LinePartitioning()
        {
            Post post = GetPost1();
            quest.PartitionMode = PartitionMode.ByLine;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post2_LinePartitioning()
        {
            Post post = GetPost2();
            quest.PartitionMode = PartitionMode.ByLine;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }

        [TestMethod]
        public void Process_Post3_LinePartitioning()
        {
            Post post = GetPost3();
            quest.PartitionMode = PartitionMode.ByLine;

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(3, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);
        }


        [TestMethod]
        public void Process_Post3_LinePartitioning_TaskFilter()
        {
            Post post = GetPost3();
            quest.PartitionMode = PartitionMode.ByLine;
            quest.UseCustomTaskFilters = true;
            quest.CustomTaskFilters = "Action";

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(1, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);

            quest.UseCustomTaskFilters = false;
            quest.CustomTaskFilters = "";
        }

        [TestMethod]
        public void Process_Post3_LineTaskPartitioning_TaskFilter()
        {
            Post post = GetPost3();
            quest.PartitionMode = PartitionMode.ByLineTask;
            quest.UseCustomTaskFilters = true;
            quest.CustomTaskFilters = "Action";

            var results = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, results);
            Assert.AreEqual(2, results!.Count);
            Assert.AreEqual("Action", results.First().Task);
            Assert.AreEqual(1, results!.First().Lines.Count);

            quest.UseCustomTaskFilters = false;
            quest.CustomTaskFilters = "";
        }

        [TestMethod]
        public void Normalize_1()
        {
            var (name, block) = GetBasePlan1();

            var (outName, results) = voteConstructor.NormalizePlan(name, block);

            Assert.AreEqual(name, outName);
            Assert.AreEqual("", results.Task);
            Assert.AreEqual(4, results.Lines.Count);
            Assert.AreEqual("Plan: Sound of Music", results.Lines.First().CleanContent);
        }

        [TestMethod]
        public void Normalize_2()
        {
            var (name, block) = GetBasePlan2();

            var (outName, results) = voteConstructor.NormalizePlan(name, block);

            Assert.AreEqual(name, outName);
            Assert.AreEqual("", results.Task);
            Assert.AreEqual(4, results.Lines.Count);
            Assert.AreEqual("Plan: Sound of Music", results.Lines.First().CleanContent);
        }

        [TestMethod]
        public void Normalize_3()
        {
            var (name, block) = GetBasePlan3();

            var (outName, results) = voteConstructor.NormalizePlan(name, block);

            Assert.AreEqual(name, outName);
            Assert.AreEqual("", results.Task);
            Assert.AreEqual(4, results.Lines.Count);
            Assert.AreEqual("Plan Sound of Music", results.Lines.First().CleanContent);
        }

    }
}
