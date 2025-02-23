using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.Components.Counting;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Votes;
using NetTally.Utility;

namespace NetTally.Tests.Components.Counting;
[TestClass]
public class VoteBlocksTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    private static OriginType GetOrigin1()
    {
        var author = Authors.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        int postNumber = 123;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    [TestMethod]
    public void GetBlocks_Empty()
    {
        List<VoteLineType> lines = [];

        var blocks = VoteBlocks.GetBlocks(lines);
        Assert.IsNotNull(blocks);

        Assert.AreEqual(0, blocks.Count());
    }

    [TestMethod]
    public void GetBlocks_OneLine_OneBlock()
    {
        var origin = GetOrigin1();
        var text = """
            [X] First action
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteBlocks.GetBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);
        Assert.AreEqual(1, blocks.Count());
    }

    [TestMethod]
    public void GetBlocks_TwoLines_OneBlock()
    {
        var origin = GetOrigin1();
        var text = """
            [X] First action
            -[X] With detail
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteBlocks.GetBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);
        Assert.AreEqual(1, blocks.Count());
        Assert.AreEqual(2, blocks.First().LineCount);
    }

    [TestMethod]
    public void GetBlocks_TwoLines_TwoBlocks()
    {
        var origin = GetOrigin1();
        var text = """
            [X] First action
            [X] Second action
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteBlocks.GetBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);
        Assert.AreEqual(2, blocks.Count());
        Assert.AreEqual(1, blocks.First().LineCount);
    }

    [TestMethod]
    public void ContentBlock_Fail()
    {
        var origin = GetOrigin1();
        var text = """
            [X] First action
            [X] Second action
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsThisAContentBlock(blocks[0]));
        Assert.IsFalse(VoteBlocks.IsThisAContentBlock(blocks[1]));
    }

    [TestMethod]
    public void ContentBlock_Prefix_Fail()
    {
        var origin = GetOrigin1();
        var text = """
            -[X] First action
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsThisAContentBlock(blocks[0]));
    }

    [TestMethod]
    public void ContentBlock_Pass()
    {
        var origin = GetOrigin1();
        var text = """
            [X] First action
            -[X] With detail
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsTrue(VoteBlocks.IsThisAContentBlock(blocks[0]));
    }

    [TestMethod]
    public void ProposedPlan_NotPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] First action
            -[X] With detail
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAProposedPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ProposedPlan_NotProposedPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAProposedPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ProposedPlan_NotProposedPlan_Implicit()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            [X] First step
            [X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAProposedPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ProposedPlan_ProposedPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Proposed Plan Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsTrue(VoteBlocks.IsBlockAProposedPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ProposedPlan_ProposedBasePlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Base Plan Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsTrue(VoteBlocks.IsBlockAProposedPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ProposedPlan_SingleLinePlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Proposed Plan Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAProposedPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ExplicitPlan_NotAPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAnExplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ExplicitPlan_ExplicitPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsTrue(VoteBlocks.IsBlockAnExplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ExplicitPlan_ImplicitPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            [X] First step
            [X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAnExplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ExplicitPlan_SingleLinePlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAnExplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ImplicitPlan_NotAPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAnImplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ImplicitPlan_ExplicitPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAnImplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ImplicitPlan_ImplicitPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            [X] First step
            [X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsTrue(VoteBlocks.IsBlockAnImplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ImplicitPlan_ImplicitPlanDouble()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            [X] Plan Stop!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAnImplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void ImplicitPlan_SingleLinePlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockAnImplicitPlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void SingleLinePlan_NotAPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockASingleLinePlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void SingleLinePlan_NotAPlanSingle()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockASingleLinePlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void SingleLinePlan_ExplicitPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            -[X] First step
            -[X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockASingleLinePlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void SingleLinePlan_ImplicitPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            [X] First step
            [X] Second step
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsFalse(VoteBlocks.IsBlockASingleLinePlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void SingleLinePlan_SingleLinePlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var blocks = VoteCounter.GetVoteAsBlock(post.VoteLines);
        Assert.IsNotNull(blocks);

        Assert.IsTrue(VoteBlocks.IsBlockASingleLinePlan(blocks[0]).IsPlan);
    }

    [TestMethod]
    public void CheckIfPlan_NotAPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var line = post.VoteLines[0];
        var result = VoteBlocks.CheckIfPlan(line);

        Assert.AreEqual(PlanStatus.None, result.PlanStatus);
    }

    [TestMethod]
    public void CheckIfPlan_ProposedPlan()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Proposed Plan Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var line = post.VoteLines[0];
        var result = VoteBlocks.CheckIfPlan(line);

        Assert.AreEqual(PlanStatus.Proposed, result.PlanStatus);
        Assert.AreEqual("Action!", result.PlanName);
    }

    [TestMethod]
    public void CheckIfPlan_Plan1()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Plan Action!
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var line = post.VoteLines[0];
        var result = VoteBlocks.CheckIfPlan(line);

        Assert.AreEqual(PlanStatus.Plan, result.PlanStatus);
        Assert.AreEqual("Action!", result.PlanName);
    }

    [TestMethod]
    public void CheckIfPlan_Plan2()
    {
        var origin = GetOrigin1();
        var text = """
            [X] Kinematics's Plan
            """;

        var post = Posting.Create(origin, text);
        Assert.IsNotNull(post);

        var line = post.VoteLines[0];
        var result = VoteBlocks.CheckIfPlan(line);

        Assert.AreEqual(PlanStatus.Plan, result.PlanStatus);
        Assert.AreEqual("Kinematics", result.PlanName);
    }

}
