using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Experiment3;
using NetTally.Votes;

namespace NetTally.Tests.Experiment3
{
    [TestClass]
    public class VoteConstructorPlanTests
    {
        static IServiceProvider serviceProvider;
        static VoteConstructor voteConstructor;
        static IQuest sampleQuest;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            serviceProvider = TestStartup.ConfigureServices();

            voteConstructor = serviceProvider.GetRequiredService<VoteConstructor>();

            sampleQuest = new Quest();
        }

        #region Base Plans
        [TestMethod]
        public void CheckForPlans_BasePlans_NoPlans()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText = 
@"[x] Line 1
[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockABasePlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_BasePlans_NormalPlan()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan Cyclops
-[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockABasePlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_BasePlans_HasBasePlan()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Base Plan Cyclops
-[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockABasePlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
        }

        [TestMethod]
        public void CheckForPlans_BasePlans_HasProposedPlan()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Proposed plan: Cyclops
-[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockABasePlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
        }

        [TestMethod]
        public void CheckForPlans_BasePlans_ReferenceOnly()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Base Plan Cyclops";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(1, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockABasePlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_BasePlans_WithMoreVote_1()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Proposed plan: Cyclops
-[x] Line 2
[x] Extra";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(3, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockABasePlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
            Assert.AreEqual(2, plans.First().Value.Count());
        }

        [TestMethod]
        public void CheckForPlans_BasePlans_WithMoreVote_2()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Before
[x] Proposed plan: Cyclops
-[x] Line 2
[x] Extra";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(4, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockABasePlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
            Assert.AreEqual(2, plans.First().Value.Count());
        }
        #endregion

        #region Normal Plans
        [TestMethod]
        public void CheckForPlans_StandardPlans_NoPlans()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Line 1
[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockAnExplicitPlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_StandardPlans_NormalPlan()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan Cyclops
-[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockAnExplicitPlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
        }

        [TestMethod]
        public void CheckForPlans_StandardPlans_HasBasePlan()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Base Plan Cyclops
-[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockAnExplicitPlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
        }

        [TestMethod]
        public void CheckForPlans_StandardPlans_ReferenceOnly()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan Cyclops";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(1, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockAnExplicitPlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_StandardPlans_WithMoreVote_1()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] plan: Cyclops
-[x] Line 2
[x] Extra";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(3, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockAnExplicitPlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
            Assert.AreEqual(2, plans.First().Value.Count());
        }

        [TestMethod]
        public void CheckForPlans_StandardPlans_WithMoreVote_2()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Before
[x] Plan: Cyclops
-[x] Line 2
[x] Extra";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(4, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: true, VoteBlocks.IsBlockAnExplicitPlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
            Assert.AreEqual(2, plans.First().Value.Count());
        }
        #endregion

        #region Implicit Plans
        [TestMethod]
        public void CheckForPlans_ImplicitPlans_NoPlans()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Line 1
[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: false, VoteBlocks.IsBlockAnImplicitPlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_ImplicitPlans_NormalPlan()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan Cyclops
-[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: false, VoteBlocks.IsBlockAnImplicitPlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_ImplicitPlans_1()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan Cyclops
[x] Line 2";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(2, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: false, VoteBlocks.IsBlockAnImplicitPlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
        }

        [TestMethod]
        public void CheckForPlans_ImplicitPlans_ReferenceOnly()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan Cyclops";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(1, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: false, VoteBlocks.IsBlockAnImplicitPlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_ImplicitPlans_WithMoreVote_1()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan: Cyclops
-[x] Line 2
[x] Extra";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(3, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: false, VoteBlocks.IsBlockAnImplicitPlan);

            Assert.AreEqual(0, plans.Count);
        }

        [TestMethod]
        public void CheckForPlans_ImplicitPlans_WithMoreVote_2()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Plan: Cyclops
[x] Line 2
[x] Extra";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(3, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: false, VoteBlocks.IsBlockAnImplicitPlan);

            Assert.AreEqual(1, plans.Count);
            Assert.AreEqual("Cyclops", plans.First().Key);
            Assert.AreEqual(3, plans.First().Value.Count());
        }

        [TestMethod]
        public void CheckForPlans_ImplicitPlans_WithMoreVote_3()
        {
            sampleQuest.PartitionMode = PartitionMode.None;

            string author = "Kinematics";
            string postId = "123456";
            string postText =
@"[x] Before
[x] Plan: Cyclops
-[x] Line 2
[x] Extra";
            int postNumber = 10;

            Post post = new Post(author, postId, postText, postNumber);

            Assert.IsTrue(post.IsVote);
            Assert.AreEqual(4, post.VoteLines.Count);

            var plans = voteConstructor.PreprocessPostGetPlans(post, sampleQuest, asBlocks: false, VoteBlocks.IsBlockAnImplicitPlan);

            Assert.AreEqual(0, plans.Count);
        }
        #endregion
    }
}
