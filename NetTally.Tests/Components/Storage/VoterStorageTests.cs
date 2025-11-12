using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Configure;
using NetTally.Models;
using NetTally.Tally.Processing;
using NetTally.Tally.Storage;

namespace NetTally.Tests.Components.Storage;

[TestClass]
public class VoterStorageTests
{
    #region Setup
    static readonly VoterStorage voterStorage = [];

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestInitialize]
    public void Initialize()
    {
        voterStorage.Clear();
    }
    #endregion Setup

    #region Origin
    private static Origin GetOrigin_Kinematics()
    {
        var author = Author.Create("Kinematics");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123456);
        var postNumber = PostNumber.Create(101);

        var detail = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, detail);

        return origin!;
    }

    private static Origin GetOrigin_Atreya()
    {
        var author = Author.Create("Atreya");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123457);
        var postNumber = PostNumber.Create(102);

        var detail = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, detail);

        return origin!;
    }

    private static Origin GetOrigin_Kimberly()
    {
        var author = Author.Create("Kimberly");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123458);
        var postNumber = PostNumber.Create(103);

        var detail = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, detail);

        return origin!;
    }

    private static Origin GetOrigin_Biigoh()
    {
        var author = Author.Create("Biigoh");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123459);
        var postNumber = PostNumber.Create(104);

        var detail = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, detail);

        return origin!;
    }

    private static Origin GetOrigin_Muramasa()
    {
        var author = Author.Create("Muramasa");
        Uri uri = new(Strings.ExampleHostUrl);
        Uri permalink = new(Strings.ExampleHostUrl);
        var postId = PostId.Create(123460);
        var postNumber = PostNumber.Create(105);

        var detail = Source.Create(uri, permalink, postId, postNumber);
        var origin = Origin.CreateUser(author, detail);

        return origin!;
    }

    private static readonly string VoteText = "[X] A line of vote text";

    private static Post GetPost(Origin origin)
    {
        return Post.Create(origin, VoteText)!;
    }

    private static VoteBlock GetVote(Post post)
    {
        var vote = Vote.Create(post);
        Assert.IsNotNull(vote);

        return VoteBlocks.GetBlocks(vote.VoteLines).First();
    }

    private static Post GetPost_Kinematics()
    {
        return GetPost(GetOrigin_Kinematics());
    }
    #endregion Origin



    [TestMethod]
    public void Store_One_Vote()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        voterStorage.Add(origin, vote);

        Assert.IsTrue(voterStorage.HasIdentity(origin));
        Assert.IsTrue(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Store_Same_Vote()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        voterStorage.Add(origin, vote);
        Assert.ThrowsExactly<ArgumentException>(() => voterStorage.Add(origin, vote));

        Assert.IsTrue(voterStorage.HasIdentity(origin));
        Assert.IsTrue(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Store_Same_Vote_Index()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        voterStorage.Add(origin, vote);
        voterStorage[origin] = vote;

        Assert.IsTrue(voterStorage.HasIdentity(origin));
        Assert.IsTrue(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Try_Store_One_Vote()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        bool added = voterStorage.TryAdd(origin, vote);

        Assert.IsTrue(added);
        Assert.IsTrue(voterStorage.HasIdentity(origin));
        Assert.IsTrue(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Try_Store_Same_Vote()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        voterStorage.Add(origin, vote);
        bool added = voterStorage.TryAdd(origin, vote);

        Assert.IsFalse(added);
        Assert.IsTrue(voterStorage.HasIdentity(origin));
        Assert.IsTrue(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Remove_Vote()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        voterStorage.Add(origin, vote);
        bool removed = voterStorage.Remove(origin);

        Assert.IsTrue(removed);
        Assert.IsFalse(voterStorage.HasIdentity(origin));
        Assert.IsFalse(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.IsEmpty(voterStorage);
    }


    [TestMethod]
    public void Remove_And_Get_Vote()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        voterStorage.Add(origin, vote);
        bool removed = voterStorage.Remove(origin, out var removedVote);

        Assert.IsTrue(removed);
        Assert.IsTrue(VoteBlockComparer.Instance.Equals(vote, removedVote));
        Assert.IsFalse(voterStorage.HasIdentity(origin));
        Assert.IsFalse(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.IsEmpty(voterStorage);
    }

    [TestMethod]
    public void Check_Plan()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);
        var planName = Author.Create("Zoom");
        var planOrigin = Origin.CreatePlan(planName, origin.Author, origin.Source);

        Assert.IsNotNull(planOrigin);

        voterStorage.Add(planOrigin, vote);

        Assert.IsFalse(voterStorage.HasIdentity(origin));
        Assert.IsTrue(voterStorage.HasIdentity(planOrigin));
        Assert.IsFalse(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasPlan(planName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Check_Voter_Simple()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);
        var simpleOrigin = Origin.CreateUser(origin.Name);

        Assert.IsNotNull(simpleOrigin);

        voterStorage.Add(origin, vote);

        Assert.IsTrue(voterStorage.HasIdentity(origin));
        Assert.IsTrue(voterStorage.HasIdentity(simpleOrigin));
        Assert.IsTrue(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Check_Plan_Simple()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        var planName = Author.Create("Zoom");
        var planOrigin = Origin.CreatePlan(planName, origin.Author, origin.Source);
        Assert.IsNotNull(planOrigin);

        var simpleOrigin = Origin.CreatePlan(planOrigin.Name);
        Assert.IsNotNull(simpleOrigin);

        voterStorage.Add(planOrigin, vote);

        Assert.IsTrue(voterStorage.HasIdentity(simpleOrigin));
        Assert.IsTrue(voterStorage.HasPlan(planName));
        Assert.HasCount(1, voterStorage);
    }

    [TestMethod]
    public void Check_Complex_1()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        var planName = Author.Create("Zoom");
        var planOrigin = Origin.CreatePlan(planName, origin.Author, origin.Source);
        Assert.IsNotNull(planOrigin);

        var user2 = GetOrigin_Atreya();
        var user3 = GetOrigin_Kimberly();
        var user4 = GetOrigin_Biigoh();
        var user5 = GetOrigin_Muramasa();

        voterStorage.Add(planOrigin, vote);
        voterStorage.Add(origin, vote);
        voterStorage.Add(user2, vote);
        voterStorage.Add(user3, vote);
        voterStorage.Add(user4, vote);
        voterStorage.Add(user5, vote);

        Assert.IsTrue(voterStorage.HasPlan(planName));
        Assert.IsTrue(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user2.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user3.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user4.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user5.Name.DisplayName));
        Assert.HasCount(6, voterStorage);
    }

    [TestMethod]
    public void Check_Complex_2()
    {
        var post = GetPost_Kinematics();
        var origin = post.Origin;
        var vote = GetVote(post);

        var planName = Author.Create("Zoom");
        var planOrigin = Origin.CreatePlan(planName, origin.Author, origin.Source);
        Assert.IsNotNull(planOrigin);

        var user2 = GetOrigin_Atreya();
        var user3 = GetOrigin_Kimberly();
        var user4 = GetOrigin_Biigoh();
        var user5 = GetOrigin_Muramasa();

        voterStorage.Add(planOrigin, vote);
        voterStorage.Add(origin, vote);
        voterStorage.Add(user2, vote);
        voterStorage.Add(user3, vote);
        voterStorage.Remove(origin);
        voterStorage.Add(user4, vote);
        voterStorage.Add(user5, vote);

        Assert.IsTrue(voterStorage.HasPlan(planName));
        Assert.IsFalse(voterStorage.HasVoter(origin.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user2.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user3.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user4.Name.DisplayName));
        Assert.IsTrue(voterStorage.HasVoter(user5.Name.DisplayName));
        Assert.HasCount(5, voterStorage);
    }
}
