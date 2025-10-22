using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Models;

namespace NetTally.Tests.Components.Counting;

[TestClass]
public class ReferenceVoteTests
{
    #region Setup
    static Quest quest = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        var serviceProvider = TestStartup.ConfigureServices();
        quest = TestStartup.GetExampleQuest(serviceProvider);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;
    }
    #endregion Setup

    #region Origins
    private static Origin GetOrigin_Kinematics1()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        var postNumber = PostNumber.Create(101);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Atreya()
    {
        var author = Author.Create("Atreya");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123457);
        var postNumber = PostNumber.Create(102);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Kimberly()
    {
        var author = Author.Create("Kimberly");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123458);
        var postNumber = PostNumber.Create(103);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Kinematics2()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123459);
        var postNumber = PostNumber.Create(104);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }
    #endregion Origins

    #region Posts
    private static Post GetPostFromKinematics1(string postText)
    {
        var origin = GetOrigin_Kinematics1();
        return Post.Create(origin!, postText)!;
    }

    private static Post GetPostFromKinematics2(string postText)
    {
        var origin = GetOrigin_Kinematics2();
        return Post.Create(origin!, postText)!;
    }

    private static Post GetPostFromAtreya(string postText)
    {
        var origin = GetOrigin_Atreya();
        return Post.Create(origin!, postText)!;
    }

    private static Post GetPostFromKimberly(string postText)
    {
        var origin = GetOrigin_Kimberly();
        return Post.Create(origin!, postText)!;
    }
    #endregion Posts

    #region Define post text
    readonly static string oneLine = """
        [X] Run Lola Run!
        """;
    readonly static string oneLineTask = """
        [X][Movie] Run Lola Run!
        """;
    readonly static string twoLine = """
        [X] Run Lola Run!
        [X] National Geographic"
        """;
    readonly static string implicitPlan = """
        [X][Movie] Plan Run Lola Run!
        [X] National Geographic
        """;
    readonly static string explicitPlan = """
        [X][Movie] Plan Run Lola Run!
        - [X] National Geographic
        """;
    readonly static string twoChunkPlan = """
        [X][Movie] Plan Run Lola Run!
        - [X] National Geographic
        [X] Gunbuster
        """;
    readonly static string proposeBiking = """
        [X] Proposed plan: Mountain biking
        -[x] Camelback Mountain
        -[x] Grand Canyon
        """;
    readonly static string scoreBiking = """
        [75%] Plan Mountain biking
        """;

    readonly static string refKinematics = "[X] Kinematics";
    readonly static string refAtreya = "[X] Atreya";
    readonly static string refKimberly = "[X] Kimberly";
    readonly static string refKinematicsPercent = "[88%] Kinematics";
    readonly static string refAtreyaPercent = "[77%] Atreya";
    readonly static string refKimberlyPercent = "[66%] Kimberly";
    readonly static string refKinematicsApprove = "[+] Kinematics";
    readonly static string refAtreyaApprove = "[+] Atreya";
    readonly static string refKimberlyApprove = "[-] Kimberly";
    #endregion

    #region Verification
    private readonly static List<string> titles = ["Verify Examples"];

    private static void Verify_VotesBothSupport(Post post1, Post post2)
    {
        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(2, voters.Where(v => v.IsUser).Count());
        Assert.AreEqual(1, votes.Where(v => v.Category != MarkerType.None).Count());
    }
    #endregion Verification


    [TestMethod]
    public void Simple_Reference()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKinematics1(oneLine);
        var post2 = GetPostFromAtreya(refKinematics);

        Verify_VotesBothSupport(post1, post2);
    }

    [TestMethod]
    public void Simple_Reference_Marker_Percent()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKinematics1(oneLine);
        var post2 = GetPostFromAtreya(refKinematicsPercent);

        Verify_VotesBothSupport(post1, post2);
    }

    [TestMethod]
    public void Simple_Reference_Marker_Approve()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKinematics1(oneLine);
        var post2 = GetPostFromAtreya(refKinematicsApprove);

        Verify_VotesBothSupport(post1, post2);
    }

    [TestMethod]
    public void Simple_Reference_Task()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromAtreya(oneLineTask);
        var post2 = GetPostFromKimberly(refAtreya);

        Verify_VotesBothSupport(post1, post2);
    }

    [TestMethod]
    public void Simple_Reference_Task_Marker_Percent()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromAtreya(oneLineTask);
        var post2 = GetPostFromKimberly(refAtreyaPercent);

        Verify_VotesBothSupport(post1, post2);
    }

    [TestMethod]
    public void Simple_Reference_Task_Marker_Approve()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromAtreya(oneLineTask);
        var post2 = GetPostFromKimberly(refAtreyaApprove);

        Verify_VotesBothSupport(post1, post2);
    }

    [TestMethod]
    public void Reference_Self_NotAllowed()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromAtreya(twoLine);
        var post2 = GetPostFromKimberly(refKimberly);

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(2, voters.Count());
        Assert.AreEqual(3, votes.Count());
        Assert.IsTrue(OriginComparer.Instance.Equals(voters.First(), GetOrigin_Atreya()));
    }

    [TestMethod]
    public void Reference_Proxy_Disabled()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = true;

        var post1 = GetPostFromKimberly(twoLine);
        var post2 = GetPostFromKinematics1(refKimberly);

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(2, voters.Count());
        Assert.AreEqual(3, votes.Count());
    }

    [TestMethod]
    public void Reference_DoesNotExist()
    {
        quest.PartitionMode = PartitionMode.ByLine;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromAtreya(twoLine);
        var post2 = GetPostFromKinematics1(refKimberlyPercent);

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(2, voters.Count());
        Assert.AreEqual(3, votes.Count());
    }

    [TestMethod]
    public void Implicit_Plan_Name_Ref()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKimberly(implicitPlan);
        var post2 = GetPostFromAtreya(refKimberlyApprove);

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(3, voters.Count());
        Assert.AreEqual(2, voters.Where(v => v.IsUser).Count());
        Assert.AreEqual(1, votes.Where(v => v.Category != MarkerType.None).Count());
    }


    [TestMethod]
    public void Explicit_Plan_Ref()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKimberly(explicitPlan);
        var post2 = GetPostFromAtreya(oneLine); // Name of plan without "Plan"

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(3, voters.Count());
        Assert.AreEqual(2, voters.Where(v => v.IsUser).Count());
        Assert.AreEqual(1, votes.Count());
    }

    [TestMethod]
    public void Explicit_Plan_TwoChunk_Ref()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKimberly(twoChunkPlan);
        var post2 = GetPostFromKinematics2(oneLine); // Name of plan without "Plan"

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(3, voters.Count());
        Assert.AreEqual(2, voters.Where(v => v.IsUser).Count());
        Assert.AreEqual(2, votes.Count());
    }

    [TestMethod]
    public void Implicit_Plan_Ref()
    {
        quest.PartitionMode = PartitionMode.None;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKimberly(implicitPlan);
        var post2 = GetPostFromAtreya(oneLine);

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(3, voters.Count());
        Assert.AreEqual(2, voters.Where(v => v.IsUser).Count());
        Assert.AreEqual(1, votes.Count());
    }

    [TestMethod]
    public void Implicit_Plan_Block_Ref()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKimberly(implicitPlan);
        var post2 = GetPostFromAtreya(oneLine);

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(3, voters.Count());
        Assert.AreEqual(2, voters.Where(v => v.IsUser).Count());
        Assert.AreEqual(1, votes.Count());
    }

    [TestMethod]
    public void Cross_Marker_Reference_Plan()
    {
        quest.PartitionMode = PartitionMode.ByBlock;
        quest.DisableProxyVotes = false;

        var post1 = GetPostFromKinematics1(proposeBiking);
        var post2 = GetPostFromKinematics2(scoreBiking);

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var voters = quest.VoteCounter.GetAllVoters();
        var votes = quest.VoteCounter.GetAllVotes();

        Assert.AreEqual(2, voters.Count());
        Assert.AreEqual(1, voters.Where(v => v.IsUser).Count());
        Assert.AreEqual(1, votes.Count());
    }
}
