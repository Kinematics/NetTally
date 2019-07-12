using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class PartitioningTests
    {
        #region Setup
        static IServiceProvider serviceProvider;
        static IVoteCounter voteCounter;
        static VoteConstructor voteConstructor;
        static Tally tally;
        static IQuest quest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteCounter = serviceProvider.GetRequiredService<IVoteCounter>();
            tally = serviceProvider.GetRequiredService<Tally>();
            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest();

            voteCounter.Reset();
            voteCounter.ClearPosts();
        }
        #endregion

        #region Define post text

        readonly static string oneLine = @"[X] Run Lola Run!";
        readonly static string oneLineTask = @"[X][Movie] Run Lola Run!";
        readonly static string twoLine = @"[X] Run Lola Run!
[X] National Geographic";
        readonly static string twoLineTask = @"[X][Movie] Run Lola Run!
[X] National Geographic";
        readonly static string childLine = @"[X][Movie] Run Lola Run!
-[X] National Geographic";
        readonly static string twoChunk = @"[X][Movie] Run Lola Run!
-[X] National Geographic
[X] Gunbuster";

        #endregion


        [TestMethod]
        public void SingleLine_Partitioning_None()
        {
            string postText = oneLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.None;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[] Run Lola Run!", result[0].ToComparableString());
        }

        [TestMethod]
        public void SingleLine_Partition_ByLine()
        {
            string postText = oneLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLine;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[] Run Lola Run!", result[0].ToComparableString());
        }

        [TestMethod]
        public void SingleLine_Partition_ByBlock()
        {
            string postText = oneLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByBlock;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[] Run Lola Run!", result[0].ToComparableString());
        }

        [TestMethod]
        public void SingleLine_Partition_ByLineTask()
        {
            string postText = oneLineTask;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLineTask;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", result[0].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partitioning_None()
        {
            string postText = twoLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.None;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[] Run Lola Run!\n[] National Geographic", result[0].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partition_ByLine()
        {
            string postText = twoLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLine;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(2, result!.Count);
            Assert.AreEqual("[] Run Lola Run!", result[0].ToComparableString());
            Assert.AreEqual("[] National Geographic", result[1].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partition_ByBlock()
        {
            string postText = twoLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByBlock;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(2, result!.Count);
            Assert.AreEqual("[] Run Lola Run!", result[0].ToComparableString());
            Assert.AreEqual("[] National Geographic", result[1].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partition_ByLineTask()
        {
            string postText = twoLineTask;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLineTask;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(2, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", result[0].ToComparableString());
            Assert.AreEqual("[] National Geographic", result[1].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partitioning_None()
        {
            string postText = childLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.None;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic", result[0].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partition_ByLine()
        {
            string postText = childLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLine;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(2, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", result[0].ToComparableString());
            Assert.AreEqual("-[] National Geographic", result[1].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partition_ByBlock()
        {
            string postText = childLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByBlock;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic", result[0].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partition_ByLineTask()
        {
            string postText = childLine;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLineTask;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(2, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", result[0].ToComparableString());
            Assert.AreEqual("-[][Movie] National Geographic", result[1].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partitioning_None()
        {
            string postText = twoChunk;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.None;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(1, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic\n[] Gunbuster", result[0].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partition_ByLine()
        {
            string postText = twoChunk;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLine;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(3, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", result[0].ToComparableString());
            Assert.AreEqual("-[] National Geographic", result[1].ToComparableString());
            Assert.AreEqual("[] Gunbuster", result[2].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partition_ByBlock()
        {
            string postText = twoChunk;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByBlock;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(2, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic", result[0].ToComparableString());
            Assert.AreEqual("[] Gunbuster", result[1].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partition_ByLineTask()
        {
            string postText = twoChunk;

            Post post = new Post("Kinematics", "123456", postText, 66);
            voteCounter.Quest = quest;
            quest.PartitionMode = PartitionMode.ByLineTask;

            var result = voteConstructor.ProcessPostGetVotes(post, quest);

            Assert.AreNotEqual(null, result);
            Assert.AreEqual(3, result!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", result[0].ToComparableString());
            Assert.AreEqual("-[][Movie] National Geographic", result[1].ToComparableString());
            Assert.AreEqual("[] Gunbuster", result[2].ToComparableString());
        }
    }
}
