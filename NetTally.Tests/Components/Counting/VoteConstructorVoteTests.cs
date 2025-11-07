using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Models;
using NetTally.Tally.Counting;
using NetTally.Tally.Processing;

namespace NetTally.Tests.Components.Counting;

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
            VoteCounter = serviceProvider.GetRequiredService<IVoteCounter>()
        };
    }
    #endregion

    #region Origin
    private static Origin GetOrigin_Kinematics1()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        var postNumber = PostNumber.Create(10);

        var details = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, details);

        return origin!;
    }

    private static Origin GetOrigin_Kinematics2()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(124456);
        var postNumber = PostNumber.Create(30);

        var details = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, details);

        return origin!;
    }

    private static Origin GetOrigin_Karma1()
    {
        var author = Author.Create("Karma1");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123457);
        var postNumber = PostNumber.Create(11);

        var details = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, details);

        return origin!;
    }

    private static Origin GetOrigin_Quincy()
    {
        var author = Author.Create("Quincy");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123458);
        var postNumber = PostNumber.Create(12);

        var details = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, details);

        return origin!;
    }

    private static Origin GetOrigin_Muramasa()
    {
        var author = Author.Create("Muramasa");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(9321568);
        var postNumber = PostNumber.Create(8816);

        var details = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, details);

        return origin!;
    }

    #endregion Origin

    #region Sample Posts

    private static VoteToProcess GetPost01()
    {
        var origin = GetOrigin_Kinematics1();

        string postText =
            """
            Tentative vote idea:
            [x][Action] Go to the warehouse~

            But might include something else...
            [x] Loot the boxes
            """;

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNotNull(votep);

        VoteConstructor.ConfigureWorkingVote(votep, quest);
        return votep;
    }

    private static VoteToProcess GetPost02()
    {
        var origin = GetOrigin_Karma1();

        string postText =
            """
            I agree.
            [x][Action] Go to the warehouse~
            [x] Loot the boxes
            """;

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNotNull(votep);

        VoteConstructor.ConfigureWorkingVote(votep, quest);
        return votep;
    }

    private static VoteToProcess GetPost03()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            I have a better idea.
            [x][Action] Go to the docks
            -[x] With the motorcycle
            [x] And catch them in the act.
            """;

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNotNull(votep);

        VoteConstructor.ConfigureWorkingVote(votep, quest);
        return votep;
    }

    private static VoteToProcess GetPost04()
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

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNotNull(votep);

        VoteConstructor.ConfigureWorkingVote(votep, quest);
        return votep;
    }

    private static VoteToProcess? GetPost05_Tally()
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

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNull(votep);

        return votep;
    }

    private static VoteToProcess GetPlan01()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            [x]Base Plan Sound of Music
            -[x] Climb the mountain
            -[x] Sing the songs
            -[x] Return home
            """;

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNotNull(votep);

        VoteConstructor.ConfigureWorkingVote(votep, quest);
        return votep;
    }

    private static VoteToProcess GetPlan02()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            [X]Proposed Plan: Sound of Music
            -[x] Climb the mountain
            -[x] Sing the songs
            -[x] Return home
            """;

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNotNull(votep);

        VoteConstructor.ConfigureWorkingVote(votep, quest);
        return votep;
    }

    private static VoteToProcess GetPlan03()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            [x]Plan Sound of Music
            -[x] Climb the mountain
            -[x] Sing the songs
            -[x] Return home
            """;

        var post = Post.Create(origin, postText);
        var vote = Vote.Create(post);
        var votep = Vote.CreateToProcess(vote);
        Assert.IsNotNull(votep);

        VoteConstructor.ConfigureWorkingVote(votep, quest);
        return votep;
    }
    #endregion

    #region Test Sample Posts
    [TestMethod]
    public void Process_Post1_NoPartitioning()
    {
        quest.PartitionMode = PartitionMode.None;
        var vote = GetPost01();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(2, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post2_NoPartitioning()
    {
        quest.PartitionMode = PartitionMode.None;
        var vote = GetPost02();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(2, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post3_NoPartitioning()
    {
        quest.PartitionMode = PartitionMode.None;
        var vote = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(3, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post1_BlockPartitioning()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPost01();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post2_BlockPartitioning()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPost02();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post3_BlockPartitioning()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(2, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post1_LinePartitioning()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var vote = GetPost01();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post2_LinePartitioning()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var vote = GetPost02();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post3_LinePartitioning()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var vote = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(3, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post3_LinePartitioning_TaskFilter()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.UseCustomTaskFilters = true;
        quest.CustomTaskFilters = "Action";
        var vote = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(1, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Process_Post3_LineTaskPartitioning_TaskFilter()
    {
        quest.PartitionMode = PartitionMode.ByLineTask;
        quest.UseCustomTaskFilters = true;
        quest.CustomTaskFilters = "Action";
        var vote = GetPost03();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(2, votes.Count);
        Assert.AreEqual("Action", votes.First().Task.Name);
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Normalize_1()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPlan01();

        var blocks = VoteCounter.GetVoteBlocks(vote.VoteLines);

        var processed = VoteConstructor.PreprocessPostGetPlans(
            quest,
            vote.Origin.GetName(),
            VoteBlocks.IsBlockAProposedPlan,
            blocks);

        var (name, content) = processed.First();

        var normalized = quest.VoteCounter.NormalizePlan(name, content);

        Assert.IsNotNull(normalized);

        var (normName, normContent) = normalized.Value;

        Assert.AreEqual(name, normName);
        Assert.AreEqual("", normContent.Task.Name);
        Assert.AreEqual(4, normContent.LineCount);
        Assert.AreEqual("Sound of Music", normName);
        Assert.AreEqual("Plan: Sound of Music", normContent.Lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Normalize_2()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPlan02();

        var blocks = VoteCounter.GetVoteBlocks(vote.VoteLines);

        var processed = VoteConstructor.PreprocessPostGetPlans(
            quest,
            vote.Origin.GetName(),
            VoteBlocks.IsBlockAProposedPlan,
            blocks);

        var (name, content) = processed.First();

        var normalized = quest.VoteCounter.NormalizePlan(name, content);

        Assert.IsNotNull(normalized);

        var (normName, normContent) = normalized.Value;

        Assert.AreEqual(name, normName);
        Assert.AreEqual("", normContent.Task.Name);
        Assert.AreEqual(4, normContent.LineCount);
        Assert.AreEqual("Sound of Music", normName);
        Assert.AreEqual("Plan: Sound of Music", normContent.Lines[0].Content.CleanContent);
    }

    [TestMethod]
    public void Normalize_3()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPlan03();

        var blocks = VoteCounter.GetVoteBlocks(vote.VoteLines);

        var processed = VoteConstructor.PreprocessPostGetPlans(
            quest,
            vote.Origin.GetName(),
            VoteBlocks.IsBlockAnExplicitPlan,
            blocks);

        var (name, content) = processed.First();

        var normalized = quest.VoteCounter.NormalizePlan(name, content);

        Assert.IsNotNull(normalized);

        var (normName, normContent) = normalized.Value;

        Assert.AreEqual(name, normName);
        Assert.AreEqual("", normContent.Task.Name);
        Assert.AreEqual(4, normContent.LineCount);
        Assert.AreEqual("Sound of Music", normName);
        Assert.AreEqual("Plan: Sound of Music", normContent.Lines[0].Content.CleanContent);
    }
    #endregion

    #region Test More Posts
    [TestMethod]
    public void Process_Post4_ByLine()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        var vote = GetPost04();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(7, votes.Count);
        Assert.AreEqual("", votes[0].Task.Name);
        Assert.AreEqual(1, votes[0].LineCount);
    }

    [TestMethod]
    public void Process_Post4_ByBlock()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPost04();

        var processed = VoteConstructor.TryProcessPostGetVotes(vote, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(3, votes.Count);
        Assert.AreEqual("", votes.First().Task.Name);
        Assert.AreEqual(1, votes[0].LineCount);
        Assert.AreEqual(1, votes[1].LineCount);
        Assert.AreEqual(5, votes[2].LineCount);
    }

    [TestMethod]
    public void Process_Post5_ByBlock()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var vote = GetPost05_Tally();

        Assert.IsNull(vote);
    }
    #endregion
}
