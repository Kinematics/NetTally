using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Counting;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Utility;

namespace NetTally.Tests.ComponentsF.Counting;

[TestClass]
public class VoteConstructorPlanTests
{
    static Quest sampleQuest = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        var serviceProvider = TestStartup.ConfigureServices();

        sampleQuest = new Quest
        {
            VoteCounterF = serviceProvider.GetRequiredService<IVoteCounterF>()
        };
    }

    private static OriginType GetOrigin1()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new Uri(Strings.ExampleHostUrl);
        Uri permalink = new Uri(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        int postNumber = 123;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }


    #region Base Plans
    [TestMethod]
    public void CheckForPlans_BasePlans_NoPlans()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();
        string postText =
            """
            [x] Line 1
            [x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
            sampleQuest,
            post.Origin.Author,
            VoteBlocks.IsBlockAProposedPlan,
            blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_BasePlans_NormalPlan()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();
        string postText =
            """
            [x] Plan Cyclops
            -[x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
            sampleQuest,
            post.Origin.Author,
            VoteBlocks.IsBlockAProposedPlan,
            blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_BasePlans_HasBasePlan()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Base Plan Cyclops
            -[x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAProposedPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
    }

    [TestMethod]
    public void CheckForPlans_BasePlans_HasProposedPlan()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Proposed plan: Cyclops
            -[x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAProposedPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
    }

    [TestMethod]
    public void CheckForPlans_BasePlans_ReferenceOnly()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText ="""
            [x] Base Plan Cyclops
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(1, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAProposedPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_BasePlans_WithMoreVote_1()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Proposed plan: Cyclops
            -[x] Line 2
            [x] Extra
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(3, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAProposedPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
        Assert.AreEqual(2, plans.First().Value.Count());
    }

    [TestMethod]
    public void CheckForPlans_BasePlans_WithMoreVote_2()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Before
            [x] Proposed plan: Cyclops
            -[x] Line 2
            [x] Extra
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(4, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAProposedPlan,
           blocks);

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
        var origin = GetOrigin1();

        string postText =
            """
            [x] Line 1
            [x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnExplicitPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_StandardPlans_NormalPlan()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Plan Cyclops
            -[x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnExplicitPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
    }

    [TestMethod]
    public void CheckForPlans_StandardPlans_HasBasePlan()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Base Plan Cyclops
            -[x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnExplicitPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
    }

    [TestMethod]
    public void CheckForPlans_StandardPlans_ReferenceOnly()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText = """
            [x] Plan Cyclops
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(1, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnExplicitPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_StandardPlans_WithMoreVote_1()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] plan: Cyclops
            -[x] Line 2
            [x] Extra
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(3, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnExplicitPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
        Assert.AreEqual(2, plans.First().Value.Count());
    }

    [TestMethod]
    public void CheckForPlans_StandardPlans_WithMoreVote_2()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Before
            [x] Plan: Cyclops
            -[x] Line 2
            [x] Extra
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(4, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteBlocks(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnExplicitPlan,
           blocks);

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
        var origin = GetOrigin1();

        string postText =
            """
            [x] Line 1
            [x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteAsBlock(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnImplicitPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_ImplicitPlans_NormalPlan()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Plan Cyclops
            -[x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteAsBlock(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnImplicitPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_ImplicitPlans_1()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Plan Cyclops
            [x] Line 2
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(2, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteAsBlock(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnImplicitPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
    }

    [TestMethod]
    public void CheckForPlans_ImplicitPlans_ReferenceOnly()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText = """
                [x] Plan Cyclops
                """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(1, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteAsBlock(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnImplicitPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_ImplicitPlans_WithMoreVote_1()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Plan: Cyclops
            -[x] Line 2
            [x] Extra
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(3, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteAsBlock(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnImplicitPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }

    [TestMethod]
    public void CheckForPlans_ImplicitPlans_WithMoreVote_2()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Plan: Cyclops
            [x] Line 2
            [x] Extra
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(3, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteAsBlock(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnImplicitPlan,
           blocks);

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Cyclops", plans.First().Key);
        Assert.AreEqual(3, plans.First().Value.Count());
    }

    [TestMethod]
    public void CheckForPlans_ImplicitPlans_WithMoreVote_3()
    {
        sampleQuest.PartitionMode = PartitionMode.None;
        var origin = GetOrigin1();

        string postText =
            """
            [x] Before
            [x] Plan: Cyclops
            -[x] Line 2
            [x] Extra
            """;

        var post = Post.Create(origin, postText);
        Assert.IsNotNull(post);
        Assert.IsTrue(post.HasVote);
        Assert.AreEqual(4, post.VoteLines.Count);

        var blocks = VoteCounterF.GetVoteAsBlock(post.VoteLines);

        var plans = VoteConstructor.PreprocessPostGetPlans(
           sampleQuest,
           post.Origin.Author,
           VoteBlocks.IsBlockAnImplicitPlan,
           blocks);

        Assert.AreEqual(0, plans.Count);
    }
    #endregion
}
