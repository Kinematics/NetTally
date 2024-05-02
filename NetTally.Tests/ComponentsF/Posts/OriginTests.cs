using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTally.Enums;
using NetTally.Tally.ComponentsF.Posts;

namespace NetTally.Tests.ComponentsF.Posts;
[TestClass]
public class OriginTests
{
#nullable disable
    string authorName;
    AuthorType author;
    string threadUrl;
    string permalinkUrl;
    Uri thread;
    Uri permalink;
    PostIdType postId;
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
        author = Author.Create(authorName);
        threadUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/";
        thread = new Uri(threadUrl);
        permalinkUrl = "https://forums.sufficientvelocity.com/threads/renascence-a-homura-quest.10402/post-2236809";
        permalink = new Uri(permalinkUrl);
        postIdNumber = 2236809;
        postId = PostId.Create(postIdNumber);
        postNumber = 2490;
        timestamp = DateTimeOffset.Now;
    }


    [TestMethod]
    public void ConstructUser_Standard()
    {
        var origin = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);

        Assert.IsNotNull(origin);
        Assert.AreEqual(IdentityType.User, origin.Category);
        Assert.AreEqual(author, origin.Author, new AuthorComparer());
        Assert.AreEqual(thread, origin.Thread);
        Assert.AreEqual(permalink, origin.Permalink);
        Assert.AreEqual(postId, origin.PostId);
        Assert.AreEqual(postNumber, origin.ThreadPostNumber);
        Assert.AreEqual(Origin.None, origin.Source, new OriginComparer());
    }

    [TestMethod]
    public void ConstructUser_NoAuthor_Null()
    {
        var origin = Origin.CreateUser(Author.None, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void ConstructUser_InvalidPostNumber_Zero()
    {
        var origin = Origin.CreateUser(author, thread, permalink, postId, -11, timestamp);

        Assert.IsNotNull(origin);
        Assert.AreEqual(IdentityType.User, origin.Category);
        Assert.AreEqual(author, origin.Author, new AuthorComparer());
        Assert.AreEqual(thread, origin.Thread);
        Assert.AreEqual(permalink, origin.Permalink);
        Assert.AreEqual(postId, origin.PostId);
        Assert.AreEqual(0, origin.ThreadPostNumber);
        Assert.AreEqual(Origin.None, origin.Source, new OriginComparer());
    }

    [TestMethod]
    public void ConstructShort_NoUser_Null()
    {
        var origin = Origin.CreateOriginForName(IdentityType.User, Author.None);
        Assert.IsNull(origin);
    }

    [TestMethod]
    public void ConstructShort_Standard()
    {
        var origin = Origin.CreateOriginForName(IdentityType.User, author);
        Assert.IsNotNull(origin);
        Assert.AreEqual(IdentityType.User, origin.Category);
        Assert.AreEqual(author, origin.Author, new AuthorComparer());
        Assert.AreEqual(OriginComparer.ExampleUri, origin.Thread);
        Assert.AreEqual(OriginComparer.ExampleUri, origin.Permalink);
        Assert.AreEqual(PostId.Zero, origin.PostId);
        Assert.AreEqual(0, origin.ThreadPostNumber);
        Assert.AreEqual(Origin.None, origin.Source, new OriginComparer());
    }


    [TestMethod]
    public void Construct_Compare_Equal()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_PostsDiffer_NotEqual()
    {
        PostIdType postId2 = PostId.Create(postIdNumber + 1);
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author, thread, permalink, postId2, postNumber + 1, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_CapsNames_Equal()
    {
        AuthorType author2 = Author.Create(authorName.ToUpper());
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author2, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_DiffNames_NotEqual()
    {
        AuthorType author2 = Author.Create("Louie");
        PostIdType postId2 = PostId.Create(postIdNumber + 1);
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateUser(author2, thread, permalink, postId2, postNumber + 1, timestamp);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_User_with_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        AuthorType plan = Author.Create("Nightlife");
        var origin2 = Origin.GetPlanOrigin(origin1, plan);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, new OriginComparer());
        Assert.AreEqual(origin1, origin2.Source, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Equal_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        AuthorType plan = Author.Create("Nightlife");
        var origin2 = Origin.GetPlanOrigin(origin1, plan);
        Assert.IsNotNull(origin2);
        var origin3 = Origin.GetPlanOrigin(origin1, plan);
        Assert.IsNotNull(origin3);

        Assert.AreEqual(origin2, origin3, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Diff_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        AuthorType plan1 = Author.Create("Nightlife");
        AuthorType plan2 = Author.Create("Beach Trip");
        var origin2 = Origin.GetPlanOrigin(origin1, plan1);
        Assert.IsNotNull(origin2);
        var origin3 = Origin.GetPlanOrigin(origin1, plan2);
        Assert.IsNotNull(origin3);

        Assert.AreNotEqual(origin2, origin3, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Short_Origin_Same()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.User, author);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Short_Origin_Same_Caps()
    {
        AuthorType author2 = Author.Create(authorName.ToUpper());

        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.User, author2);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Short_Origin_Diff_Name()
    {
        AuthorType author2 = Author.Create(authorName + "Z");

        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.User, author2);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Short_Origin_Diff_Type()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        var origin2 = Origin.CreateOriginForName(IdentityType.Plan, author);
        Assert.IsNotNull(origin1);
        Assert.IsNotNull(origin2);

        Assert.AreNotEqual(origin1, origin2, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Aquired_Plan_Short_Plan()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        AuthorType plan1 = Author.Create("Nightlife");
        var origin2 = Origin.GetPlanOrigin(origin1, plan1);
        Assert.IsNotNull(origin2);

        var origin3 = Origin.CreateOriginForName(IdentityType.Plan, plan1);
        Assert.IsNotNull(origin3);

        Assert.AreEqual(origin2, origin3, new OriginComparer());
    }

    [TestMethod]
    public void Compare_Aquired_Plan_Punc()
    {
        var origin1 = Origin.CreateUser(author, thread, permalink, postId, postNumber, timestamp);
        Assert.IsNotNull(origin1);

        AuthorType plan1 = Author.Create("Nightlife");
        var origin2 = Origin.GetPlanOrigin(origin1, plan1);
        Assert.IsNotNull(origin2);

        AuthorType plan2 = Author.Create("Nightlife~!");
        var origin3 = Origin.CreateOriginForName(IdentityType.Plan, plan2);
        Assert.IsNotNull(origin3);

        Assert.AreNotEqual(origin2, origin3, new OriginComparer());
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

        Assert.AreNotEqual(origin1, origin2, new OriginComparer());
    }

}
