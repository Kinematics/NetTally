using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.Components;
using NetTally.VoteCounting;
using NetTally.Votes;

namespace NetTally.Tests.Votes
{
    [TestClass]
    public class PartitioningTests
    {
        #region Setup
        static IServiceProvider serviceProvider = null!;
        static Quest quest = null!;
        static Origin origin = null!;

        [ClassInitialize]
        public static void ClassInit(TestContext _)
        {
            serviceProvider = TestStartup.ConfigureServices();
            origin = new("Kinematics", "123456", 10, new Uri("http://www.example.com/"), "http://www.example.com");
        }

        [TestInitialize]
        public void Initialize()
        {
            quest = new Quest
            {
                VoteCounter = serviceProvider.GetRequiredService<IVoteCounter>()
            };
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
            Post post = new(origin, oneLine);
            quest.PartitionMode = PartitionMode.None;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes.Count);
            Assert.AreEqual("[] Run Lola Run!", votes[0].ToComparableString());
        }

        [TestMethod]
        public void SingleLine_Partition_ByLine()
        {
            Post post = new(origin, oneLine);
            quest.PartitionMode = PartitionMode.ByLine;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes!.Count);
            Assert.AreEqual("[] Run Lola Run!", votes[0].ToComparableString());
        }

        [TestMethod]
        public void SingleLine_Partition_ByBlock()
        {
            Post post = new(origin, oneLine);
            quest.PartitionMode = PartitionMode.ByBlock;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes!.Count);
            Assert.AreEqual("[] Run Lola Run!", votes[0].ToComparableString());
        }

        [TestMethod]
        public void SingleLine_Partition_ByLineTask()
        {
            Post post = new(origin, oneLineTask);
            quest.PartitionMode = PartitionMode.ByLineTask;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", votes[0].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partitioning_None()
        {
            Post post = new(origin, twoLine);
            quest.PartitionMode = PartitionMode.None;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes!.Count);
            Assert.AreEqual("[] Run Lola Run!\n[] National Geographic", votes[0].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partition_ByLine()
        {
            Post post = new(origin, twoLine);
            quest.PartitionMode = PartitionMode.ByLine;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(2, votes!.Count);
            Assert.AreEqual("[] Run Lola Run!", votes[0].ToComparableString());
            Assert.AreEqual("[] National Geographic", votes[1].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partition_ByBlock()
        {
            Post post = new(origin, twoLine);
            quest.PartitionMode = PartitionMode.ByBlock;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(2, votes!.Count);
            Assert.AreEqual("[] Run Lola Run!", votes[0].ToComparableString());
            Assert.AreEqual("[] National Geographic", votes[1].ToComparableString());
        }

        [TestMethod]
        public void TwoLine_Partition_ByLineTask()
        {
            Post post = new(origin, twoLineTask);
            quest.PartitionMode = PartitionMode.ByLineTask;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(2, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", votes[0].ToComparableString());
            Assert.AreEqual("[] National Geographic", votes[1].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partitioning_None()
        {
            Post post = new(origin, childLine);
            quest.PartitionMode = PartitionMode.None;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic", votes[0].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partition_ByLine()
        {
            Post post = new(origin, childLine);
            quest.PartitionMode = PartitionMode.ByLine;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(2, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", votes[0].ToComparableString());
            Assert.AreEqual("-[] National Geographic", votes[1].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partition_ByBlock()
        {
            Post post = new(origin, childLine);
            quest.PartitionMode = PartitionMode.ByBlock;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic", votes[0].ToComparableString());
        }

        [TestMethod]
        public void ChildLine_Partition_ByLineTask()
        {
            Post post = new(origin, childLine);
            quest.PartitionMode = PartitionMode.ByLineTask;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(2, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", votes[0].ToComparableString());
            Assert.AreEqual("-[][Movie] National Geographic", votes[1].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partitioning_None()
        {
            Post post = new(origin, twoChunk);
            quest.PartitionMode = PartitionMode.None;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(1, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic\n[] Gunbuster", votes[0].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partition_ByLine()
        {
            Post post = new(origin, twoChunk);
            quest.PartitionMode = PartitionMode.ByLine;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(3, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", votes[0].ToComparableString());
            Assert.AreEqual("-[] National Geographic", votes[1].ToComparableString());
            Assert.AreEqual("[] Gunbuster", votes[2].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partition_ByBlock()
        {
            Post post = new(origin, twoChunk);
            quest.PartitionMode = PartitionMode.ByBlock;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(2, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!\n-[] National Geographic", votes[0].ToComparableString());
            Assert.AreEqual("[] Gunbuster", votes[1].ToComparableString());
        }

        [TestMethod]
        public void TwoChunk_Partition_ByLineTask()
        {
            Post post = new(origin, twoChunk);
            quest.PartitionMode = PartitionMode.ByLineTask;

            var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

            Assert.IsTrue(processed);
            Assert.AreNotEqual(null, votes);
            Assert.AreEqual(3, votes!.Count);
            Assert.AreEqual("[][Movie] Run Lola Run!", votes[0].ToComparableString());
            Assert.AreEqual("-[][Movie] National Geographic", votes[1].ToComparableString());
            Assert.AreEqual("[] Gunbuster", votes[2].ToComparableString());
        }
    }
}
