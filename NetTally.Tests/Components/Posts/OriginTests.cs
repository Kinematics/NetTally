using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.Components.Posts;

namespace NetTally.Tests.Components.Posts;
[TestClass]
public class OriginTests
{
#nullable disable
    string authorName;
    Author author;
    string threadUrl;
    string permalinkUrl;
    Uri thread;
    Uri permalink;
    PostId postId;
    int postIdNumber;
    int postNumber;
    DateTimeOffset timestamp;
#nullable enable

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        TestStartup.ConfigureServices();
    }

    [TestInitialize]
    public void Initialize()
    {
        authorName = "Kinematics";
        author = Authors.Create(authorName);
        threadUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
        thread = new Uri(threadUrl);
        permalinkUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/post-2236809";
        permalink = new Uri(permalinkUrl);
        postIdNumber = 2236809;
        postId = PostIds.Create(postIdNumber);
        postNumber = 2490;
        timestamp = DateTimeOffset.Now;
    }


    [TestMethod]
    public void ConstructUser_Standard()
    {
        var origin = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);

        Assert.IsNotNull(origin);
        Assert.AreEqual(IdentityType.User, origin.Category);
        Assert.AreEqual(author, origin.Author, AuthorComparer.Instance);
        Assert.AreEqual(thread, origin.Thread);
        Assert.AreEqual(permalink, origin.Permalink);
        Assert.AreEqual(postId, origin.PostId);
        Assert.AreEqual(postNumber, origin.ThreadPostNumber);
        Assert.AreEqual(Origin.None, origin.Source, OriginComparer.Instance);
    }

    [TestMethod]
    public void ConstructUser_NoAuthor_Null()
    {
        var origin = Origin.CreateUser(Authors.None, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void ConstructUser_InvalidPostNumber_Zero()
    {
        var origin = Origin.CreateUser(author, thread, permalink, postId, -11, timestamp);

        Assert.IsNotNull(origin);
        Assert.AreEqual(IdentityType.User, origin.Category);
        Assert.AreEqual(author, origin.Author, AuthorComparer.Instance);
        Assert.AreEqual(thread, origin.Thread);
        Assert.AreEqual(permalink, origin.Permalink);
        Assert.AreEqual(postId, origin.PostId);
        Assert.AreEqual(0, origin.ThreadPostNumber);
        Assert.AreEqual(Origin.None, origin.Source, OriginComparer.Instance);
    }

    [TestMethod]
    public void ConstructShort_NoUser_Null()
    {
        var origin = Origin.CreateOriginForName(IdentityType.User, Authors.None);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void ConstructShort_Standard()
    {
        var origin = Origin.CreateOriginForName(IdentityType.User, author);
        Assert.IsNotNull(origin);
        Assert.AreEqual(IdentityType.User, origin.Category);
        Assert.AreEqual(author, origin.Author, AuthorComparer.Instance);
        Assert.AreEqual(OriginComparer.ExampleUri, origin.Thread);
        Assert.AreEqual(OriginComparer.ExampleUri, origin.Permalink);
        Assert.AreEqual(PostIds.Zero, origin.PostId);
        Assert.AreEqual(0, origin.ThreadPostNumber);
        Assert.AreEqual(Origin.None, origin.Source, OriginComparer.Instance);
    }


    [TestMethod]
    public void Construct_Compare_Equal()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_PostsDiffer_NotEqual()
    {
        PostId postId2 = PostIds.Create(postIdNumber + 1);
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author, thread, permalink, postId2, postNumber + 1, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_CapsNames_Equal()
    {
        Author author2 = Authors.Create(authorName.ToUpper());
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author2, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_DiffNames_NotEqual()
    {
        Author author2 = Authors.Create("Louie");
        PostId postId2 = PostIds.Create(postIdNumber + 1);
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author2, thread, permalink, postId2, postNumber + 1, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_User_with_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        Author plan = Authors.Create("Nightlife");
        var origin2 = Origin.CreatePlanOrigin(origin1, plan);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
        Assert.AreEqual(origin1, origin2.Source, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Equal_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        Author plan = Authors.Create("Nightlife");
        var origin2 = Origin.CreatePlanOrigin(origin1, plan);
        Assert.IsNotNull(origin2);
        var origin3 = Origin.CreatePlanOrigin(origin1, plan);
        Assert.IsNotNull(origin3);

        Assert.AreEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Diff_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        Author plan1 = Authors.Create("Nightlife");
        Author plan2 = Authors.Create("Beach Trip");
        var origin2 = Origin.CreatePlanOrigin(origin1, plan1);
        Assert.IsNotNull(origin2);
        var origin3 = Origin.CreatePlanOrigin(origin1, plan2);
        Assert.IsNotNull(origin3);

        Assert.AreNotEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Same()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.User, author);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Same_Caps()
    {
        Author author2 = Authors.Create(authorName.ToUpper());

        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.User, author2);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Diff_Name()
    {
        Author author2 = Authors.Create(authorName + "Z");

        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.User, author2);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Short_Origin_Diff_Type()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.Plan, author);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Aquired_Plan_Short_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        Author plan1 = Authors.Create("Nightlife");
        var origin2 = Origin.CreatePlanOrigin(origin1, plan1);
        Assert.IsNotNull(origin2);

        var origin3 = Origin.CreateOriginForName(IdentityType.Plan, plan1);
        Assert.IsNotNull(origin3);

        Assert.AreEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Aquired_Plan_Punc()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        Author plan1 = Authors.Create("Nightlife");
        var origin2 = Origin.CreatePlanOrigin(origin1, plan1);
        Assert.IsNotNull(origin2);

        Author plan2 = Authors.Create("Nightlife~!");
        var origin3 = Origin.CreateOriginForName(IdentityType.Plan, plan2);
        Assert.IsNotNull(origin3);

        Assert.AreNotEqual(origin2, origin3, OriginComparer.Instance);
    }

    [TestMethod]
    public void Compare_Diff_Source()
    {
        var threadUrl2 = "https://forums.spacebattles.com/threads/renascence-a-homura-quest.10402/";
        var thread2 = new Uri(threadUrl2);
        var permalinkUrl2 = "https://forums.spacebattles.com/threads/renascence-a-homura-quest.10402/post-2236809";
        var permalink2 = new Uri(permalinkUrl2);

        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author, thread2, permalink2, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, OriginComparer.Instance);
    }

}
