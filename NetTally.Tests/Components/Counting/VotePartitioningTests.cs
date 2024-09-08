using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.Components.Counting;
using NetTally.Tally.Components.Posts;
using NetTally.Tally.Components.Votes;
using NetTally.Utility;

namespace NetTally.Tests.Components.Counting;
[TestClass]
public class VotePartitioningTests
{
    static Quest quest = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        var serviceProvider = TestStartup.ConfigureServices();
        quest = TestStartup.GetExampleQuest(serviceProvider);
    }

    #region Data
    private static OriginType GetOrigin_Kinematics()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        int postNumber = 10;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }


    readonly static string oneLineVote = """
            [X] Run Lola Run!
            """;
    readonly static string oneLineTaskVote = """
            [X][Movie] Run Lola Run!
            """;
    readonly static string twoLineVote = """
            [X] Run Lola Run!
            [X] National Geographic
            """;
    readonly static string twoLineTaskVote = """
            [X][Movie] Run Lola Run!
            [X] National Geographic
            """;
    readonly static string childLineVote = """
            [X][Movie] Run Lola Run!
            -[X] National Geographic
            """;
    readonly static string twoChunkVote = """
            [X][Movie] Run Lola Run!
            -[X] National Geographic
            [X] Gunbuster
            """;
    #endregion Data



    [TestMethod]
    public void SingleLine_Partitioning_None()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), oneLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.None;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void SingleLine_Partitioning_ByLine()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), oneLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLine;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void SingleLine_Partitioning_ByBlock()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), oneLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlock;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void SingleLine_Partitioning_ByLineTask()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), oneLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLineTask;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void SingleLine_Partitioning_ByBlockAll()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), oneLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlockAll;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoLine_Partitioning_None()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.None;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("""
            [] Run Lola Run!
            [] National Geographic
            """, VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoLine_Partitioning_ByLine()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLine;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoLine_Partitioning_ByBlock()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlock;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoLine_Partitioning_ByLineTask()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLineTask;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoLine_Partitioning_ByBlockAll()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlockAll;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("[] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void ChildLine_Partitioning_None()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), childLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.None;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("""
            [][Movie] Run Lola Run!
            -[] National Geographic
            """, VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void ChildLine_Partitioning_ByLine()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), childLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLine;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("[][Movie] Run Lola Run!", VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void ChildLine_Partitioning_ByBlock()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), childLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlock;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("""
            [][Movie] Run Lola Run!
            -[] National Geographic
            """, VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void ChildLine_Partitioning_ByLineTask()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), childLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLineTask;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("[][Movie] National Geographic", VoteBlockDisplay.ToComparableString(votes[1]));
    }

    [TestMethod]
    public void ChildLine_Partitioning_ByBlockAll()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), childLineVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlockAll;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("""
            [][Movie] Run Lola Run!
            -[] National Geographic
            """, VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoChunk_Partitioning_None()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoChunkVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.None;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("""
            [][Movie] Run Lola Run!
            -[] National Geographic
            [] Gunbuster
            """, VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoChunk_Partitioning_ByLine()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoChunkVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLine;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(3, votes.Count);
        Assert.AreEqual("[] Gunbuster", VoteBlockDisplay.ToComparableString(votes[2]));
    }

    [TestMethod]
    public void TwoChunk_Partitioning_ByBlock()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoChunkVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlock;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("""
            [][Movie] Run Lola Run!
            -[] National Geographic
            """, VoteBlockDisplay.ToComparableString(votes[0]));
    }

    [TestMethod]
    public void TwoChunk_Partitioning_ByLineTask()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoChunkVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByLineTask;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(3, votes.Count);
        Assert.AreEqual("[][Movie] National Geographic", VoteBlockDisplay.ToComparableString(votes[1]));
    }

    [TestMethod]
    public void TwoChunk_Partitioning_ByBlockAll()
    {
        var post = Post.CreateToProcess(GetOrigin_Kinematics(), twoChunkVote);
        Assert.IsNotNull(post);

        quest.PartitionMode = PartitionMode.ByBlockAll;

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.IsNotNull(votes);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("""
            [][Movie] Run Lola Run!
            -[] National Geographic
            """, VoteBlockDisplay.ToComparableString(votes[0]));
    }

}
