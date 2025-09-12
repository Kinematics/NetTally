using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Enums;
using NetTally.Tally.Posts.Component;
using NetTally.Tally.Posts.Component.Creation;
using NetTally.Tally.Vote.Component;
using NetTally.Utility;

namespace NetTally.Tests.Components.Counting;

[TestClass]
public class VoteCounterTests
{
    #region Setup
    static Quest quest = null!;
    static QuestsInfo questsInfo = null!;
    static IServiceProvider serviceProvider = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        serviceProvider = TestStartup.ConfigureServices();
        questsInfo = serviceProvider.GetRequiredService<QuestsInfo>();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        quest = TestStartup.GetExampleQuest(serviceProvider);
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;
        questsInfo.SelectedQuest = quest;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;
    }
    #endregion

    #region Origin
    private static Origin GetOrigin_Kinematics()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123426);
        var postNumber = PostId.Create(98);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Brogatar1()
    {
        var author = Author.Create("Brogatar");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        var postNumber = PostId.Create(100);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Brogatar2()
    {
        var author = Author.Create("Brogatar");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123476);
        var postNumber = PostId.Create(110);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Madfish1()
    {
        var author = Author.Create("Madfish");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123460);
        var postNumber = PostId.Create(101);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    private static Origin GetOrigin_Madfish2()
    {
        var author = Author.Create("Madfish");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123466);
        var postNumber = PostId.Create(105);

        var origin = Origin.CreateUser(author, uri, permalink, postId, postNumber);

        return origin!;
    }

    #endregion Origin

    #region Post Text
    static readonly List<string> titles = ["A title for testing"];

    static readonly string postText1 = """
            [X] Add this to your list of experiments for today.
            [X] This is fine by you.
            -[X] At least for today.
            """;
    static readonly string postText2 = """
            [X] Save it for another day.
            [X] This is fine by you.
            -[X] At least for today.
            """;
    static readonly string postText3 = """
            [X] Plan Experiment
            -[X] This is fine by you.
            --[X] At least for today.
            """;
    static readonly string postText4 = """
            [X] Proposed Plan: Experiment
            -[X] Add this to your list of experiments for today.
            -[X] This is fine by you.
            --[X] At least for today.
            """;
    static readonly string postText5 = """
            [X] Plan: Experiment
            -[X] Add this to your list of experiments for today.
            -[X] This is fine by you.
            --[X] At least for today.
            """;
    static readonly string postText6 = """
            [X] Plan: Experiment
            -[X] Alchemy structure
            -[X] Pea soup
            --[X] At least for today.
            """;
    static readonly string postText7 = """
            The referenced post did not have the problem described, but another post did.  Basically,
            [x] Plan 『url="https://forums.sufficientvelocity.com/members/4076/"』@Kinematics『/url』 
            Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).
            """;
    static readonly string postText8 = """
            The referenced post did not have the problem described, but another post did.  Basically,
            [x] Plan 『url="https://forums.sufficientvelocity.com/members/4076/"』Kinematics『/url』 
            Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).
            """;
    static readonly string postText9 = """
            The referenced post did not have the problem described, but another post did.  Basically, 
            [x] 『url="https://forums.sufficientvelocity.com/members/4076/"』@Kinematics『/url』 
            Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).
            """;
    static readonly string postText10 = """
            The referenced post did not have the problem described, but another post did.  Basically, 
            [x] 『url="https://forums.sufficientvelocity.com/members/4076/"』Kinematics『/url』 
            Wouldn't be applied to my proposed plan because it got turned into a member link (the '@' symbol is dropped on QQ's forums, so that doesn't interfere in this case).
            """;
    #endregion Post Text

    #region Basics
    [TestMethod]
    public void Check_Tally_Adds_Normal()
    {
        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText2);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(3, allVotes[1].LineCount);

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }


    [TestMethod]
    public void Check_Reset()
    {
        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText2);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(3, allVotes[1].LineCount);

        quest.VoteCounter.Reset();

        allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(0, allVotes.Count);
    }

    public static void Check_Tally_Adds_Plan()
    {
        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var post1 = Post.Create(origin1, postText3);
        var post2 = Post.Create(origin2, postText2);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(3, allVotes[1].LineCount);

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasPlan("Experiment"));

        var vote1 = quest.VoteCounter.GetVotesBy(origin1).ToList();

        Assert.AreEqual(1, vote1.Count);

        var voters1 = quest.VoteCounter.GetVotersFor(vote1[0]);

        Assert.AreEqual(2, voters1.Count());
    }
    #endregion Basics

    #region Replacements
    [TestMethod]
    public void Reprocess_Doesnt_Stack_Lines()
    {
        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText2);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(3, allVotes[1].LineCount);

        quest.PartitionMode = PartitionMode.ByLine;

        quest.ConstructVotes();

        allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(4, allVotes.Count);
        Assert.AreEqual(1, allVotes[0].LineCount);
        Assert.AreEqual(1, allVotes[1].LineCount);
        Assert.AreEqual(1, allVotes[2].LineCount);
        Assert.AreEqual(1, allVotes[3].LineCount);

        quest.PartitionMode = PartitionMode.None;

        quest.ConstructVotes();

        allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(3, allVotes[1].LineCount);
    }

    [TestMethod]
    public void Check_Tally_Adds_Reference()
    {
        string postTextRef = "[X] Brogatar";

        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postTextRef);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }


    [TestMethod]
    public void Check_Tally_Replacement_Vote()
    {
        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var origin1a = GetOrigin_Brogatar2();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText2);
        var post3 = Post.Create(origin1a, postText2);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);
        Assert.IsNotNull(post3);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);
        Assert.IsTrue(post3.HasVote);

        List<Post> posts = [post1, post2, post3];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }

    [TestMethod]
    public void Check_User_Proxy_Only_Proposed_Plan()
    {
        string postTextRef = "[X] Brogatar";

        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var post1 = Post.Create(origin1, postText4);
        var post2 = Post.Create(origin2, postTextRef);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);

        Assert.AreEqual(0, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));
        Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[1]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasPlan("Experiment"));
    }

    [TestMethod]
    public void Check_Original_User_Can_Replace_Plan()
    {
        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var origin3 = GetOrigin_Kinematics();
        var origin1a = GetOrigin_Brogatar2();
        var post1 = Post.Create(origin1, postText5);
        var post2 = Post.Create(origin2, postText6);
        var post3 = Post.Create(origin3, postText6);
        var post4 = Post.Create(origin1a, postText6);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);
        Assert.IsNotNull(post3);
        Assert.IsNotNull(post4);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);
        Assert.IsTrue(post3.HasVote);
        Assert.IsTrue(post4.HasVote);

        quest.PartitionMode = PartitionMode.None;
        quest.AllowUsersToUpdatePlans = true;

        List<Post> posts = [post1, post2];

        quest.ConstructVotes(titles, posts);

        var plans = quest.VoteCounter.GetReferencePlans().ToList();

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Add this to your list of experiments for today.",
            plans[0].Lines[1].Content.CleanContent);

        quest.VoteCounter.Reset();
        posts = [post1, post2, post3];

        quest.ConstructVotes(titles, posts);

        plans = quest.VoteCounter.GetReferencePlans().ToList();

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Add this to your list of experiments for today.",
            plans[0].Lines[1].Content.CleanContent);

        quest.VoteCounter.Reset();
        posts = [post1, post2, post3, post4];

        quest.ConstructVotes(titles, posts);

        plans = quest.VoteCounter.GetReferencePlans().ToList();

        Assert.AreEqual(1, plans.Count);
        Assert.AreEqual("Alchemy structure",
            plans[0].Lines[1].Content.CleanContent);
    }

    #endregion Replacements

    #region Callouts as proxies
    [TestMethod]
    public void Check_Callout_Links_With_At_As_Plan()
    {
        var origin1 = GetOrigin_Kinematics();
        var origin2 = GetOrigin_Brogatar1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText7);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }

    [TestMethod]
    public void Check_Callout_Links_Without_At_As_Plan()
    {
        var origin1 = GetOrigin_Kinematics();
        var origin2 = GetOrigin_Brogatar1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText8);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }

    [TestMethod]
    public void Check_Callout_Links_With_At()
    {
        var origin1 = GetOrigin_Kinematics();
        var origin2 = GetOrigin_Brogatar1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText9);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }

    [TestMethod]
    public void Check_Callout_Links_Without_At()
    {
        var origin1 = GetOrigin_Kinematics();
        var origin2 = GetOrigin_Brogatar1();
        var post1 = Post.Create(origin1, postText1);
        var post2 = Post.Create(origin2, postText10);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);
        Assert.AreEqual(3, allVotes[0].LineCount);
        Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }
    #endregion Callouts as proxies

    #region Future references
    [TestMethod]
    public void Check_Future_Reference_Handling_Normal()
    {
        string text1 = "[X] Brogatar's First post";
        string text2 = "[X] Brogatar";
        string text3 = "[X] Brogatar's Second post";

        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var origin3 = GetOrigin_Brogatar2();
        var post1 = Post.Create(origin1, text1);
        var post2 = Post.Create(origin2, text2);
        var post3 = Post.Create(origin3, text3);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);
        Assert.IsNotNull(post3);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);
        Assert.IsTrue(post3.HasVote);

        List<Post> posts = [post1, post2, post3];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);

        Assert.AreEqual(2, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));
        Assert.AreEqual("[] Brogatar's Second post",
            VoteBlockDisplay.ToComparableString(allVotes[0]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }

    [TestMethod]
    public void Check_Future_Reference_Handling_Preempted()
    {
        string text1 = "[X] Brogatar's First post";
        string text2 = "[X] Brogatar";
        string text3 = "[X] Changed my mind";
        string text4 = "[X] Brogatar's Second post";

        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();
        var origin3 = GetOrigin_Madfish2();
        var origin4 = GetOrigin_Brogatar2();
        var post1 = Post.Create(origin1, text1);
        var post2 = Post.Create(origin2, text2);
        var post3 = Post.Create(origin3, text3);
        var post4 = Post.Create(origin4, text4);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);
        Assert.IsNotNull(post3);
        Assert.IsNotNull(post4);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);
        Assert.IsTrue(post3.HasVote);
        Assert.IsTrue(post4.HasVote);

        List<Post> posts = [post1, post2, post3, post4];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);

        Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[0]));
        Assert.AreEqual(1, quest.VoteCounter.VoteStorage.GetUserSupportCountFor(allVotes[1]));

        Assert.AreEqual("[] Changed my mind",
            VoteBlockDisplay.ToComparableString(allVotes[0]));
        Assert.AreEqual("[] Brogatar's Second post",
            VoteBlockDisplay.ToComparableString(allVotes[1]));

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));
    }
    #endregion Future references

    #region Test general vote matching
    public static void Test_Votes_Match(string text1, string text2)
    {
        Assert.IsFalse(string.IsNullOrEmpty(text1));
        Assert.IsFalse(string.IsNullOrEmpty(text2));

        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();

        var post1 = Post.Create(origin1, text1);
        var post2 = Post.Create(origin2, text2);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(1, allVotes.Count);

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));

        var vote1 = allVotes[0];
        var voters = quest.VoteCounter.VoteStorage.GetVotersFor(vote1).ToList();

        Assert.AreEqual(2, voters.Count);
        Assert.IsTrue(voters.Contains(origin1));
        Assert.IsTrue(voters.Contains(origin2));
    }

    public static void Test_Votes_Dont_Match(string text1, string text2)
    {
        Assert.IsFalse(string.IsNullOrEmpty(text1));
        Assert.IsFalse(string.IsNullOrEmpty(text2));

        var origin1 = GetOrigin_Brogatar1();
        var origin2 = GetOrigin_Madfish1();

        var post1 = Post.Create(origin1, text1);
        var post2 = Post.Create(origin2, text2);

        Assert.IsNotNull(post1);
        Assert.IsNotNull(post2);

        Assert.IsTrue(post1.HasVote);
        Assert.IsTrue(post2.HasVote);

        List<Post> posts = [post1, post2];

        quest.PartitionMode = PartitionMode.None;
        quest.ConstructVotes(titles, posts);

        var allVotes = quest.VoteCounter.GetAllVotes().ToList();

        Assert.AreEqual(2, allVotes.Count);

        Assert.IsTrue(quest.VoteCounter.HasVoter(origin1.Author.Name));
        Assert.IsTrue(quest.VoteCounter.HasVoter(origin2.Author.Name));

        var vote1 = allVotes[0];
        var voters1 = quest.VoteCounter.VoteStorage.GetVotersFor(vote1).ToList();
        var vote2 = allVotes[1];
        var voters2 = quest.VoteCounter.VoteStorage.GetVotersFor(vote2).ToList();

        Assert.AreEqual(1, voters1.Count);
        Assert.AreEqual(1, voters2.Count);
    }

    [TestMethod]
    public void Check_Match_Same()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic test";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_BBCode()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic 『b』test『/b』";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_No_Case()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic TEST";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Yes_Case()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic TEST";
        quest.CaseIsSignificant = true;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Dont_Match(text1, text2);
    }


    [TestMethod]
    public void Check_Match_No_Punc()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic 'test'";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Yes_Punc()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic 'test'";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = true;

        Test_Votes_Dont_Match(text1, text2);
    }


    [TestMethod]
    public void Check_Match_No_Space()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic 'Test'";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Yes_Space()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic  Test";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = true;

        Test_Votes_Dont_Match(text1, text2);
    }


    [TestMethod]
    public void Check_Match_No_Space_And_Case()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic 'Test'";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Yes_Space_And_Case()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic 'test'";
        quest.CaseIsSignificant = true;
        quest.WhitespaceAndPunctuationIsSignificant = true;

        Test_Votes_Dont_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Yes_Space_And_Case_2()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic Test";
        quest.CaseIsSignificant = true;
        quest.WhitespaceAndPunctuationIsSignificant = true;

        Test_Votes_Dont_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Apostrophe()
    {
        string text1 = "[x] Basic don't";
        string text2 = "[x] Basic don’t";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Quote()
    {
        string text1 = "[x] Basic test";
        string text2 = "[x] Basic “test”";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = false;

        Test_Votes_Match(text1, text2);
    }


    [TestMethod]
    public void Check_Match_Apostrophe_2()
    {
        string text1 = "[x] Basic don't";
        string text2 = "[x] Basic don’t";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = true;

        Test_Votes_Match(text1, text2);
    }

    [TestMethod]
    public void Check_Match_Quote_2()
    {
        string text1 = @"[x] Basic ""test""";
        string text2 = "[x] Basic “test”";
        quest.CaseIsSignificant = false;
        quest.WhitespaceAndPunctuationIsSignificant = true;

        Test_Votes_Match(text1, text2);
    }
    #endregion Test general vote matching
}
