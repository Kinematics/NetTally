using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.Components.Counting;
using NetTally.Tally.Components.Posts;
using NetTally.Utility;

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

    #region Origins
    private static Origin GetOrigin_Kinematics1()
    {
        var author = Authors.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostIds.Create(123456);
        var postNumber = PostIds.Create(10);

        var origin = Origins.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Kinematics2()
    {
        var author = Authors.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostIds.Create(124456);
        var postNumber = PostIds.Create(30);

        var origin = Origins.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Karma1()
    {
        var author = Authors.Create("Karma1");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostIds.Create(123457);
        var postNumber = PostIds.Create(11);

        var origin = Origins.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Quincy()
    {
        var author = Authors.Create("Quincy");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostIds.Create(123458);
        var postNumber = PostIds.Create(12);

        var origin = Origins.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Muramasa()
    {
        var author = Authors.Create("Muramasa");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostIds.Create(9321568);
        var postNumber = PostIds.Create(8816);

        var origin = Origins.CreateUser(author, uri, permalink, postId, postNumber);

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

        var post = Posting.Create(origin, postText)!;
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

        var post = Posting.Create(origin, postText)!;
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

        var post = Posting.Create(origin, postText)!;
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

        var post = Posting.Create(origin, postText)!;
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

        var post = Posting.Create(origin, postText)!;
        var postp = new PostToProcess(post);
        
        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    private static PostToProcess GetPlan01()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            [x]Base Plan Sound of Music
            -[x] Climb the mountain
            -[x] Sing the songs
            -[x] Return home
            """;

        var post = Posting.Create(origin, postText)!;
        var postp = new PostToProcess(post);

        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    private static PostToProcess GetPlan02()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            [X]Proposed Plan: Sound of Music
            -[x] Climb the mountain
            -[x] Sing the songs
            -[x] Return home
            """;

        var post = Posting.Create(origin, postText)!;
        var postp = new PostToProcess(post);

        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }

    private static PostToProcess GetPlan03()
    {
        var origin = GetOrigin_Quincy();

        string postText =
            """
            [x]Plan Sound of Music
            -[x] Climb the mountain
            -[x] Sing the songs
            -[x] Return home
            """;

        var post = Posting.Create(origin, postText)!;
        var postp = new PostToProcess(post);

        VoteConstructor.ConfigureWorkingVote(postp, quest);
        return postp;
    }
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
        Assert.AreEqual(2, votes.First().LineCount);
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
        Assert.AreEqual(2, votes.First().LineCount);
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
        Assert.AreEqual(3, votes.First().LineCount);
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
        Assert.AreEqual(1, votes.First().LineCount);
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
        Assert.AreEqual(1, votes.First().LineCount);
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
        Assert.AreEqual(2, votes.First().LineCount);
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
        Assert.AreEqual(1, votes.First().LineCount);
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
        Assert.AreEqual(1, votes.First().LineCount);
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
        Assert.AreEqual(1, votes.First().LineCount);
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
        Assert.AreEqual(1, votes.First().LineCount);
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
        Assert.AreEqual(1, votes.First().LineCount);
    }

    [TestMethod]
    public void Normalize_1()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        var post = GetPlan01();

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);

        var processed = VoteConstructor.PreprocessPostGetPlans(
            quest,
            post.Origin.Author,
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
        var post = GetPlan02();

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);

        var processed = VoteConstructor.PreprocessPostGetPlans(
            quest,
            post.Origin.Author,
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
        var post = GetPlan03();

        var blocks = VoteCounter.GetVoteBlocks(post.VoteLines);

        var processed = VoteConstructor.PreprocessPostGetPlans(
            quest,
            post.Origin.Author,
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
        var post = GetPost04();

        var processed = VoteConstructor.TryProcessPostGetVotes(post, quest, out var votes);

        Assert.IsTrue(processed);
        Assert.AreEqual(7, votes.Count);
        Assert.AreEqual("", votes[0].Task.Name);
        Assert.AreEqual(1, votes[0].LineCount);
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
        Assert.AreEqual(1, votes[0].LineCount);
        Assert.AreEqual(1, votes[1].LineCount);
        Assert.AreEqual(5, votes[2].LineCount);
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
