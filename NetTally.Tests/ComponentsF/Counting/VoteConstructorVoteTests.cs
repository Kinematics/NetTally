using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Counting;
using NetTally.Tally.ComponentsF.Posts;
using NetTally.Utility;

namespace NetTally.Tests.ComponentsF.Counting;

[TestClass]
public class VoteConstructorVoteTests
{
    #region Setup
    static IServiceProvider serviceProvider = null!;
    static Quest quest = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        serviceProvider = TestStartup.ConfigureServices();
    }

    [TestInitialize]
    public void TestInit()
    {
        quest = new Quest
        {
            VoteCounterF = serviceProvider.GetRequiredService<IVoteCounterF>()
        };
    }
    #endregion

    #region Origins
    private static OriginType GetOrigin_Kinematics1()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new Uri(Strings.ExampleHostUrl);
        Uri permalink = new Uri(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        int postNumber = 10;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static OriginType GetOrigin_Kinematics2()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new Uri(Strings.ExampleHostUrl);
        Uri permalink = new Uri(Strings.ExampleHostUrl);
        var postId = PostId.Create(124456);
        int postNumber = 30;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static OriginType GetOrigin_Karma1()
    {
        var author = Author.Create("Karma1");
        Uri uri = new Uri(Strings.ExampleHostUrl);
        Uri permalink = new Uri(Strings.ExampleHostUrl);
        var postId = PostId.Create(123457);
        int postNumber = 11;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static OriginType GetOrigin_Quincy()
    {
        var author = Author.Create("Quincy");
        Uri uri = new Uri(Strings.ExampleHostUrl);
        Uri permalink = new Uri(Strings.ExampleHostUrl);
        var postId = PostId.Create(123458);
        int postNumber = 12;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static OriginType GetOrigin_Muramasa()
    {
        var author = Author.Create("Muramasa");
        Uri uri = new Uri(Strings.ExampleHostUrl);
        Uri permalink = new Uri(Strings.ExampleHostUrl);
        var postId = PostId.Create(9321568);
        int postNumber = 8816;

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    #endregion Origins

    #region Sample Posts

    private static PostToProcess GetPost01()
    {
        var origin = GetOrigin_Kinematics1();

        string postText =
            """
            Tentative vote idea:
            [x][Action] Go to the warehouse~

            But might include something else...
            [x] Loot the boxes
            """;

        var post = Post.Create(origin, postText)!;
        var postp = new PostToProcess(post);
        
        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    private static PostToProcess GetPost02()
    {
        var origin = GetOrigin_Karma1();

        string postText =
            """
            I agree.
            [x][Action] Go to the warehouse~
            [x] Loot the boxes
            """;

        var post = Post.Create(origin, postText)!;
        var postp = new PostToProcess(post);
        
        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    private static PostToProcess GetPost03()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            I have a better idea.
            [x][Action] Go to the docks
            -[x] With the motorcycle
            [x] And catch them in the act.
            """;

        var post = Post.Create(origin, postText)!;
        var postp = new PostToProcess(post);
        
        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    private static PostToProcess GetPost04()
    {
        var origin = GetOrigin_Muramasa();

        string postText =
            """
            [x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
            [x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
            [x] Head over to Oriko's.
            -[x] 20 minutes roof hopping practice. Then fly the rest of the way.
            -[x] Cleansing.
            -[x] Take both of them food shopping (or whoever wants to go.)
            -[x] Light conversation. No need for serious precog questions right now.
            """;

        var post = Post.Create(origin, postText)!;
        var postp = new PostToProcess(post);
        
        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    private static PostToProcess GetPost05_Tally()
    {
        var origin = GetOrigin_Muramasa();

        string postText =
            """
            『b』Vote Tally『/b』
            『color=transparent』##### NetTally 1.0『/color』
            [x] Text Nagisa's uncle about her visiting today. Establish a specific time. (Keep in mind Sayaka's hospital visit.)
            [x] Telepathy Oriko and Kirika. They probably need to pick up some groceries at this point. It should be fine if you go with them. And of course, you can cleanse their gems too.
            [x] Head over to Oriko's.
            -[x] 20 minutes roof hopping practice. Then fly the rest of the way.
            -[x] Cleansing.
            -[x] Take both of them food shopping (or whoever wants to go.)
            -[x] Light conversation. No need for serious precog questions right now.
            """;

        var post = Post.Create(origin, postText)!;
        var postp = new PostToProcess(post);
        
        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    //static (string name, VoteLineBlock block) GetBasePlan1()
    //{
    //    VoteLine line1 = new("", "X", "", "Base Plan Sound of Music", MarkerType.Vote, 100);
    //    VoteLine line2 = new("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
    //    VoteLine line3 = new("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
    //    VoteLine line4 = new("-", "X", "", "Return home", MarkerType.Vote, 100);

    //    List<VoteLine> lines = [line1, line2, line3, line4];

    //    VoteLineBlock block = new(lines);

    //    return ("Sound of Music", block);
    //}

    //static (string name, VoteLineBlock block) GetBasePlan2()
    //{
    //    VoteLine line1 = new("", "X", "", "Proposed Plan: Sound of Music", MarkerType.Vote, 100);
    //    VoteLine line2 = new("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
    //    VoteLine line3 = new("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
    //    VoteLine line4 = new("-", "X", "", "Return home", MarkerType.Vote, 100);

    //    List<VoteLine> lines = [line1, line2, line3, line4];

    //    VoteLineBlock block = new(lines);

    //    return ("Sound of Music", block);
    //}

    //static (string name, VoteLineBlock block) GetBasePlan3()
    //{
    //    VoteLine line1 = new("", "X", "", "Plan Sound of Music", MarkerType.Vote, 100);
    //    VoteLine line2 = new("-", "X", "", "Climb the mountain", MarkerType.Vote, 100);
    //    VoteLine line3 = new("-", "X", "", "Sing the songs", MarkerType.Vote, 100);
    //    VoteLine line4 = new("-", "X", "", "Return home", MarkerType.Vote, 100);

    //    List<VoteLine> lines = [line1, line2, line3, line4];

    //    VoteLineBlock block = new(lines);

    //    return ("Sound of Music", block);
    //}
    #endregion

    #region Test Sample Posts
    [TestMethod]
    public void Process_Post1_NoPartitioning()
    {
        quest.PartitionMode = PartitionMode.None;
        var post = GetPost01();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(2, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post2_NoPartitioning()
    {
        quest.PartitionMode = PartitionMode.None;
        var post = GetPost02();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(2, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post3_NoPartitioning()
    {
        quest.PartitionMode = PartitionMode.None;
        var post = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(3, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post1_BlockPartitioning()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var post = GetPost01();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post2_BlockPartitioning()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var post = GetPost02();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post3_BlockPartitioning()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var post = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(2, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post1_LinePartitioning()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var post = GetPost01();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post2_LinePartitioning()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var post = GetPost02();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post3_LinePartitioning()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var post = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(3, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post3_LinePartitioning_TaskFilter()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.UseCustomTaskFilters = true;
        quest.CustomTaskFilters = "Action";
        var post = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().Lines.Count);
    }

    [TestMethod]
    public void Process_Post3_LineTaskPartitioning_TaskFilter()
    {
        quest.PartitionMode = PartitionMode.ByLineTask;
        quest.UseCustomTaskFilters = true;
        quest.CustomTaskFilters = "Action";
        var post = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().Lines.Count);
    }

    //[TestMethod]
    //public void Normalize_1()
    //{
    //    var (name, block) = GetBasePlan1();

    //    var (outName, votes) = VoteConstructor.NormalizePlan(name, block);

    //    Assert.AreEqual(name, outName);
    //    Assert.AreEqual("", votes.Task);
    //    Assert.AreEqual(4, votes.Lines.Count);
    //    Assert.AreEqual("Plan: Sound of Music", votes.Lines[0].CleanContent);
    //}

    //[TestMethod]
    //public void Normalize_2()
    //{
    //    var (name, block) = GetBasePlan2();

    //    var (outName, votes) = VoteConstructor.NormalizePlan(name, block);

    //    Assert.AreEqual(name, outName);
    //    Assert.AreEqual("", votes.Task);
    //    Assert.AreEqual(4, votes.Lines.Count);
    //    Assert.AreEqual("Plan: Sound of Music", votes.Lines[0].CleanContent);
    //}

    //[TestMethod]
    //public void Normalize_3()
    //{
    //    var (name, block) = GetBasePlan3();

    //    var (outName, votes) = VoteConstructor.NormalizePlan(name, block);

    //    Assert.AreEqual(name, outName);
    //    Assert.AreEqual("", votes.Task);
    //    Assert.AreEqual(4, votes.Lines.Count);
    //    Assert.AreEqual("Plan Sound of Music", votes.Lines[0].CleanContent);
    //}
    #endregion

    #region Test More Posts
    [TestMethod]
    public void Process_Post4_ByLine()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var post = GetPost04();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(7, votes.Count);
        Assert.AreEqual("", votes[0].Task.Name);
        Assert.AreEqual(1, votes[0].Lines.Count);
    }

    [TestMethod]
    public void Process_Post4_ByBlock()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var post = GetPost04();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(3, votes.Count);
        Assert.AreEqual("", votes.First().Task.Name);
        Assert.AreEqual(1, votes[0].Lines.Count);
        Assert.AreEqual(1, votes[1].Lines.Count);
        Assert.AreEqual(5, votes[2].Lines.Count);
    }

    [TestMethod]
    public void Process_Post5_ByBlock()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var post = GetPost05_Tally();

        Assert.IsFalse(post.HasVote);

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(0, votes.Count);
    }
    #endregion
}
